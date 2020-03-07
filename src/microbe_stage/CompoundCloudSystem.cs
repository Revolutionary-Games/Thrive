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

    private int neededCloudsAtOnePosition;

    private List<CompoundCloudPlane> clouds = new List<CompoundCloudPlane>();
    private PackedScene cloudScene;

    public override void _Ready()
    {
        cloudScene = GD.Load<PackedScene>("res://src/microbe_stage/CompoundCloudPlane.tscn");
    }

    public override void _Process(float delta)
    {
        PositionClouds();
        UpdateCloudContents(delta);
    }

    /// <summary>
    ///   Resets the cloud contents and positions
    /// </summary>
    public void Init()
    {
        var config = SimulationParameters.Instance;

        clouds.Clear();

        foreach (var child in GetChildren())
        {
            clouds.Add((CompoundCloudPlane)child);
        }

        var compounds = config.GetCloudCompounds();

        // Count the number of clouds needed at one position from the loaded compound types
        neededCloudsAtOnePosition = (int)Math.Ceiling(compounds.Count / 4.0f);

        // We need to dynamically spawn more / delete some if this doesn't match
        while (clouds.Count < 9 * neededCloudsAtOnePosition)
        {
            var createdCloud = (CompoundCloudPlane)cloudScene.Instance();
            clouds.Add(createdCloud);
            AddChild(createdCloud);
        }

        while (clouds.Count > 9 * neededCloudsAtOnePosition)
        {
            var cloud = clouds[0];
            RemoveChild(cloud);
            cloud.Free();
            clouds.Remove(cloud);
        }

        for (int i = 0; i < clouds.Count; i += neededCloudsAtOnePosition)
        {
            Compound cloud1;
            Compound cloud2 = null;
            Compound cloud3 = null;
            Compound cloud4 = null;

            int startOffset = (i % neededCloudsAtOnePosition) * neededCloudsAtOnePosition;

            cloud1 = compounds[startOffset + 0];

            if (startOffset + 1 < compounds.Count)
                cloud2 = compounds[startOffset + 1];

            if (startOffset + 2 < compounds.Count)
                cloud3 = compounds[startOffset + 2];

            if (startOffset + 3 < compounds.Count)
                cloud4 = compounds[startOffset + 3];

            clouds[i].Init(cloud1, cloud2, cloud3, cloud4);
        }

        PositionClouds();
    }

    private void PositionClouds()
    {
    }

    private void UpdateCloudContents(float delta)
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
}
