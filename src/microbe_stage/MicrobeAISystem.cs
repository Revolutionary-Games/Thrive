using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public class MicrobeAISystem
{
    private readonly List<Task> tasks = new();

    private readonly Node worldRoot;

    // Because this is run in a threaded environment (and because this is the AI), this should
    // NEVER call a data changing method from this class
    private readonly CompoundCloudSystem clouds;

    public MicrobeAISystem(Node worldRoot, CompoundCloudSystem cloudSystem)
    {
        this.worldRoot = worldRoot;
        clouds = cloudSystem;
    }

    public void Process(float delta)
    {
        if (CheatManager.NoAI)
            return;

        var nodes = worldRoot.GetTree().GetNodesInGroup(Constants.AI_GROUP);

        // TODO: it would be nice to only rebuild these lists if some AI think interval has elapsed and these are needed
        var allMicrobes = worldRoot.GetTree().GetNodesInGroup(Constants.AI_TAG_MICROBE);
        var allChunks = worldRoot.GetTree().GetNodesInGroup(Constants.AI_TAG_CHUNK);

        var data = new MicrobeAICommonData(allMicrobes.Cast<Microbe>().ToList(),
            allChunks.Cast<FloatingChunk>().ToList(), clouds);

        // The objects are processed here in order to take advantage of threading
        var executor = TaskExecutor.Instance;

        for (int i = 0; i < nodes.Count; i += Constants.MICROBE_AI_OBJECTS_PER_TASK)
        {
            int start = i;

            var task = new Task(() =>
            {
                var random = new Random();
                for (int a = start;
                     a < start + Constants.MICROBE_AI_OBJECTS_PER_TASK && a < nodes.Count;
                     ++a)
                {
                    RunAIFor(nodes[a] as IMicrobeAI, delta, random, data);
                }
            });

            tasks.Add(task);
        }

        // Start and wait for tasks to finish
        executor.RunTasks(tasks);
        tasks.Clear();
    }

    /// <summary>
    ///   Main AI think function for cells
    /// </summary>
    /// <param name="ai">The thing with AI interface implemented</param>
    /// <param name="delta">Passed time</param>
    /// <param name="random">Randomness source</param>
    /// <param name="data">Common data for AI agents, should not be modified</param>
    private void RunAIFor(IMicrobeAI? ai, float delta, Random random, MicrobeAICommonData data)
    {
        if (ai == null)
        {
            GD.PrintErr("A node has been put in the ai group but it isn't derived from IMicrobeAI");
            return;
        }

        // Limit how often the AI is run
        ai.TimeUntilNextAIUpdate -= delta;

        if (ai.TimeUntilNextAIUpdate > 0)
            return;

        ai.TimeUntilNextAIUpdate = Constants.MICROBE_AI_THINK_INTERVAL;

        ai.AIThink(delta, random, data);
    }
}
