namespace Systems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Runs biological processes on entities
    /// </summary>
    [With(typeof(CompoundStorage))]
    [With(typeof(BioProcesses))]
    public sealed class ProcessSystem : AEntitySetSystem<float>
    {
        private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");
        private static readonly Compound Glucose = SimulationParameters.Instance.GetCompound("glucose");
        private static readonly Compound Temperature = SimulationParameters.Instance.GetCompound("temperature");

        private BiomeConditions? biome;

        /// <summary>
        ///   Used to go from the calculated compound values to per second values for reporting statistics
        /// </summary>
        private float inverseDelta;

        public ProcessSystem(World world, IParallelRunner runner) : base(world, runner,
            Constants.SYSTEM_LOW_ENTITIES_PER_THREAD)
        {
        }

        /// <summary>
        ///   Creates a process list to send to <see cref="BioProcesses"/> from a given list of existing organelles
        /// </summary>
        public static List<TweakedProcess> ComputeActiveProcessList(IEnumerable<IPositionedOrganelle> organelles)
        {
            // TODO: switch to a manual approach if the performance characteristics of this LINQ query is not good
            // The old approach just uses a linear scan of the already handled process types and adds to their existing
            // rate
            return organelles.Select(o => o.Definition).SelectMany(o => o.RunnableProcesses).GroupBy(p => p.Process)
                .Select(g => new TweakedProcess(g.Key, g.Sum(p => p.Count))).ToList();
        }

        /// <summary>
        ///   Computes the process efficiency numbers for given organelles given the active biome data.
        ///   <see cref="amountType"/> specifies how changes during an in-game day are taken into account.
        /// </summary>
        public static Dictionary<string, OrganelleEfficiency> ComputeOrganelleProcessEfficiencies(
            IEnumerable<OrganelleDefinition> organelles, BiomeConditions biome, CompoundAmountType amountType)
        {
            var result = new Dictionary<string, OrganelleEfficiency>();

            foreach (var organelle in organelles)
            {
                var info = new OrganelleEfficiency(organelle);

                foreach (var process in organelle.RunnableProcesses)
                {
                    info.Processes.Add(CalculateProcessMaximumSpeed(process, biome, amountType));
                }

                result[organelle.InternalName] = info;
            }

            return result;
        }

        /// <summary>
        ///   Computes the energy balance for the given organelles in biome and at a given time during the day (or type
        ///   can be specified to be a different type of value)
        /// </summary>
        public static EnergyBalanceInfo ComputeEnergyBalance(IEnumerable<OrganelleTemplate> organelles,
            BiomeConditions biome, MembraneType membrane, bool isPlayerSpecies,
            WorldGenerationSettings worldSettings, CompoundAmountType amountType)
        {
            var organellesList = organelles.ToList();

            var maximumMovementDirection = MicrobeInternalCalculations.MaximumSpeedDirection(organellesList);
            return ComputeEnergyBalance(organellesList, biome, membrane, maximumMovementDirection, isPlayerSpecies,
                worldSettings, amountType);
        }

        /// <summary>
        ///   Computes the energy balance for the given organelles in biome
        /// </summary>
        /// <param name="organelles">The organelles to compute the balance with</param>
        /// <param name="biome">The conditions the organelles are simulated in</param>
        /// <param name="membrane">The membrane type to adjust the energy balance with</param>
        /// <param name="onlyMovementInDirection">
        ///   Only movement organelles that can move in this (cell origin relative) direction are calculated. Other
        ///   movement organelles are assumed to be inactive in the balance calculation.
        /// </param>
        /// <param name="isPlayerSpecies">Whether this microbe is a member of the player's species</param>
        /// <param name="worldSettings">The world generation settings for this game</param>
        /// <param name="amountType">Specifies how changes during an in-game day are taken into account</param>
        public static EnergyBalanceInfo ComputeEnergyBalance(IEnumerable<OrganelleTemplate> organelles,
            BiomeConditions biome, MembraneType membrane, Vector3 onlyMovementInDirection,
            bool isPlayerSpecies, WorldGenerationSettings worldSettings, CompoundAmountType amountType)
        {
            var result = new EnergyBalanceInfo();

            float processATPProduction = 0.0f;
            float processATPConsumption = 0.0f;
            float movementATPConsumption = 0.0f;

            int hexCount = 0;

            foreach (var organelle in organelles)
            {
                foreach (var process in organelle.Definition.RunnableProcesses)
                {
                    var processData = CalculateProcessMaximumSpeed(process, biome, amountType);

                    if (processData.WritableInputs.TryGetValue(ATP, out var amount))
                    {
                        processATPConsumption += amount;

                        result.AddConsumption(organelle.Definition.InternalName, amount);
                    }

                    if (processData.WritableOutputs.TryGetValue(ATP, out amount))
                    {
                        processATPProduction += amount;

                        result.AddProduction(organelle.Definition.InternalName, amount);
                    }
                }

                // Take special cell components that take energy into account
                if (organelle.Definition.HasMovementComponent)
                {
                    var amount = Constants.FLAGELLA_ENERGY_COST;

                    var organelleDirection = MicrobeInternalCalculations.GetOrganelleDirection(organelle);
                    if (organelleDirection.Dot(onlyMovementInDirection) > 0)
                    {
                        movementATPConsumption += amount;
                        result.Flagella += amount;
                        result.AddConsumption(organelle.Definition.InternalName, amount);
                    }
                }

                if (organelle.Definition.HasCiliaComponent)
                {
                    var amount = Constants.CILIA_ENERGY_COST;

                    movementATPConsumption += amount;
                    result.Cilia += amount;
                    result.AddConsumption(organelle.Definition.InternalName, amount);
                }

                // Store hex count
                hexCount += organelle.Definition.HexCount;
            }

            // Add movement consumption together
            result.BaseMovement = Constants.BASE_MOVEMENT_ATP_COST * hexCount;
            result.AddConsumption("baseMovement", result.BaseMovement);
            result.TotalMovement = movementATPConsumption + result.BaseMovement;

            // Add osmoregulation
            result.Osmoregulation = Constants.ATP_COST_FOR_OSMOREGULATION * hexCount *
                membrane.OsmoregulationFactor;

            if (isPlayerSpecies)
            {
                result.Osmoregulation *= worldSettings.OsmoregulationMultiplier;
            }

            result.AddConsumption("osmoregulation", result.Osmoregulation);

            // Compute totals
            result.TotalProduction = processATPProduction;
            result.TotalConsumptionStationary = processATPConsumption + result.Osmoregulation;
            result.TotalConsumption = result.TotalConsumptionStationary + result.TotalMovement;

            result.FinalBalance = result.TotalProduction - result.TotalConsumption;
            result.FinalBalanceStationary = result.TotalProduction - result.TotalConsumptionStationary;

            return result;
        }

        /// <summary>
        ///   Computes the compound balances for given organelle list in a patch and at a given time during the day (or
        ///   using longer timespan values)
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Assumes that all processes run at maximum speed
        ///   </para>
        /// </remarks>
        public static Dictionary<Compound, CompoundBalance> ComputeCompoundBalance(
            IEnumerable<OrganelleDefinition> organelles, BiomeConditions biome, CompoundAmountType amountType)
        {
            var result = new Dictionary<Compound, CompoundBalance>();

            void MakeSureResultExists(Compound compound)
            {
                if (!result.ContainsKey(compound))
                {
                    result[compound] = new CompoundBalance();
                }
            }

            foreach (var organelle in organelles)
            {
                foreach (var process in organelle.RunnableProcesses)
                {
                    var speedAdjusted = CalculateProcessMaximumSpeed(process, biome, amountType);

                    foreach (var input in speedAdjusted.Inputs)
                    {
                        MakeSureResultExists(input.Key);
                        result[input.Key].AddConsumption(organelle.InternalName, input.Value);
                    }

                    foreach (var output in speedAdjusted.Outputs)
                    {
                        MakeSureResultExists(output.Key);
                        result[output.Key].AddProduction(organelle.InternalName, output.Value);
                    }
                }
            }

            return result;
        }

        public static Dictionary<Compound, CompoundBalance> ComputeCompoundBalance(
            IEnumerable<OrganelleTemplate> organelles, BiomeConditions biome, CompoundAmountType amountType)
        {
            return ComputeCompoundBalance(organelles.Select(o => o.Definition), biome, amountType);
        }

        /// <summary>
        ///   Computes the compound balances for given organelle list in a patch and at a given time during the day (or
        ///   using longer timespan values)
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Assumes that the cell produces at most as much ATP as it consumes
        ///   </para>
        /// </remarks>
        public static Dictionary<Compound, CompoundBalance> ComputeCompoundBalanceAtEquilibrium(
            IEnumerable<OrganelleDefinition> organelles, BiomeConditions biome, CompoundAmountType amountType,
            EnergyBalanceInfo energyBalance)
        {
            var result = new Dictionary<Compound, CompoundBalance>();

            void MakeSureResultExists(Compound compound)
            {
                if (!result.ContainsKey(compound))
                {
                    result[compound] = new CompoundBalance();
                }
            }

            float consumptionProductionRatio = energyBalance.TotalConsumption / energyBalance.TotalProduction;
            bool useRatio;

            foreach (var organelle in organelles)
            {
                foreach (var process in organelle.RunnableProcesses)
                {
                    var speedAdjusted = CalculateProcessMaximumSpeed(process, biome, amountType);

                    useRatio = false;

                    // If the cell produces more ATP than it needs, its ATP producing processes need to be toned down
                    if (speedAdjusted.Outputs.ContainsKey(ATP) && consumptionProductionRatio < 1.0f)
                        useRatio = true;

                    foreach (var input in speedAdjusted.Inputs)
                    {
                        if (input.Key == ATP)
                            continue;

                        float amount = input.Value;

                        if (useRatio)
                            amount *= consumptionProductionRatio;

                        MakeSureResultExists(input.Key);
                        result[input.Key].AddConsumption(organelle.InternalName, amount);
                    }

                    foreach (var output in speedAdjusted.Outputs)
                    {
                        if (output.Key == ATP)
                            continue;

                        float amount = output.Value;

                        if (useRatio)
                            amount *= consumptionProductionRatio;

                        MakeSureResultExists(output.Key);
                        result[output.Key].AddProduction(organelle.InternalName, amount);
                    }
                }
            }

            return result;
        }

        public static Dictionary<Compound, CompoundBalance> ComputeCompoundBalanceAtEquilibrium(
            IEnumerable<OrganelleTemplate> organelles, BiomeConditions biome, CompoundAmountType amountType,
            EnergyBalanceInfo energyBalance)
        {
            return ComputeCompoundBalanceAtEquilibrium(organelles.Select(o => o.Definition), biome, amountType,
                energyBalance);
        }

        /// <summary>
        ///   Calculates the maximum speed a process can run at in a biome based on the environmental compounds.
        ///   Can be switched between the average, maximum etc. conditions that occur in the span of an in-game day.
        /// </summary>
        public static ProcessSpeedInformation CalculateProcessMaximumSpeed(TweakedProcess process,
            BiomeConditions biome, CompoundAmountType pointInTimeType)
        {
            var result = new ProcessSpeedInformation(process.Process);

            float speedFactor = 1.0f;
            float efficiency = 1.0f;

            // Environmental inputs need to be processed first
            foreach (var input in process.Process.Inputs)
            {
                if (!input.Key.IsEnvironmental)
                    continue;

                // Environmental compound that can limit the rate
                var availableInEnvironment = GetAmbientInBiome(input.Key, biome, pointInTimeType);

                var availableRate = input.Key == Temperature ?
                    CalculateTemperatureEffect(availableInEnvironment) :
                    availableInEnvironment / input.Value;

                result.AvailableAmounts[input.Key] = availableInEnvironment;

                efficiency *= availableInEnvironment;

                // More than needed environment value boosts the effectiveness
                result.AvailableRates[input.Key] = availableRate;

                speedFactor *= availableRate;

                result.WritableInputs[input.Key] = input.Value;
            }

            result.Efficiency = efficiency;

            speedFactor *= process.Rate * process.Count;

            // Note that we don't consider storage constraints here so we don't use spaceConstraintModifier calculations

            // So that the speed factor is available here
            foreach (var entry in process.Process.Inputs)
            {
                if (entry.Key.IsEnvironmental)
                    continue;

                // Normal, cloud input

                result.WritableInputs.Add(entry.Key, entry.Value * speedFactor);
            }

            foreach (var entry in process.Process.Outputs)
            {
                var amount = entry.Value * speedFactor;

                result.WritableOutputs[entry.Key] = amount;

                if (amount <= 0)
                    result.WritableLimitingCompounds.Add(entry.Key);
            }

            result.CurrentSpeed = speedFactor;

            return result;
        }

        public static float CalculateEnvironmentModifier(BioProcess processData,
            SingleProcessStatistics? currentProcessStatistics, BiomeConditions biome)
        {
            float environmentModifier = 1.0f;

            foreach (var entry in processData.Inputs)
            {
                if (!entry.Key.IsEnvironmental)
                    continue;

                // Processing runs on the current game time following values
                var ambient = GetAmbientInBiome(entry.Key, biome, CompoundAmountType.Current);

                // currentProcessStatistics?.AddInputAmount(entry.Key, entry.Value * inverseDelta);
                currentProcessStatistics?.AddInputAmount(entry.Key, ambient);

                // do environmental modifier here, and save it for later
                environmentModifier *= entry.Key == Temperature ?
                    CalculateTemperatureEffect(ambient) :
                    ambient / entry.Value;

                if (environmentModifier <= MathUtils.EPSILON)
                    currentProcessStatistics?.AddLimitingFactor(entry.Key);
            }

            return environmentModifier;
        }

        /// <summary>
        ///   Sets the biome whose environmental values affect processes
        /// </summary>
        public void SetBiome(BiomeConditions newBiome)
        {
            biome = newBiome;
        }

        /// <summary>
        ///   Get the current amount of environmental compound
        /// </summary>
        public float GetAmbient(Compound compound, CompoundAmountType amountType)
        {
            if (biome == null)
                throw new InvalidOperationException("Biome needs to be set before getting ambient compounds");

            return GetAmbientInBiome(compound, biome, amountType);
        }

        protected override void PreUpdate(float delta)
        {
            if (biome == null)
            {
                GD.PrintErr("ProcessSystem has no biome set");
            }

            inverseDelta = 1.0f / delta;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var storage = ref entity.Get<CompoundStorage>();
            ref var processes = ref entity.Get<BioProcesses>();

            ProcessNode(ref processes, ref storage, delta);
        }

        /// <summary>
        ///   Since temperature works differently to other compounds, we use this method to deal with it. Logic here
        ///   is liable to be updated in the future to use alternative effect models.
        /// </summary>
        private static float CalculateTemperatureEffect(float temperature)
        {
            // Assume thermosynthetic processes are most efficient at 100°C and drop off linearly to zero
            var optimal = 100;
            return Mathf.Clamp(temperature / optimal, 0, 2 - temperature / optimal);
        }

        private static float GetAmbientInBiome(Compound compound, BiomeConditions biome, CompoundAmountType amountType)
        {
            if (!biome.TryGetCompound(compound, amountType, out var environmentalCompoundProperties))
                return 0;

            return environmentalCompoundProperties.Ambient;
        }

        private void ProcessNode(ref BioProcesses processor, ref CompoundStorage storage, float delta)
        {
            var bag = storage.Compounds;

            // Set all compounds to not be useful, when some compound is used it will be marked useful
            bag.ClearUseful();

            var processStatistics = processor.ProcessStatistics;

            processStatistics?.MarkAllUnused();

            if (processor.ActiveProcesses != null)
            {
                foreach (var process in processor.ActiveProcesses)
                {
                    var currentProcessStatistics = processStatistics?.GetAndMarkUsed(process.Process);
                    currentProcessStatistics?.BeginFrame(delta);

                    RunProcess(ref processor, process, bag, currentProcessStatistics, delta);
                }
            }

            bag.ClampNegativeCompoundAmounts();
            bag.FixNaNCompounds();

            processStatistics?.RemoveUnused();
        }

        private void RunProcess(ref BioProcesses processor, TweakedProcess process, CompoundBag bag,
            SingleProcessStatistics? currentProcessStatistics, float delta)
        {
            if (biome == null)
                throw new NullReferenceException("Biome needs to be set");

            var processData = process.Process;

            // Make RuBisCo display properly
            if (process.Rate == 0 && processData.InternalName == "calvin_cycle")
            {
                currentProcessStatistics?.AddLimitingFactor(ATP);
            }

            // First check the environmental compounds so that we can build the right environment modifier for accurate
            // check of normal compound input amounts
            float environmentModifier = CalculateEnvironmentModifier(processData, null, biome);

            float resourceOveruseLimiter = 1.0f;
            foreach (var entry in processData.Inputs)
            {
                bag.SetUseful(entry.Key);

                if (entry.Key.IsEnvironmental)
                    continue;

                var inputRemoved = entry.Value * environmentModifier * process.Rate * process.Count;

                // currentProcessStatistics?.AddInputAmount(entry.Key, 0);
                // We don't multiply by delta here because we report the per-second values anyway. In the actual
                // process output numbers (computed after testing the speed), we need to multiply by inverse delta
                currentProcessStatistics?.AddInputAmount(entry.Key, inputRemoved);

                inputRemoved *= delta;

                // If not enough we can't run the process unless we can lower resourceOveruseLimiter enough
                var availableAmount = bag.GetCompoundAmount(entry.Key);
                if (availableAmount < inputRemoved)
                {
                    var neededModifier = availableAmount / inputRemoved;

                    if (neededModifier > Constants.MINIMUM_RUNNABLE_PROCESS_FRACTION)
                    {
                        if (neededModifier < resourceOveruseLimiter)
                        {
                            resourceOveruseLimiter = neededModifier;
                        }
                    }
                    else
                    {
                        resourceOveruseLimiter = 0;
                        currentProcessStatistics?.AddLimitingFactor(entry.Key);
                    }
                }
            }

            var overProductionLimiter = 1.0f;
            foreach (var entry in processData.Outputs)
            {
                // For now lets assume compounds we produce are also useful
                bag.SetUseful(entry.Key);

                var outputAdded = entry.Value * environmentModifier * process.Rate * process.Count;

                // currentProcessStatistics?.AddOutputAmount(entry.Key, 0);
                currentProcessStatistics?.AddOutputAmount(entry.Key, outputAdded);

                outputAdded *= delta;

                // if environmental right now this isn't released anywhere
                if (entry.Key.IsEnvironmental)
                    continue;

                // If there no space is left and we can't adjust the overProductionLimiter enough,
                // we can't run the process.
                var remainingSpace = bag.GetCapacityForCompound(entry.Key) - bag.GetCompoundAmount(entry.Key);
                if (outputAdded > remainingSpace)
                {
                    var neededModifier = remainingSpace / outputAdded;

                    if (neededModifier > Constants.MINIMUM_RUNNABLE_PROCESS_FRACTION)
                    {
                        if (neededModifier < overProductionLimiter)
                        {
                            overProductionLimiter = neededModifier;
                        }
                    }
                    else
                    {
                        overProductionLimiter = 0;
                        currentProcessStatistics?.AddCapacityProblem(entry.Key);
                    }
                }
            }

            // This modifies the process overall speed to allow really fast processes to run, for example if there are
            // a ton of one organelle it might consume 100 glucose per go, which might be unlikely for the cell to have
            // so if there is *some* but not enough space for results (and also inputs) this can run the process as
            // fraction of the speed to allow the cell to still function well
            var storageModifier = Mathf.Min(overProductionLimiter, resourceOveruseLimiter);

            float semiModifier = process.Rate * process.Count * delta * environmentModifier;
            float totalModifier = semiModifier * storageModifier;

            if (currentProcessStatistics != null)
            {
                currentProcessStatistics.CurrentSpeed = process.Rate * process.Count * environmentModifier *
                    storageModifier;
            }

            var glucoseConsumer = false;

            // Consume inputs
            foreach (var entry in processData.Inputs)
            {
                if (entry.Key.IsEnvironmental)
                    continue;

                if (entry.Key == Glucose && totalModifier >= MathUtils.EPSILON)
                    glucoseConsumer = true;

                var inputRemoved = entry.Value * totalModifier;

                currentProcessStatistics?.AddInputAmount(entry.Key, inputRemoved * inverseDelta);

                // This should always succeed (due to the earlier check) so it is always assumed here that this
                // succeeded
                bag.TakeCompound(entry.Key, inputRemoved);
            }

            // Add outputs
            foreach (var entry in processData.Outputs)
            {
                if (entry.Key.IsEnvironmental)
                    continue;

                var outputGenerated = entry.Value * totalModifier;

                if (entry.Key == ATP)
                {
                    if (glucoseConsumer)
                    {
                        processor.GlucoseATP += outputGenerated;
                    }
                    else if (overProductionLimiter < resourceOveruseLimiter)
                    {
                        // Calculate how much ATP would have been generated if it wouldn't have overproduced
                        processor.LimitedATP += entry.Value * semiModifier *
                            (resourceOveruseLimiter - overProductionLimiter);
                    }
                }

                currentProcessStatistics?.AddOutputAmount(entry.Key, outputGenerated * inverseDelta);

                bag.AddCompound(entry.Key, outputGenerated);
            }
        }
    }
}
