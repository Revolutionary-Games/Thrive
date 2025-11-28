using System;
using System.Collections.Generic;
using Godot;
using Saving.Serializers;
using SharedBase.Archive;

/// <summary>
///   General implementation of an action history and undo / redo for use by editors
/// </summary>
/// <typeparam name="T">Type of actions to hold</typeparam>
public class ActionHistory<T> : IArchivable
    where T : ReversibleAction
{
    public ActionHistory()
    {
        Actions = new List<T>();
    }

    protected ActionHistory(List<T> actions, int actionIndex)
    {
        Actions = actions;
        ActionIndex = actionIndex;
    }

    public virtual ushort CurrentArchiveVersion => ActionHistorySerializer.SERIALIZATION_VERSION;

    public virtual ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.ExtendedActionHistory;

    public bool CanBeReferencedInArchive => false;

    /// <summary>
    ///   marks the last action that has been done (not undone, but possibly redone), is 0 if there is none.
    /// </summary>
    protected int ActionIndex { get; private set; }

    protected List<T> Actions { get; private set; }

    public virtual void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(ActionIndex);
        writer.WriteObject(Actions);
    }

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
    ///   Gets the action that would be performed with <see cref="Redo"/>
    /// </summary>
    /// <returns>The action or null if there is nothing to redo</returns>
    public T? ActionToRedo()
    {
        if (!CanRedo())
            return null;

        return Actions[ActionIndex];
    }

    /// <summary>
    ///   Gets the action that would be performed with <see cref="Undo"/>
    /// </summary>
    /// <returns>The action or null if there is nothing to undo</returns>
    public T? ActionToUndo()
    {
        if (!CanUndo())
            return null;

        return Actions[ActionIndex - 1];
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

    /// <summary>
    ///   Deletes the entire action history. Used for now to work with editors that have partially done undo history
    /// </summary>
    internal void Nuke()
    {
        if (Actions.Count < 1)
            return;

        GD.Print("Action history nuked");
        Actions.Clear();
        ActionIndex = 0;
    }
}
