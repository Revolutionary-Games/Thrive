using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Manages spawning and processing compound clouds
/// </summary>
public class CompoundCloudSystem : Node
{
    private readonly List<Task> tasks = new List<Task>();

    private List<CompoundCloudPlane> clouds = new List<CompoundCloudPlane>();

    public override void _Ready()
    {
        foreach (var child in GetChildren())
        {
            clouds.Add((CompoundCloudPlane)child);
        }

        if (clouds.Count != 9)
            GD.PrintErr("CompoundCloudSystem doesn't have 9 child cloud objects");
    }

    public override void _Process(float delta)
    {
        // The clouds are processed here in order to take advantage of threading

        // The first cloud is processed on the main thread
        bool first = true;

        var executor = TaskExecutor.Instance;

        foreach (var cloud in clouds)
        {
            var task = new Task(() => cloud.UpdateCloud(delta));

            tasks.Add(task);

            if (!first)
            {
                executor.AddTask(task);
            }

            first = false;
        }

        // Run the first task on this thread
        tasks[0].RunSynchronously();

        // Wait for all tasks to complete
        foreach (var task in tasks)
        {
            task.Wait();
        }

        // TODO: moving compounds between next to each other clouds

        // Update the cloud textures
        foreach (var cloud in clouds)
        {
            cloud.UploadTexture();
        }

        tasks.Clear();
    }

    /// <summary>
    ///   Resets the cloud contents and positions
    /// </summary>
    public void Init()
    {
    }
}
