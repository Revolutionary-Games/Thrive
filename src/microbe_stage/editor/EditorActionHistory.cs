using System;
using System.Collections.Generic;
using System.Linq;
using Saving.Serializers;
using SharedBase.Archive;

/// <summary>
///   Holds the action history for the editor.
/// </summary>
/// <remarks>
///   <para>
///     Is capable of MP calculation.
///   </para>
///   <para>
///     TODO: this has a bit of microbe editor specific logic which should be generalized
///   </para>
/// </remarks>
public class EditorActionHistory<TAction> : ActionHistory<TAction>
    where TAction : EditorAction
{
    private List<EditorCombinableActionData>? cache;

    public EditorActionHistory()
    {
    }

    /// <summary>
    ///   Used by the deserializer
    /// </summary>
    protected EditorActionHistory(List<TAction> actions, int actionIndex) : base(actions, actionIndex)
    {
    }

    public override ushort CurrentArchiveVersion => ActionHistorySerializer.SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.ExtendedEditorActionHistory;

    private List<EditorCombinableActionData> History => cache ??= GetActionHistorySinceLastHistoryResettingAction();

    /// <summary>
    ///   Calculates how much MP these actions would cost if performed on top of the current history.
    /// </summary>
    public double WhatWouldActionsCost(IEnumerable<EditorCombinableActionData> actions)
    {
        double sum = 0;

        // TODO: there's a potential pitfall here in that if multiple things are in actions they cannot see each other
        // and thus their total cost once applied may not be the same as calculated here.
        // A proper solution would need to build a temporary list of all the data and insert the actions into it and
        // only then calculate the resulting change in cost.

        // TODO: somehow avoid the enumerator allocation here
        foreach (var action in actions)
            sum += WhatWouldActionCost(action);

        return sum;
    }

    /// <summary>
    ///   Calculates how much MP this action would cost if performed on top of the current history.
    /// </summary>
    public double WhatWouldActionCost(EditorCombinableActionData combinableAction)
    {
        if (CheatManager.InfiniteMP)
            return 0;

        return combinableAction.CalculateCost(History, History.Count);
    }

    /// <summary>
    ///   Calculates the remaining MP from the action history.
    /// </summary>
    public double CalculateMutationPointsLeft()
    {
        if (CheatManager.InfiniteMP)
            return Constants.BASE_MUTATION_POINTS;

        // As History is a reference, changing this affects the history cache
        var processedHistory = History;

        double mpLeft = Constants.BASE_MUTATION_POINTS;

        var count = processedHistory.Count;
        for (int i = 0; i < count; ++i)
        {
            var action = processedHistory[i];

            mpLeft -= action.CalculateCost(History, i);
        }

        return mpLeft;
    }

    public override bool Redo()
    {
        var result = base.Redo();
        cache = null;
        return result;
    }

    public override bool Undo()
    {
        var result = base.Undo();
        cache = null;
        return result;
    }

    public override void AddAction(TAction action)
    {
        // Check if the action can be merged (for example, behaviour or rigidity slider subsequent edits should
        // merge) in a single step for undo.
        if (ActionIndex > 0)
        {
            var merged = MergeNewActionIntoPreviousIfPossible(action, Actions[ActionIndex - 1]);
            if (merged != null)
                action = merged;
        }

        // Handle adding directly the action to our history cache, this saves us from having to rebuild the cache
        if (action.Data.Any(d => d.ResetsHistory))
        {
            // We reset the cache here as otherwise there would be a potential problem if there exists a combined
            // action where one part resets the history, but there's still other data after that. We could handle
            // clearing and then adding those items to the cache here, but that is likely so rare it isn't really
            // worth the complication to do here, so instead we just reset the cache here and will use the full rebuild
            // logic when needed.
            cache = null;
        }
        else
        {
            History.AddRange(action.Data);
        }

        base.AddAction(action);
    }

    /// <summary>
    ///   Pops the topmost performed action and returns it. This should be used only for editing the topmost action
    ///   to combine it with more steps.
    /// </summary>
    /// <returns>The action</returns>
    /// <exception cref="InvalidOperationException">If there is no performed action</exception>
    public TAction PopTopAction()
    {
        if (Actions.Count < ActionIndex)
            throw new InvalidOperationException("There is no topmost action to pop");

        var action = Actions[ActionIndex - 1];

        // We undo the action here as when it is added back, it will be performed again.
        // And we need this to adjust the ActionIndex to be right after we remove the element.
        // Note that this will clear the history cache.
        if (!Undo())
            throw new Exception("Failed to undo the action we want to pop");

        if (!Actions.Remove(action))
            throw new Exception("Failed to remove action from history");

        return action;
    }

    public bool HexPlacedThisSession<THex, TContext>(THex hex)
        where THex : class, IActionHex, IArchivable
        where TContext : IArchivable
    {
        return History.OfType<HexPlacementActionData<THex, TContext>>().Any(a => a.PlacedHex == hex);
    }

    /// <summary>
    ///   If the next action to redo should be performed in a specific context, this returns that context.
    /// </summary>
    /// <typeparam name="TContext">The type of context to be returned.</typeparam>
    /// <returns>The context the next action to redo should be performed in.</returns>
    public TContext? GetRedoContext<TContext>()
        where TContext : IArchivable
    {
        return GetContext<TContext>(ActionToRedo());
    }

    /// <summary>
    ///   If the next action to undo should be performed in a specific context, this returns that context.
    /// </summary>
    /// <typeparam name="TContext">The type of context to be returned.</typeparam>
    /// <returns>The context the next action to undo should be performed in.</returns>
    public TContext? GetUndoContext<TContext>()
        where TContext : IArchivable
    {
        return GetContext<TContext>(ActionToUndo());
    }

    private static TContext? GetContext<TContext>(TAction? action)
        where TContext : IArchivable
    {
        // We don't allow combining actions from different contexts,
        // so we only need to check the first data for the context
        var data = action?.Data.FirstOrDefault();

        if (data is EditorCombinableActionData<TContext> specificData)
            return specificData.Context;

        return default;
    }

    private TAction? MergeNewActionIntoPreviousIfPossible(TAction action, TAction previousAction)
    {
        var previousActionData = previousAction.Data.ToList();
        var currentActionData = action.Data.ToList();

        if (!currentActionData.Any())
            return null;

        // For now, we allow combining only if all data values can be combined
        bool matches = true;

        foreach (var currentData in currentActionData)
        {
            bool currentMatched = false;

            foreach (var previousData in previousActionData)
            {
                // TODO: technically we should store a list of canceled out actions that further currentData items
                // are not allowed match against, but for now that doesn't seem necessary. This will become necessary
                // with more complex combined actions, which is something we might have in the future.
                if (previousData.WantsToMergeWith(currentData))
                {
                    currentMatched = true;
                    break;
                }
            }

            if (!currentMatched)
                matches = false;
        }

        if (!matches)
            return null;

        var newDataList = new List<EditorCombinableActionData>();
        newDataList.AddRange(previousActionData);

        // We are going to replace the new action with what we had before, so we always want to pop the action from
        // history here as it will be added back when this method returns
        PopAndConfirmMatches(previousAction);

        // Perform the combining now that we've confirmed it should be supported
        foreach (var currentData in currentActionData)
        {
            bool merged = false;

            foreach (var newData in newDataList)
            {
                // In the cancels-out case we still want to merge the data so that the action can stay as a placeholder
                // in the history keeping the original state
                if (newData.WantsToMergeWith(currentData))
                {
                    merged = true;

                    // TryMerge assumes that the data given as the parameter to the method is always newer,
                    // so it must be absolutely guaranteed that the order we loop here is correct,
                    // i.e. always going from the oldest data towards the newer data
                    if (!newData.TryMerge(currentData))
                    {
                        throw new InvalidOperationException("Action data that should have accepted a merge, didn't");
                    }
                }

                if (merged)
                    break;
            }

            if (!merged)
            {
                throw new InvalidOperationException(
                    "Action data could not be merged after first checking that they could be merged");
            }
        }

        previousAction.ApplyMergedData(newDataList);
        return previousAction;
    }

    private void PopAndConfirmMatches(TAction actionInstance)
    {
        var previousAction = PopTopAction();

        if (!ReferenceEquals(previousAction, actionInstance))
            throw new InvalidOperationException("Popped latest action did not match expected object");
    }

    /// <summary>
    ///   Returns all actions since the last time a history resetting action was done
    /// </summary>
    private List<EditorCombinableActionData> GetActionHistorySinceLastHistoryResettingAction()
    {
        var relevantActions = Actions.Take(ActionIndex).SelectMany(a => a.Data).ToList();
        var lastHistoryResetActionIndex = relevantActions.FindLastIndex(d => d.ResetsHistory);
        return lastHistoryResetActionIndex == -1 ?
            relevantActions :
            Actions.Skip(lastHistoryResetActionIndex).SelectMany(p => p.Data).ToList();
    }
}
