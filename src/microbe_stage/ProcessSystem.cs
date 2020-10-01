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
    public static EnergyBalanceInfo ComputeEnergyBalance(IEnumerable<OrganelleDefinition> organelles,
        BiomeConditions biome, MembraneType membrane)
    {
        var result = new EnergyBalanceInfo();

        float processATPProduction = 0.0f;
        float processATPConsumption = 0.0f;
        float movementATPConsumption = 0.0f;

        int hexCount = 0;

        var enumerated = organelles.ToList();

        foreach (var organelle in enumerated)
        {
            foreach (var process in organelle.RunnableProcesses)
            {
                var processData = CalculateProcessMaximumSpeed(process, biome);

                if (processData.OtherInputs.ContainsKey(ATP))
                {
                    var amount = processData.OtherInputs[ATP].Amount;

                    processATPConsumption += amount;

                    result.AddConsumption(organelle.InternalName, amount);
                }

                if (processData.Outputs.ContainsKey(ATP))
                {
                    var amount = processData.Outputs[ATP].Amount;

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

    public void Process(float delta)
    {
        if (biome == null)
        {
            GD.PrintErr("ProcessSystem has no biome set");
            return;
        }

        var nodes = worldRoot.GetTree().GetNodesInGroup(Constants.PROCESS_GROUP);

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
                    ProcessNode(nodes[a] as IProcessable, delta);
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
    public void SetBiome(BiomeConditions biome)
    {
        this.biome = biome;
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

            var input = new ProcessSpeedInformation.EnvironmentalInput(entry.Key, entry.Value);

            var availableInEnvironment = GetDissolvedInBiome(entry.Key, biome);

            input.AvailableAmount = availableInEnvironment;

            // More than needed environment value boosts the effectiveness
            input.AvailableRate = availableInEnvironment / entry.Value;

            speedFactor *= input.AvailableRate;

            result.EnvironmentInputs[entry.Key] = input;
        }

        speedFactor *= process.Rate;

        // So that the speedfactor is available here
        foreach (var entry in process.Process.Inputs)
        {
            if (entry.Key.IsEnvironmental)
                continue;

            // Normal, cloud input

            var input = new ProcessSpeedInformation.CompoundAmount(entry.Key, entry.Value * speedFactor);

            result.OtherInputs.Add(entry.Key, input);
        }

        foreach (var entry in process.Process.Outputs)
        {
            var output = new ProcessSpeedInformation.CompoundAmount(entry.Key,
                entry.Value * speedFactor);

            result.Outputs[entry.Key] = output;
        }

        result.SpeedFactor = speedFactor;

        return result;
    }

    private void ProcessNode(IProcessable processor, float delta)
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

        foreach (TweakedProcess process in processor.ActiveProcesses)
        {
            // If rate is 0 dont do it
            // The rate specifies how fast fraction of the specified process
            // numbers this cell can do
            if (process.Rate <= 0.0f)
                continue;

            var processData = process.Process;

            // Can your cell do the process
            bool canDoProcess = true;

            // Loop through to make sure you can follow through with your
            // whole process so nothing gets wasted as that would be
            // frustrating, its two more for loops, yes but it should only
            // really be looping at max two or three times anyway. also make
            // sure you wont run out of space when you do add the compounds.
            // Input
            // Defaults to 1
            float environmentModifier = 1.0f;

            foreach (var entry in processData.Inputs)
            {
                // Set used compounds to be useful, we dont want to purge
                // those
                bag.SetUseful(entry.Key);

                var inputRemoved = entry.Value * process.Rate * delta;

                // TODO: It might be faster to just check if there is any
                // dissolved amount of this compound or not
                if (entry.Key.IsEnvironmental)
                {
                    // do environmental modifier here, and save it for later
                    environmentModifier *= GetDissolved(entry.Key) / entry.Value;
                }
                else
                {
                    // If not enough compound we can't do the process
                    if (bag.GetCompoundAmount(entry.Key) < inputRemoved)
                    {
                        canDoProcess = false;
                    }
                }
            }

            if (environmentModifier <= MathUtils.EPSILON)
            {
                canDoProcess = false;
            }

            // Output
            // This is now always looped (even when we can't do the process)
            // because the is useful part is needs to be always be done
            foreach (var entry in processData.Outputs)
            {
                // For now lets assume compounds we produce are also
                // useful
                bag.SetUseful(entry.Key);

                // Apply the general modifiers and
                // apply the environmental modifier
                var outputAdded = entry.Value * process.Rate * delta * environmentModifier;

                // If no space we can't do the process, and if environmental
                // right now this isn't released anywhere
                if (entry.Key.IsEnvironmental)
                {
                    continue;
                }

                if (bag.GetCompoundAmount(entry.Key) + outputAdded > bag.Capacity)
                {
                    canDoProcess = false;
                }
            }

            // Only carry out this process if you have all the required
            // ingredients and enough space for the outputs
            if (!canDoProcess)
                continue;

            // Inputs.
            foreach (var entry in processData.Inputs)
            {
                // TODO: It might be faster to just check if there is any
                // dissolved amount of this compound or not
                if (entry.Key.IsEnvironmental)
                    continue;

                // Note: the environment modifier is applied here, but not
                // when checking if we have enough compounds. So sometimes
                // we might not run a process when we actually would have
                // enough compounds to run it.
                // TODO: the reverse is also probably true, ie. we can run a process we shouldn't be able to due to the
                // environmental modifier boosting its speed
                var inputRemoved = entry.Value * process.Rate * delta *
                    environmentModifier;

                // This should always succeed (due to the earlier check) so
                // it is always assumed here that the process succeeded
                bag.TakeCompound(entry.Key, inputRemoved);
            }

            // Outputs.
            foreach (var entry in processData.Outputs)
            {
                if (entry.Key.IsEnvironmental)
                    continue;

                var outputGenerated = entry.Value * process.Rate * delta *
                    environmentModifier;

                bag.AddCompound(entry.Key, outputGenerated);
            }
        }

        bag.ClampNegativeCompoundAmounts();
    }
}
