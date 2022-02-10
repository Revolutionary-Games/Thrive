using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Runs actions on the main thread before the next update
/// </summary>
public class Invoke : Node
{
    private static Invoke? instance;

    private readonly BlockingCollection<Action> queuedInvokes = new();
    private readonly BlockingCollection<Action> nextFrameInvokes = new();
    private readonly List<Action> tempActionList = new();

    private Invoke()
    {
        instance = this;

        PauseMode = PauseModeEnum.Process;
        ProcessPriority = -1000;
    }

    public static Invoke Instance => instance ?? throw new InstanceNotLoadedYetException();

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
            // TODO: would be nice to have a more explicit system to skip already disposed objects from being in the
            // invoke queue. https://github.com/Revolutionary-Games/Thrive/issues/2477
            try
            {
                action.Invoke();
            }
            catch (ObjectDisposedException e)
            {
                GD.PrintErr("An invoke target is already disposed: ", e);
            }
        }

        tempActionList.Clear();

        // And then run the actions that are allowed to run as soon as possible
        while (queuedInvokes.TryTake(out Action action))
        {
            try
            {
                action.Invoke();
            }
            catch (ObjectDisposedException e)
            {
                GD.PrintErr("An invoke target is already disposed: ", e);
            }
        }
    }
}
