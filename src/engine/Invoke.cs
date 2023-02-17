using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Godot;
using Object = Godot.Object;

/// <summary>
///   Runs actions on the main thread before the next update
/// </summary>
public class Invoke : Node
{
    private static Invoke? instance;

    private readonly BlockingCollection<Action> queuedInvokes = new();
    private readonly BlockingCollection<Action> nextFrameInvokes = new();
    private readonly List<Action> tempActionList = new();

    private bool disposed;

    private Invoke()
    {
        instance = this;

        PauseMode = PauseModeEnum.Process;
        ProcessPriority = -1000;
    }

    public static Invoke Instance => instance ?? throw new InstanceNotLoadedYetException();

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

    /// <summary>
    ///   Queues an action to run on the next frame
    /// </summary>
    /// <param name="action">Action to run</param>
    public void Queue(Action action)
    {
        if (disposed)
        {
            GD.PrintErr("Invoke is already disposed, cannot queue an action");
            return;
        }

        nextFrameInvokes.Add(action);
    }

    /// <summary>
    ///   Queues an action to run on the next frame for a Godot reference typed object. This variant can avoid disposed
    ///   exceptions when running on Godot objects
    /// </summary>
    /// <param name="action">Action to run</param>
    /// <param name="forObject">
    ///   Object the action is for. The action will be skipped automatically if the object is destroyed before the
    ///   action is ran
    /// </param>
    /// <param name="logDispose">
    ///   If true then a log message is printed if <see cref="forObject"/> is disposed before the action is ran
    /// </param>
    public void QueueForObject(Action action, Object forObject, bool logDispose = false)
    {
        if (disposed)
        {
            GD.PrintErr("Invoke is already disposed, cannot queue an action for object");
            return;
        }

        var skippableInvoke = new SkippableDisposedInvoke(action, forObject, logDispose);

        nextFrameInvokes.Add(() => skippableInvoke.Run());
    }

    /// <summary>
    ///   Performs an action as soon as possible, doesn't wait until next frame
    /// </summary>
    /// <param name="action">The action to perform</param>
    public void Perform(Action action)
    {
        if (disposed)
        {
            GD.PrintErr("Invoke is already disposed, cannot perform an action");
            return;
        }

        queuedInvokes.Add(action);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            disposed = true;

            queuedInvokes.Dispose();
            nextFrameInvokes.Dispose();
        }

        base.Dispose(disposing);
    }

    private class SkippableDisposedInvoke
    {
        private readonly Action underlyingAction;
        private readonly Object objectToCheck;
        private readonly bool logFailure;

        public SkippableDisposedInvoke(Action underlyingAction, Object objectToCheck, bool logFailure)
        {
            this.underlyingAction = underlyingAction;
            this.objectToCheck = objectToCheck;
            this.logFailure = logFailure;
        }

        public void Run()
        {
            // For now rely on asking Godot if the instance is valid (there doesn't seem to be easy access to the flag
            // on the object to see if the disposed flag is set, though we could call something like GetInstanceId and
            // see if that throws disposed exception)
            if (!IsInstanceValid(objectToCheck))
            {
                if (logFailure)
                {
                    GD.Print("Object of type ", objectToCheck.GetType().FullName,
                        " was already disposed before invoke");
                }

                return;
            }

            underlyingAction.Invoke();
        }
    }
}
