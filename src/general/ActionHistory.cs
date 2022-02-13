using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   General implementation of an action history and undo / redo for use by editors
/// </summary>
/// <typeparam name="T">Type of actions to hold</typeparam>
public class ActionHistory<T>
    where T : ReversibleAction
{
    /// <summary>
    ///   marks the last action that has been done (not undone, but
    ///   possibly redone), is 0 if there is none.
    /// </summary>
    [JsonProperty]
    protected int ActionIndex { get; private set; }

    [JsonProperty]
    protected List<T> Actions { get; private set; } = new();

    public bool CanRedo()
    {
        return ActionIndex < Actions.Count;
    }

    public bool CanUndo()
    {
        return ActionIndex > 0;
    }

    public virtual bool Redo()
    {
        if (!CanRedo())
            return false;

        Actions[ActionIndex++].Perform();

        return true;
    }

    public virtual bool Undo()
    {
        if (!CanUndo())
            return false;

        Actions[--ActionIndex].Undo();

        return true;
    }

    /// <summary>
    ///   Adds a new action and performs it
    /// </summary>
    public virtual void AddAction(T action)
    {
        // Throw away old actions if we are not at the end of the action list
        while (ActionIndex < Actions.Count)
            Actions.RemoveAt(Actions.Count - 1);

        if (ActionIndex != Actions.Count)
            throw new Exception("action history logic is wrong");

        action.Perform();
        Actions.Add(action);
        ++ActionIndex;
    }
}
