using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Runs processes in parallel on entities
/// </summary>
public class ProcessSystem
{
    private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");
    private static readonly Compound Temperature = SimulationParameters.Instance.GetCompound("temperature");
    private static readonly Compound Sunlight = SimulationParameters.Instance.GetCompound("sunlight");
    private readonly List<Task> tasks = new();

    private readonly Node worldRoot;
    private BiomeConditions? biome;

    public ProcessSystem(Node worldRoot)
    {
        this.worldRoot = worldRoot;
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

        speedFactor *= process.Rate;

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

    public void Process(float delta)
    {
        if (biome == null)
        {
            GD.PrintErr("ProcessSystem has no biome set");
            return;
        }

        var nodes = worldRoot.GetTree().GetNodesInGroup(Constants.PROCESS_GROUP);
        var nodeCount = nodes.Count;

        // Used to go from the calculated compound values to per second values for reporting statistics
        float inverseDelta = 1.0f / delta;

        // The objects are processed here in order to take advantage of threading
        var executor = TaskExecutor.Instance;

        for (int i = 0; i < nodeCount; i += Constants.PROCESS_OBJECTS_PER_TASK)
        {
            int start = i;

            var task = new Task(() =>
            {
                for (int a = start;
                     a < start + Constants.PROCESS_OBJECTS_PER_TASK && a < nodeCount; ++a)
                {
                    ProcessNode(nodes[a] as IProcessable, delta, inverseDelta);
                }
            });

            tasks.Add(task);
        }

        // Start and wait for tasks to finish
        executor.RunTasks(tasks);
        tasks.Clear();
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

    private static float GetAmbientInBiome(Compound compound, BiomeConditions biome, CompoundAmountType amountType)
    {
        if (!biome.TryGetCompound(compound, amountType, out var environmentalCompoundProperties))
            return 0;

        return environmentalCompoundProperties.Ambient;
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

    private void ProcessNode(IProcessable? processor, float delta, float inverseDelta)
    {
        if (processor == null)
        {
            GD.PrintErr("A node has been put in the process group but it isn't derived from IProcessable");
            return;
        }

        var bag = processor.ProcessCompoundStorage;

        // Set all compounds to not be useful, when some compound is
        // used it will be marked useful
        bag.ClearUseful();

        var processStatistics = processor.ProcessStatistics;

        processStatistics?.MarkAllUnused();

        foreach (var process in processor.ActiveProcesses)
        {
            // If rate is 0 dont do it
            // The rate specifies how fast fraction of the specified process numbers this cell can do
            // TODO: would be nice still to report these to process statistics
            if (process.Rate <= 0.0f)
                continue;

            // TODO: reporting duplicate process types would be nice in debug mode here

            var processData = process.Process;

            var currentProcessStatistics = processStatistics?.GetAndMarkUsed(process.Process);
            currentProcessStatistics?.BeginFrame(delta);

            RunProcess(delta, processData, bag, process, currentProcessStatistics, inverseDelta);
        }

        bag.ClampNegativeCompoundAmounts();
        bag.FixNaNCompounds();

        processStatistics?.RemoveUnused();
    }

    private void RunProcess(float delta, BioProcess processData, CompoundBag bag, TweakedProcess process,
        SingleProcessStatistics? currentProcessStatistics, float inverseDelta)
    {
        // Can your cell do the process
        bool canDoProcess = true;

        float environmentModifier = 1.0f;

        // This modifies the process overall speed to allow really fast processes to run, for example if there are
        // a ton of one organelle it might consume 100 glucose per go, which might be unlikely for the cell to have
        // so if there is *some* but not enough space for results (and also inputs) this can run the process as
        // fraction of the speed to allow the cell to still function well
        float spaceConstraintModifier = 1.0f;

        // First check the environmental compounds so that we can build the right environment modifier for accurate
        // check of normal compound input amounts
        foreach (var entry in processData.Inputs)
        {
            // Set used compounds to be useful, we dont want to purge those
            bag.SetUseful(entry.Key);

            if (!entry.Key.IsEnvironmental)
                continue;

            // Processing runs on the current game time following values
            var ambient = GetAmbient(entry.Key, CompoundAmountType.Current);

            // currentProcessStatistics?.AddInputAmount(entry.Key, entry.Value * inverseDelta);
            currentProcessStatistics?.AddInputAmount(entry.Key, ambient);

            // do environmental modifier here, and save it for later
            environmentModifier *= entry.Key == Temperature ?
                CalculateTemperatureEffect(ambient) :
                ambient / entry.Value;

            if (environmentModifier <= MathUtils.EPSILON)
                currentProcessStatistics?.AddLimitingFactor(entry.Key);
        }

        if (environmentModifier <= MathUtils.EPSILON)
            canDoProcess = false;

        // Compute spaceConstraintModifier before updating the final use and input amounts
        foreach (var entry in processData.Inputs)
        {
            if (entry.Key.IsEnvironmental)
                continue;

            var inputRemoved = entry.Value * process.Rate * environmentModifier;

            // currentProcessStatistics?.AddInputAmount(entry.Key, 0);
            // We don't multiply by delta here because we report the per-second values anyway. In the actual process
            // output numbers (computed after testing the speed), we need to multiply by inverse delta
            currentProcessStatistics?.AddInputAmount(entry.Key, inputRemoved);

            inputRemoved = inputRemoved * delta * spaceConstraintModifier;

            // If not enough we can't run the process unless we can lower spaceConstraintModifier enough
            var availableAmount = bag.GetCompoundAmount(entry.Key);
            if (availableAmount < inputRemoved)
            {
                bool canRun = false;

                if (availableAmount > MathUtils.EPSILON)
                {
                    var neededModifier = availableAmount / inputRemoved;

                    if (neededModifier > Constants.MINIMUM_RUNNABLE_PROCESS_FRACTION)
                    {
                        spaceConstraintModifier = neededModifier;
                        canRun = true;

                        // Due to rounding errors there can be very small disparity here between the amount available
                        // and what we will take with the modifiers. See the comment in outputs for more details
                    }
                }

                if (!canRun)
                {
                    canDoProcess = false;
                    currentProcessStatistics?.AddLimitingFactor(entry.Key);
                }
            }
        }

        foreach (var entry in processData.Outputs)
        {
            // For now lets assume compounds we produce are also useful
            bag.SetUseful(entry.Key);

            var outputAdded = entry.Value * process.Rate * environmentModifier;

            // currentProcessStatistics?.AddOutputAmount(entry.Key, 0);
            currentProcessStatistics?.AddOutputAmount(entry.Key, outputAdded);

            outputAdded = outputAdded * delta * spaceConstraintModifier;

            // if environmental right now this isn't released anywhere
            if (entry.Key.IsEnvironmental)
                continue;

            // If no space we can't do the process, if we can't adjust the space constraint modifier enough
            var remainingSpace = bag.GetCapacityForCompound(entry.Key) - bag.GetCompoundAmount(entry.Key);
            if (outputAdded > remainingSpace)
            {
                bool canRun = false;

                if (remainingSpace > MathUtils.EPSILON)
                {
                    var neededModifier = remainingSpace / outputAdded;

                    if (neededModifier > Constants.MINIMUM_RUNNABLE_PROCESS_FRACTION)
                    {
                        spaceConstraintModifier = neededModifier;
                        canRun = true;
                    }

                    // With all of the modifiers we can lose a tiny bit of compound that won't fit due to rounding
                    // errors, but we ignore that here
                }

                if (!canRun)
                {
                    canDoProcess = false;
                    currentProcessStatistics?.AddCapacityProblem(entry.Key);
                }
            }
        }

        // Only carry out this process if you have all the required ingredients and enough space for the outputs
        if (!canDoProcess)
        {
            if (currentProcessStatistics != null)
                currentProcessStatistics.CurrentSpeed = 0;
            return;
        }

        float totalModifier = process.Rate * delta * environmentModifier * spaceConstraintModifier;

        if (currentProcessStatistics != null)
            currentProcessStatistics.CurrentSpeed = process.Rate * environmentModifier * spaceConstraintModifier;

        // Consume inputs
        foreach (var entry in processData.Inputs)
        {
            if (entry.Key.IsEnvironmental)
                continue;

            var inputRemoved = entry.Value * totalModifier;

            currentProcessStatistics?.AddInputAmount(entry.Key, inputRemoved * inverseDelta);

            // This should always succeed (due to the earlier check) so it is always assumed here that this succeeded
            bag.TakeCompound(entry.Key, inputRemoved);
        }

        // Add outputs
        foreach (var entry in processData.Outputs)
        {
            if (entry.Key.IsEnvironmental)
                continue;

            var outputGenerated = entry.Value * totalModifier;

            currentProcessStatistics?.AddOutputAmount(entry.Key, outputGenerated * inverseDelta);

            bag.AddCompound(entry.Key, outputGenerated);
        }
    }
}
