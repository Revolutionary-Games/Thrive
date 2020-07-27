using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Runs an actions on the main thread before the next update
/// </summary>
public class Invoke : Node
{
    private static Invoke instance;

    private readonly BlockingCollection<Action> queuedInvokes = new BlockingCollection<Action>();
    private readonly BlockingCollection<Action> nextFrameInvokes = new BlockingCollection<Action>();
    private readonly List<Action> tempActionList = new List<Action>();

    private Invoke()
    {
        instance = this;

        PauseMode = PauseModeEnum.Process;
        ProcessPriority = -1000;
    }

    public static Invoke Instance => instance;

    /// <summary>
    ///   Queues an action to run on the next frame
    /// </summary>
    /// <param name="action">Action to run</param>
    public void Queue(Action action)
    {
        nextFrameInvokes.Add(action);
    }

    /// <summary>
    ///   Performs an action as soon as possible, doesn't wait until next frame
    /// </summary>
    /// <param name="action">The action to perform</param>
    public void Perform(Action action)
    {
        queuedInvokes.Add(action);
    }

    public override void _Process(float delta)
    {
        // Move the queued invokes to a temp list
        while (nextFrameInvokes.TryTake(out Action tmp))
        {
            tempActionList.Add(tmp);
        }

        // Run the temp list actions first to make sure that their Perform calls would work
        foreach (var action in tempActionList)
        {
            action.Invoke();
        }

        tempActionList.Clear();

        // And then run the actions that are allowed to run as soon as possible
        while (queuedInvokes.TryTake(out Action action))
        {
            action.Invoke();
        }
    }
}
