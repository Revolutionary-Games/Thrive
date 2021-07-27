using System;
using System.Collections.Generic;
using System.Linq;

public class EditorActionHistory : ActionHistory<MicrobeEditorAction>
{
    /// <summary>
    ///   Calculates the remaining MP from the action history
    /// </summary>
    /// <returns>The remaining MP</returns>
    public int CalculateMutationPointsLeft()
    {
        var copy = GetActionHistorySinceLastNewMicrobePress().Select(p => p.Data).ToList();
        var copyLength = copy.Count;
        for (int i = 0; i < copyLength - 1; i++)
        {
            for (int y = i + 1; y < copyLength; y++)
            {
                switch (copy[y].GetInterferenceModeWith(copy[i]))
                {
                    case MicrobeActionInterferenceMode.NoInterference:
                        break;
                    case MicrobeActionInterferenceMode.Combinable:
                        var combinedValue = copy[y].Combine(copy[i]);
                        copy.RemoveAt(i);
                        copy.RemoveAt(y);
                        copyLength--;
                        y--;
                        copy.Insert(y, combinedValue);
                        break;
                    case MicrobeActionInterferenceMode.ReplacesOther:
                        copy.RemoveAt(i);
                        i--;
                        y = i;
                        break;
                    case MicrobeActionInterferenceMode.CancelsOut:
                        copy.RemoveAt(i);
                        copy.RemoveAt(y);
                        i--;
                        y--;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        return Constants.BASE_MUTATION_POINTS - copy.Sum(p => p.CalculateCost());
    }

    private List<MicrobeEditorAction> GetActionHistorySinceLastNewMicrobePress()
    {
        var relevantActions = actions.Take(actionIndex).ToList();
        var lastNewMicrobeActionIndex = relevantActions.FindLastIndex(p => p.Data is NewMicrobeActionData);
        return lastNewMicrobeActionIndex == -1 ? relevantActions : actions.Skip(lastNewMicrobeActionIndex).ToList();
    }
}
