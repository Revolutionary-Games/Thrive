#if DEBUG
#define CHECK_USED_STATISTICS
#endif

namespace Systems;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutoEvo;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;
using World = DefaultEcs.World;

/// <summary>
///   Runs biological processes on entities
/// </summary>
/// <remarks>
///   <para>
///     This is marked as writing to the processes due to <see cref="BioProcesses.ProcessStatistics"/>
///   </para>
/// </remarks>
[With(typeof(CompoundStorage))]
[With(typeof(BioProcesses))]
[RunsAfter(typeof(CompoundAbsorptionSystem))]
[RunsBefore(typeof(OsmoregulationAndHealingSystem))]
[RunsBefore(typeof(MicrobeMovementSystem))]
[RuntimeCost(55)]
public sealed class ProcessSystem : AEntitySetSystem<float>
{
#if CHECK_USED_STATISTICS
    private readonly List<ProcessStatistics> usedStatistics = new();
#endif

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
    /// <remarks>
    ///   <para>
    ///     This takes in an existing list to avoid allocations as much as possible. It is more efficient to clear the
    ///     list if it is used for unrelated cells before calling this method.
    ///   </para>
    /// </remarks>
    public static void ComputeActiveProcessList(IReadOnlyList<IPositionedOrganelle> organelles,
        [NotNull] ref List<TweakedProcess>? result)
    {
        result ??= new List<TweakedProcess>();

        int count = organelles.Count;
        for (int i = 0; i < count; ++i)
        {
            var organelle = organelles[i];
            var processes = organelle.Definition.RunnableProcesses;

            // Organelle upgrades can change the processes of an organelle so that needs to be checked here
            if (organelle.Upgrades != null)
            {
                var upgraded = organelle.Definition.GetUpgradeProcesses(organelle.Upgrades);
                if (upgraded != null)
                    processes = upgraded;
            }

            MergeProcessLists(result, processes);
        }

        // Remove unmarked processes, so that old processes aren't kept around
        // Also unmarks marked processes
        int writeIndex = 0;

        for (int readIndex = 0; readIndex < result.Count; ++readIndex)
        {
            if (result[readIndex].Marked)
            {
                var process = result[readIndex];
                process.Marked = false;
                result[writeIndex++] = process;
            }
        }

        // This if is not strictly necessary as RemoveRange works with also 0 items
        if (writeIndex < result.Count)
            result.RemoveRange(writeIndex, result.Count - writeIndex);
    }

    /// <summary>
    ///   Merges <paramref name="toAdd"/> into <paramref name="result"/>. <paramref name="toAdd"/> is unaffected.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Linearly scans for duplicate processes (merging them). Might need to be updated to use a map provided by a
    ///     parameter if the current algorithm is too slow.
    ///   </para>
    /// </remarks>
    public static void MergeProcessLists(List<TweakedProcess> result, List<TweakedProcess> toAdd)
    {
        int processCount = toAdd.Count;

        for (int i = 0; i < processCount; ++i)
        {
            var process = toAdd[i];
            var processKey = process.Process;

            bool added = false;

            // Try to add to existing result first
            int resultCount = result.Count;
            for (int j = 0; j < resultCount; ++j)
            {
                if (result[j].Process == processKey)
                {
                    var replacedEntry = result[j];

                    if (!replacedEntry.Marked)
                    {
                        // Added to an entry that is kept for keeping a consistent speed multiplier, but isn't yet
                        // considered to be a real result entry
                        // To keep consistent ordering no matter what the old data is, we need to move the current
                        // item to be in place of the first non-marked item
                        for (int l = 0; l < j; ++l)
                        {
                            if (!result[l].Marked)
                            {
                                // Swap positions of the data, as we will write to the k index (that is updated)
                                // we need to only write the moving away data to perform the swap
                                result[j] = result[l];
                                j = l;
                                break;
                            }
                        }

                        // Add without copying the base rate as that is outdated data we don't want to add to
                        result[j] = new TweakedProcess(processKey, process.Rate)
                        {
                            SpeedMultiplier = replacedEntry.SpeedMultiplier,
                            Marked = true,
                        };
                    }
                    else
                    {
                        // Add to the existing rate, as TweakedProcess is a struct this doesn't allocate memory
                        result[j] = new TweakedProcess(processKey, process.Rate + replacedEntry.Rate)
                        {
                            SpeedMultiplier = replacedEntry.SpeedMultiplier,
                            Marked = true,
                        };
                    }

                    added = true;
                    break;
                }
            }

            if (added)
                continue;

            // If not found, then create a new result
            result.Add(new TweakedProcess(processKey, process.Rate)
            {
                Marked = true,
            });
        }
    }

    /// <summary>
    ///   Computes the process efficiency numbers for given organelles given the active biome data.
    ///   <see cref="amountType"/> specifies how changes during an in-game day are taken into account.
    /// </summary>
    public static Dictionary<string, OrganelleEfficiency> ComputeOrganelleProcessEfficiencies(
        IEnumerable<OrganelleDefinition> organelles, BiomeConditions biome,
        ResolvedMicrobeTolerances environmentTolerances, CompoundAmountType amountType)
    {
        var result = new Dictionary<string, OrganelleEfficiency>();

        foreach (var organelle in organelles)
        {
            var info = new OrganelleEfficiency(organelle);

            foreach (var process in organelle.RunnableProcesses)
            {
                info.Processes.Add(CalculateProcessMaximumSpeed(process, environmentTolerances.ProcessSpeedModifier,
                    biome, amountType, false));
            }

            result[organelle.InternalName] = info;
        }

        return result;
    }

