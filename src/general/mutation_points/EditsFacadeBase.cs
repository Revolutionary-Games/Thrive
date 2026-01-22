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
    ///   that action. When applying changes, this is used to quickly skip over useless things from before.
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

        // Make sure if no history reset actions were present, we start from the beginning
        var start = Math.Max(0, lastHistoryReset);

        // We need to only process from the latest history reset action as nothing from-before-that can affect things
        // Note that many reset history actions need to set up state correctly in the processing, so we must process
        // it itself
        int count = activeActions.Count;
        for (int i = start; i < count; ++i)
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

    /// <summary>
    ///   Allows direct calling of <see cref="ApplyAction"/>
    /// </summary>
    internal void BecomeUsedByTopLevelFacade()
    {
        dirty = false;
    }

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
