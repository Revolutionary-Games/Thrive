using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   General implementation of an action history and undo / redo for use by editors
/// </summary>
/// <typeparam name="T">Type of actions to hold</typeparam>
public abstract class ActionHistory<T>
    where T : ReversibleAction
{
    [JsonProperty]
    protected List<T> actions = new();

    /// <summary>
    ///   marks the last action that has been done (not undone, but
    ///   possibly redone), is 0 if there is none.
    /// </summary>
    [JsonProperty]
    protected int actionIndex;

    public bool CanRedo()
    {
        return actionIndex < actions.Count;
    }

    public bool CanUndo()
    {
        return actionIndex > 0;
    }

    public virtual bool Redo()
    {
        if (!CanRedo())
            return false;

        actions[actionIndex++].Perform();

        return true;
    }

    public virtual bool Undo()
    {
        if (!CanUndo())
            return false;

        actions[--actionIndex].Undo();

        return true;
    }

    /// <summary>
    ///   Adds a new action and performs it
    /// </summary>
    public virtual void AddAction(T action)
    {
        // Throw away old actions if we are not at the end of the action list
        while (actionIndex < actions.Count)
            actions.RemoveAt(actions.Count - 1);

        if (actionIndex != actions.Count)
            throw new Exception("action history logic is wrong");

        action.Perform();
        actions.Add(action);
        ++actionIndex;
    }
}