    /// <summary>
    ///   Computes the simple energy balance for the given organelles in biome
    /// </summary>
    /// <param name="organelles">The organelles to compute the balance with</param>
    /// <param name="biome">The conditions the organelles are simulated in</param>
    /// <param name="environmentTolerances">Environmental tolerances that affect the processes</param>
    /// <param name="membrane">The membrane type to adjust the energy balance with</param>
    /// <param name="onlyMovementInDirection">
    ///   Only movement organelles that can move in this (cell origin relative) direction are calculated. Other
    ///   movement organelles are assumed to be inactive in the balance calculation.
    /// </param>
    /// <param name="includeMovementCost">
    ///   Only when true are movement related energy costs included in the calculation. When false base movement data
    ///   is provided, but it is not taken into account in the sums, but total movement cost is not calculated. If that
    ///   is required then include movement cost parameter should be set to true and from the result the variables
    ///   giving balance without movement should be used as an alternative to setting this false.
    /// </param>
    /// <param name="isPlayerSpecies">Whether this microbe is a member of the player's species</param>
    /// <param name="worldSettings">The world generation settings for this game</param>
    /// <param name="amountType">Specifies how changes during an in-game day are taken into account</param>
    /// <param name="cache">Auto-Evo Cache for speeding up the function</param>
    /// <param name="result">
    ///   The resulting energy balance.
    /// </param>
    public static void ComputeEnergyBalanceSimple(IReadOnlyList<OrganelleTemplate> organelles,
        IBiomeConditions biome, in ResolvedMicrobeTolerances environmentTolerances, MembraneType membrane,
        Vector3 onlyMovementInDirection,
        bool includeMovementCost, bool isPlayerSpecies, WorldGenerationSettings worldSettings,
        CompoundAmountType amountType, SimulationCache? cache,
        EnergyBalanceInfoSimple result)
    {
#if DEBUG
        if (result is EnergyBalanceInfoFull)
        {
            if (Debugger.IsAttached)
                Debugger.Break();

            throw new ArgumentException("Call the full result variant when you have a full result object " +
                "(otherwise it won't be filled correctly)");
        }
#endif

        CalculateSimplePartOfEnergyBalance(organelles, biome, environmentTolerances, membrane, onlyMovementInDirection,
            includeMovementCost, isPlayerSpecies, worldSettings, amountType, cache, result);
    }

    /// <summary>
    ///   Computes the full energy balance for the given organelles in biome
    /// </summary>
    /// <param name="organelles">The organelles to compute the balance with</param>
    /// <param name="biome">The conditions the organelles are simulated in</param>
    /// <param name="environmentTolerances">Environmental tolerances that affect this</param>
    /// <param name="membrane">The membrane type to adjust the energy balance with</param>
    /// <param name="onlyMovementInDirection">
    ///   Only movement organelles that can move in this (cell origin relative) direction are calculated. Other
    ///   movement organelles are assumed to be inactive in the balance calculation.
    /// </param>
    /// <param name="includeMovementCost">
    ///   Only when true are movement-related energy costs included in the calculation. When false base movement data
    ///   is provided, but it is not taken into account in the sums, but total movement cost is not calculated. If that
    ///   is required, then include movement cost parameter should be set to true, and from the result, the variables
    ///   giving balance without movement should be used as an alternative to setting this false.
    /// </param>
    /// <param name="isPlayerSpecies">Whether this microbe is a member of the player's species</param>
    /// <param name="worldSettings">The world generation settings for this game</param>
    /// <param name="amountType">Specifies how changes during an in-game day are taken into account</param>
    /// <param name="cache">Auto-Evo Cache for speeding up the function</param>
    /// <param name="result">The resulting energy balance.</param>
    public static void ComputeEnergyBalanceFull(IReadOnlyList<OrganelleTemplate> organelles,
        IBiomeConditions biome, ResolvedMicrobeTolerances environmentTolerances, MembraneType membrane,
        Vector3 onlyMovementInDirection,
        bool includeMovementCost, bool isPlayerSpecies, WorldGenerationSettings worldSettings,
        CompoundAmountType amountType, SimulationCache? cache, EnergyBalanceInfoFull result)
    {
        CalculateSimplePartOfEnergyBalance(organelles, biome, environmentTolerances, membrane, onlyMovementInDirection,
            includeMovementCost, isPlayerSpecies, worldSettings, amountType, cache, result);

        // Once simple balance is calculated we add the extra info on top, this approach loops the organelles twice
        // but reduces code duplication

        int organelleCount = organelles.Count;
        for (int i = 0; i < organelleCount; ++i)
        {
            var organelle = organelles[i];

            AddOrganelleATPTracking(organelle, biome, environmentTolerances, amountType, cache, result);

            // Take special cell components that take energy into account
            if (TryGetMovementCostForOrganelle(includeMovementCost, organelle, onlyMovementInDirection, out var cost))
                result.AddConsumption(organelle.Definition.InternalName, cost);

            if (includeMovementCost && organelle.Definition.HasCiliaComponent)
            {
                var amount = Constants.CILIA_ENERGY_COST;
                result.AddConsumption(organelle.Definition.InternalName, amount);
            }
        }

        // Movement and osmoregulation are calculated as totals already, so these aren't added up,
        // but rather they replace any previously added value.
        // The movement and osmoregulation values are copied into the consumption breakdown here.
        // These don't use the usual add method as these values are already included in the totals,
        // so using that would lead to double counting.
        if (includeMovementCost)
        {
            result.Consumption["baseMovement"] = result.BaseMovement;
        }

        result.Consumption["osmoregulation"] = result.Osmoregulation;
    }

