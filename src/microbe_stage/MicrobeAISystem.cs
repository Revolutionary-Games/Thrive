using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class MicrobeAISystem
{
    private readonly List<Task> tasks = new List<Task>();

    private readonly Node worldRoot;

    public MicrobeAISystem(Node worldRoot)
    {
        this.worldRoot = worldRoot;
    }

    public void Process(float delta)
    {
        var nodes = worldRoot.GetTree().GetNodesInGroup(Constants.AI_GROUP);

        // The objects are processed here in order to take advantage of threading
        var executor = TaskExecutor.Instance;

        for (int i = 0; i < nodes.Count; i += Constants.MICROBE_AI_OBJECTS_PER_TASK)
        {
            int start = i;

            var task = new Task(() =>
            {
                var random = new Random();
                for (int a = start;
                     a < start + Constants.MICROBE_AI_OBJECTS_PER_TASK && a < nodes.Count; ++a)
                {
                    RunAIFor(nodes[a] as IMicrobeAI, delta, random);
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
    private void RunAIFor(IMicrobeAI ai, float delta, Random random)
    {
        if (ai == null)
        {
            GD.PrintErr("A node has been put in the ai group " +
                "but it isn't derived from IMicrobeAI");
            return;
        }

        // Limit how often the AI is run
        ai.TimeUntilNextAIUpdate -= delta;

        if (ai.TimeUntilNextAIUpdate > 0)
            return;

        ai.TimeUntilNextAIUpdate = Constants.MICROBE_AI_THINK_INTERVAL;

        // Run the actual AI logic here
        Microbe microbe = (Microbe)ai;

        // For now just set a random nearby look at location
        microbe.LookAtPoint = microbe.Translation + new Vector3(
            random.Next(-100, 101), 0, random.Next(-100, 101));

        // And random movement speed
        microbe.MovementDirection = new Vector3(0, 0, (float)(-1 * random.NextDouble()));
    }
}
