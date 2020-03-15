using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Runs processes in parallel on entities
/// </summary>
public class ProcessSystem
{
    private readonly List<Task> tasks = new List<Task>();

    private readonly Node worldRoot;
    private Biome biome;

    public ProcessSystem(Node worldRoot)
    {
        this.worldRoot = worldRoot;
    }

    /// <summary>
    ///   Computes the process numbers for given organelles given the active biome data
    /// </summary>
    public static string ComputeOrganelleProcessEfficiencies(
        List<OrganelleDefinition> organelles, Biome biome)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///   Computes the energy balance for the given organelles in biome
    /// </summary>
    public static string ComputeEnergyBalance(List<OrganelleDefinition> organelles,
        Biome biome, MembraneType membrane)
    {
        throw new NotImplementedException();
    }

    public void Process(float delta)
    {
        if (biome == null)
        {
            GD.PrintErr("ProcesSystem has no biome set");
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
    public void SetBiome(Biome biome)
    {
        this.biome = biome;
    }

    /// <summary>
    ///   Get the amount of environmental compound
    /// </summary>
    public float GetDissolved(string compoundName)
    {
        if (!biome.Compounds.ContainsKey(compoundName))
            return 0;
        return biome.Compounds[compoundName].Dissolved;
    }

    private void ProcessNode(IProcessable processor, float delta)
    {
        if (processor == null)
        {
            GD.PrintErr("A node has been put in the process group " +
                "but it isn't derived from IProcessable");
            return;
        }

        var simulation = SimulationParameters.Instance;

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
            // frusterating, its two more for loops, yes but it should only
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
                if (simulation.GetCompound(entry.Key).IsEnvironmental)
                {
                    // do environmental modifier here, and save it for later
                    environmentModifier *= GetDissolved(entry.Key) / entry.Value;
                }
                else
                {
                    // If not enough compound we can't do the process
                    if (bag.Compounds[entry.Key] < inputRemoved)
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
                // right now this isnt released anywhere
                if (simulation.GetCompound(entry.Key).IsEnvironmental)
                {
                    continue;
                }

                if (bag.Compounds[entry.Key] + outputAdded > bag.Capacity)
                {
                    canDoProcess = false;
                }
            }

            // Only carry out this process if you have all the required
            // ingredients and enough space for the outputs
            if (canDoProcess)
            {
                // Inputs.
                foreach (var entry in processData.Inputs)
                {
                    // TODO: It might be faster to just check if there is any
                    // dissolved amount of this compound or not
                    if (simulation.GetCompound(entry.Key).IsEnvironmental)
                        continue;

                    // Note: the enviroment modifier is applied here, but not
                    // when checking if we have enough compounds. So sometimes
                    // we might not run a process when we actually would have
                    // enough compounds to run it
                    var inputRemoved = entry.Value * process.Rate * delta *
                        environmentModifier;

                    // This should always be true (due to the earlier check) so
                    // it is always assumed here that the process succeeded
                    if (bag.Compounds[entry.Key] >= inputRemoved)
                    {
                        bag.Compounds[entry.Key] -= inputRemoved;
                    }
                }

                // Outputs.
                foreach (var entry in processData.Outputs)
                {
                    if (simulation.GetCompound(entry.Key).IsEnvironmental)
                        continue;

                    var outputGenerated = entry.Value * process.Rate * delta *
                        environmentModifier;

                    bag.Compounds[entry.Key] += outputGenerated;
                }
            }
        }

        bag.ClampNegativeCompoundAmounts();
    }
}