    /// <summary>
    ///   Computes the compound balances for given organelle list in a patch and at a given time during the day (or
    ///   using longer timespan values)
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Assumes that all processes run at maximum speed but only if input compounds are present in
    ///     <see cref="biome"/> when <see cref="requireInputCompoundsInBiome"/> is true. If false processes can be
    ///     assumed to run at normal speed even without the input compounds being present in the given biome.
    ///   </para>
    /// </remarks>
    public static void ComputeCompoundBalance(IEnumerable<OrganelleDefinition> organelles, IBiomeConditions biome,
        ResolvedMicrobeTolerances environmentTolerances, CompoundAmountType amountType,
        bool requireInputCompoundsInBiome, Dictionary<Compound, CompoundBalance> result)
    {
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
                var speedAdjusted =
                    CalculateProcessMaximumSpeed(process, environmentTolerances.ProcessSpeedModifier, biome, amountType,
                        requireInputCompoundsInBiome);

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
    }

    public static void ComputeCompoundBalance(IEnumerable<OrganelleTemplate> organelles, IBiomeConditions biome,
        ResolvedMicrobeTolerances environmentTolerances, CompoundAmountType amountType,
        bool requireInputCompoundsInBiome, Dictionary<Compound, CompoundBalance> result)
    {
        ComputeCompoundBalance(organelles.Select(o => o.Definition), biome, environmentTolerances, amountType,
            requireInputCompoundsInBiome, result);
    }

