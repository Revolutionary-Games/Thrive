using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.SharpZipLib;

/// <summary>
///   Holds the action history for the microbe editor.
///   Is capable of MP calculation.
/// </summary>
public class EditorActionHistory : ActionHistory<MicrobeEditorAction>
{
    private List<MicrobeEditorCombinableActionData>? cache;

    private List<MicrobeEditorCombinableActionData> History =>
        cache ??= GetActionHistorySinceLastNewMicrobePress();

    /// <summary>
    ///   Calculates how much MP these actions would cost if performed on top of the current history.
    /// </summary>
    public int WhatWouldActionsCost(List<MicrobeEditorCombinableActionData> actions)
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
                case MicrobeActionInterferenceMode.Combinable:
                {
                    result -= History[i].CalculateCost();
                    combinableAction = (MicrobeEditorCombinableActionData)combinableAction.Combine(History[i]);
                    break;
                }

                case MicrobeActionInterferenceMode.CancelsOut:
                {
                    result -= History[i].CalculateCost();
                    return result;
                }

                case MicrobeActionInterferenceMode.ReplacesOther:
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
                    case MicrobeActionInterferenceMode.NoInterference:
                        break;

                    case MicrobeActionInterferenceMode.Combinable:
                    {
                        var combinedValue = (MicrobeEditorCombinableActionData)History[compareIndex].Combine(History[compareToIndex]);
                        History.RemoveAt(compareIndex);
                        History.RemoveAt(compareToIndex);
                        History.Insert(compareToIndex, combinedValue);
                        copyLength--;
                        compareIndex = copyLength;
                        break;
                    }

                    case MicrobeActionInterferenceMode.ReplacesOther:
                    {
                        History.RemoveAt(compareToIndex);
                        copyLength--;
                        compareIndex = copyLength;
                        break;
                    }

                    case MicrobeActionInterferenceMode.CancelsOut:
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
        cache = GetActionHistorySinceLastNewMicrobePress();
        return result;
    }

    public override bool Undo()
    {
        var result = base.Undo();
        cache = GetActionHistorySinceLastNewMicrobePress();
        return result;
    }

    public override void AddAction(MicrobeEditorAction action)
    {
        switch (action)
        {
            case SingleMicrobeEditorAction<NewMicrobeActionData>:
            {
                History.Clear();
                break;
            }

            default:
            {
                History.AddRange(action.Data);
                break;
            }
        }

        base.AddAction(action);
    }

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
