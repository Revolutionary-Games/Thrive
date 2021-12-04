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
    private readonly List<Task> tasks = new List<Task>();

    private readonly Node worldRoot;
    private BiomeConditions biome;

    public ProcessSystem(Node worldRoot)
    {
        this.worldRoot = worldRoot;
    }

    /// <summary>
    ///   Computes the process efficiency numbers for given organelles
    ///   given the active biome data.
    /// </summary>
    public static Dictionary<string, OrganelleEfficiency> ComputeOrganelleProcessEfficiencies(
        IEnumerable<OrganelleDefinition> organelles, BiomeConditions biome)
    {
        var result = new Dictionary<string, OrganelleEfficiency>();

        foreach (var organelle in organelles)
        {
            var info = new OrganelleEfficiency(organelle);

            foreach (var process in organelle.RunnableProcesses)
            {
                info.Processes.Add(CalculateProcessMaximumSpeed(process, biome));
            }

            result[organelle.InternalName] = info;
        }

        return result;
    }

    /// <summary>
    ///   Computes the energy balance for the given organelles in biome
    /// </summary>
    public static EnergyBalanceInfo ComputeEnergyBalance(IEnumerable<OrganelleTemplate> organelles,
        BiomeConditions biome, MembraneType membrane)
    {
        var maximumMovementDirection = MicrobeInternalCalculations.MaximumSpeedDirection(organelles);
        return ComputeEnergyBalance(organelles, biome, membrane, maximumMovementDirection);
    }

    /// <summary>
    ///   Computes the energy balance for the given organelles in biome
    /// </summary>
    public static EnergyBalanceInfo ComputeEnergyBalance(IEnumerable<OrganelleTemplate> organelles,
        BiomeConditions biome, MembraneType membrane, Vector3 maximumMovementDirection)
    {
        var result = new EnergyBalanceInfo();

        float processATPProduction = 0.0f;
        float processATPConsumption = 0.0f;
        float movementATPConsumption = 0.0f;

        int hexCount = 0;

        var organelleList = organelles.ToList();
        var enumerated = (organelles.Select(o => o.Definition)).ToList();
        
        foreach (var organelle in enumerated)
        {
            foreach (var process in organelle.RunnableProcesses)
            {
                var processData = CalculateProcessMaximumSpeed(process, biome);

                if (processData.WritableInputs.ContainsKey(ATP))
                {
                    var amount = processData.WritableInputs[ATP];

                    processATPConsumption += amount;

                    result.AddConsumption(organelle.InternalName, amount);
                }

                if (processData.WritableOutputs.ContainsKey(ATP))
                {
                    var amount = processData.WritableOutputs[ATP];

                    processATPProduction += amount;

                    result.AddProduction(organelle.InternalName, amount);
                }
            }

            // Take special cell components that take energy into account
            if (organelle.HasComponentFactory<MovementComponentFactory>())
            {
                var amount = Constants.FLAGELLA_ENERGY_COST;

                movementATPConsumption += amount;
                result.Flagella += amount;

                var templateOrganelle = organelleList[enumerated.IndexOf(organelle)];
                var organelleDirection = MicrobeInternalCalculations.GetOrganelleDirection(templateOrganelle);
                if (organelleDirection.Dot(maximumMovementDirection) > 0)
                    result.AddConsumption(organelle.InternalName, amount);
            }

            // Store hex count
            hexCount += organelle.HexCount;
        }

        // Add movement consumption together
        result.BaseMovement = Constants.BASE_MOVEMENT_ATP_COST * hexCount;
        result.AddConsumption("baseMovement", result.BaseMovement);
        var totalMovementConsumption = movementATPConsumption + result.BaseMovement;

        // Add osmoregulation
        result.Osmoregulation = Constants.ATP_COST_FOR_OSMOREGULATION * hexCount *
            membrane.OsmoregulationFactor;

        result.AddConsumption("osmoregulation", result.Osmoregulation);

        // Compute totals
        result.TotalProduction = processATPProduction;
        result.TotalConsumptionStationary = processATPConsumption + result.Osmoregulation;
        result.TotalConsumption = result.TotalConsumptionStationary + totalMovementConsumption;

        result.FinalBalance = result.TotalProduction - result.TotalConsumption;
        result.FinalBalanceStationary = result.TotalProduction - result.TotalConsumptionStationary;

        return result;
    }

    /// <summary>
    ///   Computes the compound balances for given organelle list in a patch
    /// </summary>
    public static Dictionary<Compound, CompoundBalance> ComputeCompoundBalance(
        IEnumerable<OrganelleDefinition> organelles, BiomeConditions biome)
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
                var speedAdjusted = CalculateProcessMaximumSpeed(process, biome);

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
        IEnumerable<OrganelleTemplate> organelles, BiomeConditions biome)
    {
        return ComputeCompoundBalance(organelles.Select(o => o.Definition), biome);
    }

    public void Process(float delta)
    {
        // Guard against Godot delta problems. https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        if (biome == null)
        {
            GD.PrintErr("ProcessSystem has no biome set");
            return;
        }

        var nodes = worldRoot.GetTree().GetNodesInGroup(Constants.PROCESS_GROUP);

        // Used to go from the calculated compound values to per second values for reporting statistics
        float inverseDelta = 1.0f / delta;

        // The objects are processed here in order to take advantage of threading
        var executor = TaskExecutor.Instance;

        for (int i = 0; i < nodes.Count; i += Constants.PROCESS_OBJECTS_PER_TASK)
        {
            int start = i;

            var task = new Task(() =>
            {
                for (int a = start;
                    a < start + Constants.PROCESS_OBJECTS_PER_TASK && a < nodes.Count; ++a)
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
    ///   Get the amount of environmental compound
    /// </summary>
    public float GetDissolved(Compound compound)
    {
        return GetDissolvedInBiome(compound, biome);
    }

    private static float GetDissolvedInBiome(Compound compound, BiomeConditions biome)
    {
        if (!biome.Compounds.ContainsKey(compound))
            return 0;

        return biome.Compounds[compound].Dissolved;
    }

    /// <summary>
    ///   Calculates the maximum speed a process can run at in a biome
    ///   based on the environmental compounds.
    /// </summary>
    private static ProcessSpeedInformation CalculateProcessMaximumSpeed(TweakedProcess process,
        BiomeConditions biome)
    {
        var result = new ProcessSpeedInformation(process.Process);

        float speedFactor = 1.0f;

        // Environmental inputs need to be processed first
        foreach (var entry in process.Process.Inputs)
        {
            if (!entry.Key.IsEnvironmental)
                continue;

            // Environmental compound that can limit the rate

            var availableInEnvironment = GetDissolvedInBiome(entry.Key, biome);

            var availableRate = availableInEnvironment / entry.Value;

            result.AvailableAmounts[entry.Key] = availableInEnvironment;

            // More than needed environment value boosts the effectiveness
            result.AvailableRates[entry.Key] = availableRate;

            speedFactor *= availableRate;

            result.WritableInputs[entry.Key] = entry.Value;
        }

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

    private void ProcessNode(IProcessable processor, float delta, float inverseDelta)
    {
        if (processor == null)
        {
            GD.PrintErr("A node has been put in the process group " +
                "but it isn't derived from IProcessable");
            return;
        }

        var bag = processor.ProcessCompoundStorage;

        // Set all compounds to not be useful, when some compound is
        // used it will be marked useful
        bag.ClearUseful();

        var processStatistics = processor.ProcessStatistics;

        processStatistics?.MarkAllUnused();

        foreach (TweakedProcess process in processor.ActiveProcesses)
        {
            // If rate is 0 dont do it
            // The rate specifies how fast fraction of the specified process
            // numbers this cell can do
            // TODO: would be nice still to report these to process statistics
            if (process.Rate <= 0.0f)
                continue;

            var processData = process.Process;

            var currentProcessStatistics = processStatistics?.GetAndMarkUsed(process);
            currentProcessStatistics?.BeginFrame(delta);

            RunProcess(delta, processData, bag, process, currentProcessStatistics, inverseDelta);
        }

        bag.ClampNegativeCompoundAmounts();

        processStatistics?.RemoveUnused();
    }

    private void RunProcess(float delta, BioProcess processData, CompoundBag bag, TweakedProcess process,
        SingleProcessStatistics currentProcessStatistics, float inverseDelta)
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

            var dissolved = GetDissolved(entry.Key);

            // currentProcessStatistics?.AddInputAmount(entry.Key, entry.Value * inverseDelta);
            currentProcessStatistics?.AddInputAmount(entry.Key, dissolved);

            // do environmental modifier here, and save it for later
            environmentModifier *= dissolved / entry.Value;

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
            var remainingSpace = bag.Capacity - bag.GetCompoundAmount(entry.Key);
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