    /// <summary>
    ///   Computes the compound balances for given organelle list in a patch and at a given time during the day (or
    ///   using longer timespan values)
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Assumes that the cell produces at most as much ATP as it consumes (but can only run processes that have
    ///     input compounds present in <see cref="biome"/>)
    ///   </para>
    /// </remarks>
    public static void ComputeCompoundBalanceAtEquilibrium(IEnumerable<OrganelleDefinition> organelles,
        IBiomeConditions biome, ResolvedMicrobeTolerances environmentTolerances, CompoundAmountType amountType,
        EnergyBalanceInfoSimple energyBalance, Dictionary<Compound, CompoundBalance> result)
    {
        void MakeSureResultExists(Compound compound)
        {
            if (!result.ContainsKey(compound))
            {
                result[compound] = new CompoundBalance();
            }
        }

        float consumptionProductionRatio = energyBalance.TotalConsumption / energyBalance.TotalProduction;

        foreach (var organelle in organelles)
        {
            foreach (var process in organelle.RunnableProcesses)
            {
                var speedAdjusted = CalculateProcessMaximumSpeed(process, environmentTolerances.ProcessSpeedModifier,
                    biome, amountType, true);

                // If the cell produces more ATP than it needs, its ATP producing processes need to be toned down
                bool useRatio = speedAdjusted.Outputs.ContainsKey(Compound.ATP) && consumptionProductionRatio < 1.0f;

                foreach (var input in speedAdjusted.Inputs)
                {
                    if (input.Key == Compound.ATP)
                        continue;

                    float amount = input.Value;

                    if (useRatio)
                        amount *= consumptionProductionRatio;

                    MakeSureResultExists(input.Key);
                    result[input.Key].AddConsumption(organelle.InternalName, amount);
                }

                foreach (var output in speedAdjusted.Outputs)
                {
                    if (output.Key == Compound.ATP)
                        continue;

                    float amount = output.Value;

                    if (useRatio)
                        amount *= consumptionProductionRatio;

                    MakeSureResultExists(output.Key);
                    result[output.Key].AddProduction(organelle.InternalName, amount);
                }
            }
        }
    }

    public static void ComputeCompoundBalanceAtEquilibrium(IEnumerable<OrganelleTemplate> organelles,
        IBiomeConditions biome, ResolvedMicrobeTolerances environmentTolerances, CompoundAmountType amountType,
        EnergyBalanceInfoSimple energyBalance, Dictionary<Compound, CompoundBalance> result)
    {
        ComputeCompoundBalanceAtEquilibrium(organelles.Select(o => o.Definition), biome, environmentTolerances,
            amountType, energyBalance, result);
    }

    /// <summary>
    ///   Calculates into the balances how long it takes for each compound type to be filled
    /// </summary>
    /// <returns>The value of <see cref="balancesToSupplement"/> supplemented with the fill times</returns>
    public static Dictionary<Compound, CompoundBalance> ComputeCompoundFillTimes(
        Dictionary<Compound, CompoundBalance> balancesToSupplement,
        float nominalCapacity, Dictionary<Compound, float> specificCapacities)
    {
        foreach (var entry in balancesToSupplement)
        {
            // TODO: should this calculate negative balance "drain" times?
            var balance = entry.Value;
            if (balance.Balance <= 0)
                continue;

            var capacity = specificCapacities.GetValueOrDefault(entry.Key, nominalCapacity);

            balance.FillTime = capacity / balance.Balance;
        }

        return balancesToSupplement;
    }

    /// <summary>
    ///   Calculates ATP balance with the given organelle in the given <see cref="biome"/> (so only processes with
    ///   input compounds present in the biome can run)
    /// </summary>
    public static (float Production, float Consumption) CalculateOrganelleATPBalance(OrganelleTemplate organelle,
        IBiomeConditions biome, CompoundAmountType amountType, float speedModifier,
        SimulationCache? cache)
    {
        float processATPProduction = 0.0f;
        float processATPConsumption = 0.0f;

        foreach (var process in organelle.Definition.RunnableProcesses)
        {
            ProcessSpeedInformation processData;
            if (cache != null && amountType == CompoundAmountType.Average)
            {
                processData = cache.GetProcessMaximumSpeed(process, speedModifier, biome);
            }
            else
            {
                processData = CalculateProcessMaximumSpeed(process, speedModifier, biome, amountType, true);
            }

            var amount = processData.ATPConsumption;
            if (amount > 0)
                processATPConsumption += amount;

            amount = processData.ATPProduction;
            if (amount > 0)
                processATPProduction += amount;
        }

        return (processATPProduction, processATPConsumption);
    }

    /// <summary>
    ///   Adds the ATP balance tracking to the <see cref="EnergyBalanceInfoFull"/> for the given organelle in the given
    ///   <see cref="biome"/> (so only processes with input compounds present in the biome can run)
    /// </summary>
    public static void AddOrganelleATPTracking(OrganelleTemplate organelle,
        IBiomeConditions biome, ResolvedMicrobeTolerances environmentTolerances, CompoundAmountType amountType,
        SimulationCache? cache, EnergyBalanceInfoFull result)
    {
        foreach (var process in organelle.Definition.RunnableProcesses)
        {
            ProcessSpeedInformation processData;
            if (cache != null && amountType == CompoundAmountType.Average)
            {
                processData = cache.GetProcessMaximumSpeed(process, environmentTolerances.ProcessSpeedModifier, biome);
            }
            else
            {
                processData = CalculateProcessMaximumSpeed(process, environmentTolerances.ProcessSpeedModifier, biome,
                    amountType, true);
            }

            var amount = processData.ATPConsumption;

            if (amount > 0)
                result.AddConsumption(organelle.Definition.InternalName, amount);

            amount = processData.ATPProduction;
            if (amount > 0)
                result.AddProduction(organelle.Definition.InternalName, amount, processData.WritableInputs);
        }
    }

    /// <summary>
    ///   Calculates the maximum speed a process can run at in a biome based on the environmental compounds.
    ///   Can be switched between the average, maximum etc. conditions that occur in the span of an in-game day.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     If <see cref="requireInputCompoundsInBiome"/> is true then this method checks that the process inputs
    ///     (except ATP) is present in <see cref="biome"/> and if some input is not available then the process is
    ///     calculated to have <b>0</b> speed.
    ///   </para>
    /// </remarks>
    public static ProcessSpeedInformation CalculateProcessMaximumSpeed(TweakedProcess process,
        float speedModifier, IBiomeConditions biome, CompoundAmountType pointInTimeType,
        bool requireInputCompoundsInBiome)
    {
        var result = new ProcessSpeedInformation(process.Process);

        float speedFactor = 1.0f;
        float efficiency = 1.0f;

        if (requireInputCompoundsInBiome)
        {
            foreach (var input in process.Process.Inputs)
            {
                var inputCompound = input.Key.ID;

                // TODO: maybe this check should be expanded to consider any input compounds that the cell can produce
                // *itself* that way this will consider non-directly used compounds and calculate the speed correctly.
                // Without this any compounds cells usually produce will need to be skipped here similarly to ATP.
                if (inputCompound == Compound.ATP)
                    continue;

                // Quickly check all inputs to see if they are in the patch
                var inPatch = false;
                if (biome.TryGetCompound(inputCompound, CompoundAmountType.Average, out var neededCompound))
                {
                    if (neededCompound.Ambient > 0 || neededCompound.Amount > 0)
                    {
                        inPatch = true;
                    }
                }

                if (!inPatch)
                {
                    foreach (var chunk in biome.Chunks.Values)
                    {
                        if (chunk.Density > 0 &&
                            chunk.Compounds?.TryGetValue(inputCompound, out var chunkCompound) == true)
                        {
                            if (chunkCompound.Amount > 0)
                            {
                                inPatch = true;
                                break;
                            }
                        }
                    }
                }

                if (!inPatch)
                    speedFactor = 0;
            }
        }

        // Environmental inputs need to be processed first
        foreach (var input in process.Process.Inputs)
        {
            if (!input.Key.IsEnvironmental)
                continue;

            var inputCompound = input.Key.ID;

            // Environmental compound that can limit the rate
            var availableInEnvironment = GetAmbientInBiome(inputCompound, biome, pointInTimeType);

            // Is a serious limit if there is none of the compound
            if (availableInEnvironment <= 0)
            {
                result.WritableLimitingCompounds.Add(input.Key.ID);
            }

            var availableRate = inputCompound == Compound.Temperature ?
                CalculateTemperatureEffect(availableInEnvironment) :
                availableInEnvironment / (input.Value * speedModifier);

            result.AvailableAmounts[inputCompound] = availableInEnvironment;

            efficiency *= availableInEnvironment;

            // More than needed environment value boosts the effectiveness
            result.AvailableRates[inputCompound] = availableRate;

            speedFactor *= availableRate;

            result.WritableInputs[inputCompound] = input.Value * speedModifier;
        }

        result.Efficiency = efficiency;

        speedFactor *= process.Rate;

        // Note that we don't consider storage constraints here so we don't use spaceConstraintModifier calculations

        // To not claim the output is the limiting factor when no inputs could be taken at all, track if any inputs
        // are added
        bool tookInputs = false;

        // So that the speed factor is available here
        foreach (var entry in process.Process.Inputs)
        {
            if (entry.Key.IsEnvironmental)
                continue;

            // Normal, cloud input

            var inputCompound = entry.Key.ID;

            var adjustedValue = entry.Value * speedFactor * speedModifier;
            result.WritableInputs.Add(inputCompound, adjustedValue);

            if (adjustedValue > 0)
            {
                tookInputs = true;
            }
            else
            {
                // Cannot take any of this input, mark as a problem. This is helpful at least in the editor process
                // panel view.
                // TODO: this kind of unnecessarily marks some stuff red when environmental conditions are the real
                // problem
                result.WritableLimitingCompounds.Add(entry.Key.ID);
            }

            if (inputCompound == Compound.ATP)
                result.ATPConsumption += adjustedValue;
        }

        foreach (var entry in process.Process.Outputs)
        {
            var amount = entry.Value * speedFactor * speedModifier;

            var outputCompound = entry.Key.ID;

            result.WritableOutputs[outputCompound] = amount;

            if (amount <= 0 && tookInputs)
                result.WritableLimitingCompounds.Add(outputCompound);

            if (outputCompound == Compound.ATP)
                result.ATPProduction += amount;
        }

        result.CurrentSpeed = speedFactor;

        return result;
    }

    /// <summary>
    ///   Calculates an active process list for a species for use in <see cref="IWorldEffect"/>. Note that this only
    ///   supports <see cref="MicrobeSpecies"/> currently.
    /// </summary>
    /// <returns>
    ///   Balance modifier that is to be passed to <see cref="CalculateEffectiveProcessSpeedForEffect"/>
    /// </returns>
    public static float CalculateSpeciesActiveProcessListForEffect(Species species,
        List<TweakedProcess> processesResult, BiomeConditions conditions,
        ResolvedMicrobeTolerances environmentTolerances, WorldGenerationSettings worldGenerationSettings,
        SimulationCache? cache = null)
    {
        // Only microbial species can currently be handled
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        var balance = new EnergyBalanceInfoSimple();
        var maximumMovementDirection = MicrobeInternalCalculations.MaximumSpeedDirection(microbeSpecies.Organelles);

        ComputeEnergyBalanceSimple(microbeSpecies.Organelles, conditions, environmentTolerances,
            microbeSpecies.MembraneType, maximumMovementDirection, false, false, worldGenerationSettings,
            CompoundAmountType.Average, cache, balance);

        float balanceModifier = 1;

        // Scale processes to not consume excess oxygen than what is actually needed. Though, see below which
        // actual processes use this modifier.
        if (balance.TotalConsumption < balance.TotalProduction)
            balanceModifier = balance.TotalConsumption / balance.TotalProduction;

        // Cleared for efficiency (not strictly required before calling ComputeActiveProcessList, but the way this is
        // called time would be wasted on trying to match up processes between different species which is unlikely to
        // work)
        processesResult.Clear();
        ComputeActiveProcessList(microbeSpecies.Organelles, ref processesResult);

        return balanceModifier;
    }

    public static float CalculateEffectiveProcessSpeedForEffect(TweakedProcess process, float balanceModifier,
        BiomeConditions conditions, float environmentalToleranceModifier)
    {
        var rate = CalculateProcessMaximumSpeed(process, environmentalToleranceModifier, conditions,
            CompoundAmountType.Biome, true);

        // Skip checking processes that cannot run
        if (rate.CurrentSpeed <= 0)
            return 0;

        // For metabolic processes the speed is at most to reach ATP equilibrium in order to not
        // unnecessarily consume environmental oxygen
        return (process.Process.IsMetabolismProcess ? balanceModifier : 1) * rate.CurrentSpeed;
    }

    /// <summary>
    ///   Since temperature works differently to other compounds, we use this method to deal with it. Logic here
    ///   is liable to be updated in the future to use alternative effect models.
    /// </summary>
    public static float CalculateTemperatureEffect(float temperature)
    {
        // Assume thermosynthetic processes are most efficient at 100°C and drop off linearly to zero
        var optimal = Constants.OPTIMAL_THERMOPLAST_TEMPERATURE;
        return Math.Clamp(temperature / optimal, 0, 2 - temperature / optimal);
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
    /// <remarks>
    ///   <para>
    ///     TODO: this takes a lot of time during the process system run so improving this performance would help
    ///     quite a lot to make the process system run faster.
    ///   </para>
    /// </remarks>
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

#if CHECK_USED_STATISTICS
        lock (usedStatistics)
        {
            usedStatistics.Clear();
        }
#endif
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var storage = ref entity.Get<CompoundStorage>();
        ref var processes = ref entity.Get<BioProcesses>();

        float overallSpeedModifier = 1.0f;

        // TODO: remove this if check once save breakage is done
        if (entity.Has<MicrobeEnvironmentalEffects>())
        {
            var microbeEnvironmentalEffects = entity.Get<MicrobeEnvironmentalEffects>();
            overallSpeedModifier = microbeEnvironmentalEffects.ProcessSpeedModifier;

            // TODO: hopefully in the far future this safety check could be removed
            // https://github.com/Revolutionary-Games/Thrive/issues/5928
            if (float.IsNaN(overallSpeedModifier) || float.IsInfinity(overallSpeedModifier) || overallSpeedModifier < 0)
            {
                GD.PrintErr(
                    $"ProcessSystem: process speed modifier is invalid for entity {entity}: {overallSpeedModifier}");
                overallSpeedModifier = 1.0f;

                // Reset the data to not keep printing the error
                microbeEnvironmentalEffects.ProcessSpeedModifier = 1.0f;
            }
        }

        // 30% bioprocess speed bonus if have nucleus
        if (!cellProperties.IsBacteria)
        {
            overallSpeedModifier *= 1.3f;
        }
        
#if DEBUG
        if (overallSpeedModifier <= 0)
        {
            // This likely causes NaN values, so we need to track down places that would cause this.
            // If desired in the future to be able to disable processing entirely, we might want to fix this case, but
            // for now this should never be allowed to happen.
            GD.PrintErr($"ProcessSystem: process speed modifier is invalid for {entity}: {overallSpeedModifier}");

            if (Debugger.IsAttached)
                Debugger.Break();
        }
#endif

        ProcessNode(ref processes, ref storage, overallSpeedModifier, delta);
    }

    private static void CalculateSimplePartOfEnergyBalance(IReadOnlyList<OrganelleTemplate> organelles,
        IBiomeConditions biome, in ResolvedMicrobeTolerances environmentTolerances, MembraneType membrane,
        Vector3 onlyMovementInDirection, bool includeMovementCost, bool isPlayerSpecies,
        WorldGenerationSettings worldSettings, CompoundAmountType amountType,
        SimulationCache? cache, EnergyBalanceInfoSimple result)
    {
        var processATPProduction = 0.0f;
        var processATPConsumption = 0.0f;
        var movementATPConsumption = 0.0f;

        int hexCount = 0;

        int organelleCount = organelles.Count;
        for (int i = 0; i < organelleCount; ++i)
        {
            var organelle = organelles[i];

            var (production, consumption) = CalculateOrganelleATPBalance(organelle, biome, amountType,
                environmentTolerances.ProcessSpeedModifier, cache);

            processATPProduction += production;
            processATPConsumption += consumption;

            // Take special cell components that take energy into account
            if (TryGetMovementCostForOrganelle(includeMovementCost, organelle, onlyMovementInDirection, out var cost))
            {
                movementATPConsumption += cost;
                result.Flagella += cost;
            }

            if (includeMovementCost && organelle.Definition.HasCiliaComponent)
            {
                var amount = Constants.CILIA_ENERGY_COST;

                movementATPConsumption += amount;
                result.Cilia += amount;
            }

            // Store hex count
            hexCount += organelle.Definition.HexCount;
        }

        var baseMovement = Constants.BASE_MOVEMENT_ATP_COST * hexCount;
        result.BaseMovement += baseMovement;

        if (includeMovementCost)
        {
            // Add movement consumption together
            result.TotalMovement += movementATPConsumption + baseMovement;
        }
        else
        {
            result.TotalMovement = -1;
        }

        // Calculate the osmoregulation
        var osmoregulation = Constants.ATP_COST_FOR_OSMOREGULATION * hexCount *
            membrane.OsmoregulationFactor;

#if DEBUG
        if (environmentTolerances.OsmoregulationModifier <= 0)
            throw new ArgumentException("Uninitialized environmental tolerances");
#endif

        osmoregulation *= environmentTolerances.OsmoregulationModifier;

        if (isPlayerSpecies)
        {
            osmoregulation *= worldSettings.OsmoregulationMultiplier;
        }

        result.Osmoregulation += osmoregulation;

        // Compute totals
        result.TotalProduction += processATPProduction;
        result.TotalConsumptionStationary += processATPConsumption + osmoregulation;

        if (includeMovementCost)
        {
            result.TotalConsumption = result.TotalConsumptionStationary + result.TotalMovement;
        }
        else
        {
            result.TotalConsumption = result.TotalConsumptionStationary;
        }

        result.FinalBalance = result.TotalProduction - result.TotalConsumption;
        result.FinalBalanceStationary = result.TotalProduction - result.TotalConsumptionStationary;
    }

    private static bool TryGetMovementCostForOrganelle(bool includeMovementCost, OrganelleTemplate organelle,
        Vector3 onlyMovementInDirection, out float movementCost)
    {
        if (!includeMovementCost || !organelle.Definition.HasMovementComponent)
        {
            movementCost = 0;
            return false;
        }

        float amount;

        if (organelle.Upgrades?.CustomUpgradeData is FlagellumUpgrades flagellumUpgrades)
        {
            amount = Constants.FLAGELLA_ENERGY_COST + flagellumUpgrades.LengthFraction
                * Constants.FLAGELLA_MAX_UPGRADE_ATP_USAGE;
        }
        else
        {
            amount = Constants.FLAGELLA_ENERGY_COST;
        }

        movementCost = amount;

        var organelleDirection = MicrobeInternalCalculations.GetOrganelleDirection(organelle);
        if (organelleDirection.Dot(onlyMovementInDirection) > 0)
        {
            return true;
        }

        return false;
    }

    private static float GetAmbientInBiome(Compound compound, IBiomeConditions biome, CompoundAmountType amountType)
    {
        if (!biome.TryGetCompound(compound, amountType, out var environmentalCompoundProperties))
            return 0;

        return environmentalCompoundProperties.Ambient;
    }

    private void ProcessNode(ref BioProcesses processor, ref CompoundStorage storage, float overallSpeedModifier,
        float delta)
    {
        var bag = storage.Compounds;

        // Set all compounds to not be useful, when some compound is used it will be marked useful
        bag.ClearUseful();

        var processStatistics = processor.ProcessStatistics;

        if (processStatistics != null)
        {
#if CHECK_USED_STATISTICS
            lock (usedStatistics)
            {
                if (usedStatistics.Contains(processStatistics))
                    throw new Exception("Re-use of process statistics detected");

                usedStatistics.Add(processStatistics);
            }
#endif

            processStatistics.MarkAllUnused();
        }

        if (processor.ActiveProcesses != null)
        {
            foreach (var process in processor.ActiveProcesses)
            {
                // If rate is 0 don't do it
                // The rate specifies how fast fraction of the specified process numbers this cell can do
                // TODO: would be nice still to report these to process statistics
                if (process.Rate <= 0.0f)
                    continue;

                // TODO: reporting duplicate process types would be nice in debug mode here

                var processData = process.Process;

                var currentProcessStatistics = processStatistics?.GetAndMarkUsed(process);

                if (currentProcessStatistics != null)
                {
                    // For some reason in Godot 4 using the process statistics without locks, freezes up the whole
                    // process (this is not the only problem, but seems to be the fastest problem to trigger and
                    // freeze the game)
                    lock (currentProcessStatistics)
                    {
                        // Apply enabled status as this is not otherwise applied, this is necessary as the data is
                        // a copy of a struct in the speed statistics so it doesn't get the updated value as the
                        // equality comparison doesn't check the speed (because if it did that would break consistent
                        // order between pause/resume of a process in the process panel)
                        currentProcessStatistics.UpdateProcessDataIfNeeded(process);

                        currentProcessStatistics.BeginFrame(delta);
                        RunProcess(delta, processData, bag, process, ref processor, overallSpeedModifier,
                            currentProcessStatistics);
                    }
                }
                else
                {
                    RunProcess(delta, processData, bag, process, ref processor, overallSpeedModifier, null);
                }
            }
        }

        bag.ClampNegativeCompoundAmounts();
        bag.FixNaNCompounds();

        processStatistics?.RemoveUnused();
    }

    private void RunProcess(float delta, BioProcess processData, CompoundBag bag, TweakedProcess process,
        ref BioProcesses processorInfo, float overallSpeedModifier, SingleProcessStatistics? currentProcessStatistics)
    {
        // Bool for can your cell do the process
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
            var inputCompound = entry.Key.ID;

            // Set used compounds to be useful, we don't want to purge those
            bag.SetUseful(inputCompound);

            // TODO: would there be a more performant way to check if compound is environmental?
            // Maybe by modifying the process inputs and outputs to use CompoundDefinition rather than plain compound?
            if (!entry.Key.IsEnvironmental)
                continue;

            // Processing runs on the current game time following values
            var ambient = GetAmbient(inputCompound, CompoundAmountType.Current);

            // currentProcessStatistics?.AddInputAmount(entry.Key, entry.Value * inverseDelta);
            currentProcessStatistics?.AddInputAmount(inputCompound, ambient);

            // do environmental modifier here, and save it for later
            environmentModifier *= inputCompound == Compound.Temperature ?
                CalculateTemperatureEffect(ambient) :
                ambient / (entry.Value * overallSpeedModifier);

            if (environmentModifier <= MathUtils.EPSILON)
                currentProcessStatistics?.AddLimitingFactor(inputCompound);
        }

        if (environmentModifier <= MathUtils.EPSILON)
            canDoProcess = false;

        if (process.SpeedMultiplier <= 0)
        {
            canDoProcess = false;
        }
        else if (process.SpeedMultiplier > 1)
        {
            process.SpeedMultiplier = 1;
        }

        // Compute spaceConstraintModifier before updating the final use and input amounts
        foreach (var entry in processData.Inputs)
        {
            if (entry.Key.IsEnvironmental)
                continue;

            var inputCompound = entry.Key.ID;

            var inputRemoved = entry.Value * process.Rate * environmentModifier * process.SpeedMultiplier *
                overallSpeedModifier;

            // currentProcessStatistics?.AddInputAmount(entry.Key, 0);
            // We don't multiply by delta here because we report the per-second values anyway. In the actual
            // process output numbers (computed after testing the speed), we need to multiply by inverse delta
            currentProcessStatistics?.AddInputAmount(inputCompound, inputRemoved);

            inputRemoved = inputRemoved * delta * spaceConstraintModifier;

            // If not enough we can't run the process unless we can lower spaceConstraintModifier enough
            var availableAmount = bag.GetCompoundAmount(inputCompound);
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

                        // Due to rounding errors there can be very small disparity here between the amount
                        // available and what we will take with the modifiers. See the comment in outputs for
                        // more details
                    }
                }

