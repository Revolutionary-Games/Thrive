using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Holds the action history for the microbe editor.
///   Is capable of MP calculation.
/// </summary>
public class EditorActionHistory : ActionHistory<MicrobeEditorAction>
{
    private List<MicrobeEditorCombinableActionData>? cache;

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

        for (var i = 0; i < (cache ??= GetActionHistorySinceLastNewMicrobePress()).Count; ++i)
        {
            switch (combinableAction.GetInterferenceModeWith(cache[i]))
            {
                case MicrobeActionInterferenceMode.Combinable:
                    result -= cache[i].CalculateCost();
                    combinableAction = (MicrobeEditorCombinableActionData)combinableAction.Combine(cache[i]);
                    break;
                case MicrobeActionInterferenceMode.CancelsOut:
                    result -= cache[i].CalculateCost();
                    return result;
                case MicrobeActionInterferenceMode.ReplacesOther:
                    result -= cache[i].CalculateCost();
                    break;
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
        var copyLength = (cache ??= GetActionHistorySinceLastNewMicrobePress()).Count;
        for (int compareToIndex = 0; compareToIndex < copyLength - 1; compareToIndex++)
        {
            for (int compareIndex = copyLength - 1; compareIndex > compareToIndex; compareIndex--)
            {
                switch (cache[compareIndex].GetInterferenceModeWith(cache[compareToIndex]))
                {
                    case MicrobeActionInterferenceMode.NoInterference:
                        break;
                    case MicrobeActionInterferenceMode.Combinable:
                        var combinedValue = (MicrobeEditorCombinableActionData)cache[compareIndex].Combine(cache[compareToIndex]);
                        cache.RemoveAt(compareIndex);
                        cache.RemoveAt(compareToIndex);
                        cache.Insert(compareToIndex, combinedValue);
                        copyLength--;
                        compareIndex = copyLength;
                        break;
                    case MicrobeActionInterferenceMode.ReplacesOther:
                        cache.RemoveAt(compareToIndex);
                        copyLength--;
                        compareIndex = copyLength;
                        break;
                    case MicrobeActionInterferenceMode.CancelsOut:
                        cache.RemoveAt(compareIndex);
                        cache.RemoveAt(compareToIndex);
                        copyLength -= 2;
                        compareIndex = copyLength;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        return Constants.BASE_MUTATION_POINTS - cache.Sum(p => p.CalculateCost());
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
        if (cache == null)
            throw new ArgumentException("Call Redo or Undo first");

        switch (action)
        {
            case SingleMicrobeEditorAction<NewMicrobeActionData>:
                cache.Clear();
                break;
            default:
                cache.AddRange(action.Data);
                break;
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
