using System;
using System.Collections.Generic;

/// <summary>
///   Base class for classes that apply a set of edits on top of base data
/// </summary>
public abstract class EditsFacadeBase
{
    protected readonly List<EditorCombinableActionData> activeActions = new();

    /// <summary>
    ///   If <see cref="SpeciesEditsFacade.activeActions"/> contains a history reset action, this is the index of
    ///   that action. When applying changes, this is used to quickly skip over useless things.
    /// </summary>
    protected int lastHistoryReset = -1;

    protected bool dirty;

    /// <summary>
    ///   Applies a new set of actions replacing the old set
    /// </summary>
    /// <returns>A checkpoint number that can be used to revert another set of actions</returns>
    public int SetActiveActions(List<EditorCombinableActionData> actions)
    {
        activeActions.Clear();
        activeActions.AddRange(actions);

        RefreshHistoryResetPoint();

        MarkDirty();
        return activeActions.Count;
    }

    public void ClearActiveActions()
    {
        activeActions.Clear();
        lastHistoryReset = -1;
        MarkDirty();
    }

    public int AppendActions(IEnumerable<EditorCombinableActionData> data)
    {
        foreach (var actionData in data)
        {
            activeActions.Add(actionData);

            if (actionData.ResetsHistory)
            {
                lastHistoryReset = activeActions.Count - 1;
            }
        }

        MarkDirty();
        return activeActions.Count;
    }

    public int AppendAction(EditorCombinableActionData singleAction)
    {
        activeActions.Add(singleAction);

        if (singleAction.ResetsHistory)
        {
            lastHistoryReset = activeActions.Count - 1;
        }

        MarkDirty();
        return activeActions.Count;
    }

    /// <summary>
    ///   Returns to a checkpoint created by <see cref="SetActiveActions"/>, for example, to revert
    ///   <see cref="AppendActions"/> call.
    /// </summary>
    /// <param name="checkpoint">Checkpoint to return to</param>
    /// <returns>True if the checkpoint is valid</returns>
    public bool RevertAppend(int checkpoint)
    {
        if (checkpoint < 0 || checkpoint > activeActions.Count)
            return false;

        while (activeActions.Count >= checkpoint)
        {
            activeActions.RemoveAt(activeActions.Count - 1);
            MarkDirty();
        }

        if (lastHistoryReset >= activeActions.Count)
        {
            RefreshHistoryResetPoint();
        }

        return true;
    }

    public void ResolveDataIfDirty()
    {
        if (!dirty)
            return;

        OnStartApplyChanges();

        // We need to only process after the latest history reset action as nothing from-before-that can affect things
        int count = activeActions.Count;
        for (int i = lastHistoryReset + 1; i < count; ++i)
        {
            if (!ApplyAction(activeActions[i]))
            {
                throw new InvalidOperationException(
                    $"Could not apply action data at index {i}: {activeActions[i]} ({activeActions[i].GetType()})");
            }
        }

        dirty = false;
    }

    internal virtual void OnStartApplyChanges()
    {
    }

    internal abstract bool ApplyAction(EditorCombinableActionData actionData);

    protected void MarkDirty()
    {
        dirty = true;
    }

    protected void RefreshHistoryResetPoint()
    {
        lastHistoryReset = -1;
        int count = activeActions.Count;
        for (int i = 0; i < count; ++i)
        {
            if (activeActions[i].ResetsHistory)
                lastHistoryReset = i;
        }
    }
}
