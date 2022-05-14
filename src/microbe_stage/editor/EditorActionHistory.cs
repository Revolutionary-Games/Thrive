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
        int result = 0;

        foreach (var action in History)
        {
            switch (combinableAction.GetInterferenceModeWith(action))
            {
                case ActionInterferenceMode.Combinable:
                {
                    result -= action.CalculateCost();
                    combinableAction = (EditorCombinableActionData)combinableAction.Combine(action);
                    break;
                }

                case ActionInterferenceMode.CancelsOut:
                {
                    result -= action.CalculateCost();
                    return result;
                }

                case ActionInterferenceMode.ReplacesOther:
                {
                    result -= action.CalculateCost();
                    break;
                }
            }
        }

        result += combinableAction.CalculateCost();
        return result;
    }

    /// <summary>
    ///   Calculates the remaining MP from the action history.
    /// </summary>
    public int CalculateMutationPointsLeft()
    {
        var copyLength = History.Count;
        for (int compareToIndex = 0; compareToIndex < copyLength - 1; compareToIndex++)
        {
            for (int compareIndex = copyLength - 1; compareIndex > compareToIndex; compareIndex--)
            {
                switch (History[compareIndex].GetInterferenceModeWith(History[compareToIndex]))
                {
                    case ActionInterferenceMode.NoInterference:
                        break;

                    case ActionInterferenceMode.Combinable:
                    {
                        var combinedValue =
                            (EditorCombinableActionData)History[compareIndex].Combine(History[compareToIndex]);
                        History.RemoveAt(compareIndex);
                        History.RemoveAt(compareToIndex);
                        History.Insert(compareToIndex, combinedValue);
                        copyLength--;
                        compareIndex = copyLength;
                        break;
                    }

                    case ActionInterferenceMode.ReplacesOther:
                    {
                        History.RemoveAt(compareToIndex);
                        copyLength--;
                        compareIndex = copyLength;
                        break;
                    }

                    case ActionInterferenceMode.CancelsOut:
                    {
                        History.RemoveAt(compareIndex);
                        History.RemoveAt(compareToIndex);
                        copyLength -= 2;
                        compareIndex = copyLength;
                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        return Constants.BASE_MUTATION_POINTS - History.Sum(p => p.CalculateCost());
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
        // TODO: check if the action can be combined (for example behaviour or rigidity slider subsequent edits should
        // combine) in a single step for undo

        // Handle adding directly the action to our history cache, this saves us from having to rebuild the cache
        if (action.Data.Any(d => d.ResetsHistory))
        {
            History.Clear();
        }
        else
        {
            // TODO: find the root cause and add a proper fix for:
            // Seems like there's some kind of bug that triggers here if we aren't at the end of the action history
            // (user has currently undone something) so in that case we need to clear the history
            if (ActionIndex < Actions.Count)
            {
                cache = null;
            }
            else
            {
                History.AddRange(action.Data);
            }
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
