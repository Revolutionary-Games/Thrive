using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Holds the action history for the microbe editor.
/// </summary>
/// <remarks>
///   <para>
///     Is capable of MP calculation.
///   </para>
/// </remarks>
public class EditorActionHistory<TAction> : ActionHistory<TAction>
    where TAction : CellEditorAction
{
    private List<MicrobeEditorCombinableActionData>? cache;

    private List<MicrobeEditorCombinableActionData> History =>
        cache ??= GetActionHistorySinceLastNewMicrobePress();

    // TODO: remove this if this stays unused
    /// <summary>
    ///   Calculates how much MP these actions would cost if performed on top of the current history.
    /// </summary>
    public int WhatWouldActionsCost(IEnumerable<MicrobeEditorCombinableActionData> actions)
    {
        return actions.Sum(WhatWouldActionCost);
    }

    /// <summary>
    ///   Calculates how much MP this action would cost if performed on top of the current history.
    /// </summary>
    public int WhatWouldActionCost(MicrobeEditorCombinableActionData combinableAction)
    {
        int result = 0;

        for (var i = 0; i < History.Count; ++i)
        {
            switch (combinableAction.GetInterferenceModeWith(History[i]))
            {
                case ActionInterferenceMode.Combinable:
                {
                    result -= History[i].CalculateCost();
                    combinableAction = (MicrobeEditorCombinableActionData)combinableAction.Combine(History[i]);
                    break;
                }

                case ActionInterferenceMode.CancelsOut:
                {
                    result -= History[i].CalculateCost();
                    return result;
                }

                case ActionInterferenceMode.ReplacesOther:
                {
                    result -= History[i].CalculateCost();
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
                            (MicrobeEditorCombinableActionData)History[compareIndex].Combine(History[compareToIndex]);
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

    // TODO: remove if this stays unused
    public bool OrganellePlacedThisSession(OrganelleTemplate organelle)
    {
        return Actions.SelectMany(a => a.Data).OfType<PlacementActionData>().Any(a => a.Organelle == organelle);
    }

    /// <summary>
    ///   Returns all actions since the last time the user performed the "New Microbe" action
    /// </summary>
    private List<MicrobeEditorCombinableActionData> GetActionHistorySinceLastNewMicrobePress()
    {
        var relevantActions = Actions.Take(ActionIndex).SelectMany(p => p.Data).ToList();
        var lastNewMicrobeActionIndex = relevantActions.FindLastIndex(p => p is NewMicrobeActionData);
        return lastNewMicrobeActionIndex == -1 ?
            relevantActions :
            Actions.Skip(lastNewMicrobeActionIndex).SelectMany(p => p.Data).ToList();
    }
}
