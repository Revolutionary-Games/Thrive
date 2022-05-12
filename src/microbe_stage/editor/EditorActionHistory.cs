﻿using System;
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
            History.AddRange(action.Data);
        }

        base.AddAction(action);
    }

    // TODO: try to generalize this method a bit
    public bool OrganellePlacedThisSession(OrganelleTemplate organelle)
    {
        return History.OfType<PlacementActionData>().Any(a => a.Organelle == organelle);
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