                if (!canRun)
                {
                    canDoProcess = false;
                    currentProcessStatistics?.AddLimitingFactor(inputCompound);
                }
            }
        }

        bool isATPProducer = false;

        foreach (var entry in processData.Outputs)
        {
            var outputCompound = entry.Key.ID;

            // For now, lets assume compounds we produce are also useful
            bag.SetUseful(outputCompound);

            var outputAdded = entry.Value * process.Rate * environmentModifier * process.SpeedMultiplier *
                overallSpeedModifier;

            // currentProcessStatistics?.AddOutputAmount(entry.Key, 0);
            currentProcessStatistics?.AddOutputAmount(outputCompound, outputAdded);

            outputAdded = outputAdded * delta * spaceConstraintModifier;

            // if environmental right now, this isn't released anywhere
            if (entry.Key.IsEnvironmental)
                continue;

            // If no space we can't do the process, if we can't adjust the space constraint modifier enough
            var remainingSpace = bag.GetCapacityForCompound(outputCompound) - bag.GetCompoundAmount(outputCompound);
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

                    // With all the modifiers we can lose a tiny bit of compound that won't fit due to rounding
                    // errors, but we ignore that here
                }

                if (!canRun)
                {
                    canDoProcess = false;
                    currentProcessStatistics?.AddCapacityProblem(outputCompound);
                }
            }

            if (outputCompound == Compound.ATP)
                isATPProducer = true;
        }

        // Only carry out this process if you have all the required ingredients and enough space for the outputs
        if (!canDoProcess)
        {
            if (currentProcessStatistics != null)
                currentProcessStatistics.CurrentSpeed = 0;
            return;
        }

        float totalModifier = process.Rate * delta * environmentModifier * spaceConstraintModifier *
            process.SpeedMultiplier * overallSpeedModifier;

        // Apply ATP production speed cap if in effect
        if (isATPProducer && processorInfo.ATPProductionSpeedModifier != 0)
        {
            // TODO: should external modifier effects be shown in the process info somehow?

            if (processorInfo.ATPProductionSpeedModifier < 0)
            {
                // Process is disabled
                if (currentProcessStatistics != null)
                    currentProcessStatistics.CurrentSpeed = 0;
                return;
            }

            totalModifier *= processorInfo.ATPProductionSpeedModifier;
        }

        if (currentProcessStatistics != null)
        {
            // TODO: should the overall speed modifier be included in here? It already has scaled the inputs and
            // outputs
            currentProcessStatistics.CurrentSpeed = process.Rate * environmentModifier * spaceConstraintModifier *
                process.SpeedMultiplier * overallSpeedModifier;
        }

        // Consume inputs
        foreach (var entry in processData.Inputs)
        {
            if (entry.Key.IsEnvironmental)
                continue;

            var inputCompound = entry.Key.ID;

            var inputRemoved = entry.Value * totalModifier;

            currentProcessStatistics?.AddInputAmount(inputCompound, inputRemoved * inverseDelta);

            // This should always succeed (due to the earlier check), so it is always assumed here that this
            // succeeded. Caveat: see: BioProcesses.ATPProductionSpeedModifier
            bag.TakeCompound(inputCompound, inputRemoved);
        }

        // Add outputs
        foreach (var entry in processData.Outputs)
        {
            if (entry.Key.IsEnvironmental)
                continue;

            var outputCompound = entry.Key.ID;

            var outputGenerated = entry.Value * totalModifier;

            currentProcessStatistics?.AddOutputAmount(outputCompound, outputGenerated * inverseDelta);

            bag.AddCompound(outputCompound, outputGenerated);
        }
    }
}
