using System;
using System.Collections.Generic;
using System.Linq;

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

    private List<EditorCombinableActionData> History => cache ??= GetActionHistorySinceLastHistoryResettingAction();

    /// <summary>
    ///   Calculates how much MP these actions would cost if performed on top of the current history.
    /// </summary>
    public int WhatWouldActionsCost(IEnumerable<EditorCombinableActionData> actions)
    {
        return actions.Sum(WhatWouldActionCost);
    }

    /// <summary>
    ///   Calculates how much MP this action would cost if performed on top of the current history.
    /// </summary>
    public int WhatWouldActionCost(EditorCombinableActionData combinableAction)
    {
        if (CheatManager.InfiniteMP)
            return 0;

        return combinableAction.CalculateCost() + GetLeastCostCombineTuple(combinableAction, History).CostDelta;
    }

    /// <summary>
    ///   Calculates the remaining MP from the action history.
    /// </summary>
    public int CalculateMutationPointsLeft()
    {
        if (CheatManager.InfiniteMP)
            return Constants.BASE_MUTATION_POINTS;

        // As History is a reference, changing this affects the history cache
        var processedHistory = History;
        var copyLength = processedHistory.Count;

        for (int compareIndex = 1; compareIndex < copyLength; ++compareIndex)
        {
            while (true)
            {
                // Get the minimum cost action
                var (_, minimumCostCombinableAction, mode) =
                    GetLeastCostCombineTuple(processedHistory[compareIndex], processedHistory.Take(compareIndex));

                // If no more can be merged together, try next one.
                if (mode == ActionInterferenceMode.NoInterference)
                    break;

                var minimumCostCombinableActionIndex = processedHistory.IndexOf(minimumCostCombinableAction);

                switch (mode)
                {
                    case ActionInterferenceMode.Combinable:
                    {
                        var combinedValue = (EditorCombinableActionData)processedHistory[compareIndex]
                            .Combine(minimumCostCombinableAction);
                        processedHistory.RemoveAt(compareIndex);
                        processedHistory.RemoveAt(minimumCostCombinableActionIndex);
                        processedHistory.Insert(minimumCostCombinableActionIndex, combinedValue);
                        --copyLength;
                        --compareIndex;
                        break;
                    }

                    case ActionInterferenceMode.ReplacesOther:
                    {
                        processedHistory.RemoveAt(minimumCostCombinableActionIndex);
                        --copyLength;
                        --compareIndex;
                        break;
                    }

                    case ActionInterferenceMode.CancelsOut:
                    {
                        processedHistory.RemoveAt(compareIndex);
                        processedHistory.RemoveAt(minimumCostCombinableActionIndex);
                        copyLength -= 2;
                        compareIndex -= 2;
                        break;
                    }
                }

                // If mode is the following, no more checks are needed.
                if (mode == ActionInterferenceMode.CancelsOut)
                    break;
            }
        }

        return Constants.BASE_MUTATION_POINTS - processedHistory.Sum(p => p.CalculateCost());
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
        // Check if the action can be merged (for example behaviour or rigidity slider subsequent edits should
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
            History.Clear();
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

        // We undo the action here as when it is added back it will be performed again
        // And we need this to adjust the ActionIndex to be right after we remove the element
        // Note that this will clear the history cache
        if (!Undo())
            throw new Exception("Failed to undo the action we want to pop");

        if (!Actions.Remove(action))
            throw new Exception("Failed to remove action from history");

        return action;
    }

    public bool HexPlacedThisSession<THex>(THex hex)
        where THex : class, IActionHex
    {
        return History.OfType<HexPlacementActionData<THex>>().Any(a => a.PlacedHex == hex);
    }

    /// <summary>
    ///   Pick the data with which you can regain maximum MP to combine.
    /// </summary>
    /// <param name="currentData">The data you want to combine</param>
    /// <param name="previousData">A list of data currentData may combine with</param>
    /// <returns>
    ///   Three-element tuple:
    ///   CostDelta is non-positive, indicating the amount of MP you will regain;
    ///   MinimumCostActionData is the action data to combine with (not yet combined);
    ///   Mode is the interference mode currentData has with MinimumCostActionData.
    /// </returns>
    private static (int CostDelta, EditorCombinableActionData MinimumCostActionData, ActionInterferenceMode Mode)
        GetLeastCostCombineTuple(EditorCombinableActionData currentData,
            IEnumerable<EditorCombinableActionData> previousData)
    {
        return previousData.Select(data =>
        {
            var interferenceMode = currentData.GetInterferenceModeWith(data);

            return (interferenceMode switch
            {
                ActionInterferenceMode.Combinable => ((EditorCombinableActionData)currentData.Combine(data))
                    .CalculateCost() - data.CalculateCost() - currentData.CalculateCost(),
                ActionInterferenceMode.CancelsOut => -data.CalculateCost() - currentData.CalculateCost(),
                ActionInterferenceMode.ReplacesOther => -data.CalculateCost(),
                ActionInterferenceMode.NoInterference => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(interferenceMode)),
            }, data, interferenceMode);
        }).OrderBy(p => p.Item1).FirstOrDefault();
    }

    private TAction? MergeNewActionIntoPreviousIfPossible(TAction action, TAction previousAction)
    {
        var previousActionData = previousAction.Data.ToList();
        var currentActionData = action.Data.ToList();

        if (!currentActionData.Any())
            return null;

        // For now we allow combining only if all data values can be combined
        bool matches = true;

        foreach (var currentData in currentActionData)
        {
            bool currentMatched = false;

            foreach (var previousData in previousActionData)
            {
                // TODO: technically we should store a list of canceled out actions that further currentData items
                // are not allowed match against, but for now that doesn't seem necessary. This will become necessary
                // with more complex combined actions, which is something we might have in the future.
                if (previousData.WantsMergeWith(currentData) &&
                    previousData.GetInterferenceModeWith(currentData) is ActionInterferenceMode.CancelsOut
                        or ActionInterferenceMode.Combinable)
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

        // We are going to replace the new action with what we had before so we always want to pop the action from
        // history here as it will be added back when this method returns
        PopAndConfirmMatches(previousAction);

        // Perform the combining now that we've confirmed it should be supported
        foreach (var currentData in currentActionData)
        {
            bool merged = false;

            foreach (var newData in newDataList)
            {
                // In the cancels out case we still want to merge the data so that the action can stay as a placeholder
                // in the history keeping the original state
                if (newData.WantsMergeWith(currentData) &&
                    newData.GetInterferenceModeWith(currentData) is ActionInterferenceMode.CancelsOut or
                        ActionInterferenceMode.Combinable)
                {
                    merged = true;

                    // TryMerge assumes that the data to given as the parameter to the method is always newer
                    // so it must absolutely guaranteed that the order we loop here is correct, ie. always going
                    // from the oldest data towards the newer data
                    if (!newData.TryMerge(currentData))
                    {
                        throw new InvalidOperationException(
                            "Action data that should have accepted a merge, didn't");
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
