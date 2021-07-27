using System;
using System.Collections.Generic;
using System.Linq;

public class EditorActionHistory : ActionHistory<MicrobeEditorAction>
{
    private List<MicrobeEditorActionData> cache = new List<MicrobeEditorActionData>();

    /// <summary>
    ///   Calculates the remaining MP from the action history
    /// </summary>
    /// <returns>The remaining MP</returns>
    public int CalculateMutationPointsLeft()
    {
        var copyLength = cache.Count;
        for (int i = 0; i < copyLength - 1; i++)
        {
            for (int y = i + 1; y < copyLength; y++)
            {
                switch (cache[y].GetInterferenceModeWith(cache[i]))
                {
                    case MicrobeActionInterferenceMode.NoInterference:
                        break;
                    case MicrobeActionInterferenceMode.Combinable:
                        var combinedValue = cache[y].Combine(cache[i]);
                        cache.RemoveAt(y);
                        cache.RemoveAt(i);
                        y--;
                        cache.Insert(y, combinedValue);
                        copyLength--;
                        break;
                    case MicrobeActionInterferenceMode.ReplacesOther:
                        cache.RemoveAt(i);
                        y = i;
                        copyLength--;
                        break;
                    case MicrobeActionInterferenceMode.CancelsOut:
                        cache.RemoveAt(y);
                        cache.RemoveAt(i);
                        y--;
                        copyLength -= 2;
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
        cache = GetActionHistorySinceLastNewMicrobePress();
        return base.Redo();
    }

    public override bool Undo()
    {
        cache = GetActionHistorySinceLastNewMicrobePress();
        return base.Undo();
    }

    public override void AddAction(MicrobeEditorAction action)
    {
        if (action.Data is NewMicrobeActionData)
            cache.Clear();
        else
            cache.Add(action.Data);

        base.AddAction(action);
    }

    private List<MicrobeEditorActionData> GetActionHistorySinceLastNewMicrobePress()
    {
        var relevantActions = actions.Take(actionIndex).Select(p => p.Data).ToList();
        var lastNewMicrobeActionIndex = relevantActions.FindLastIndex(p => p is NewMicrobeActionData);
        return lastNewMicrobeActionIndex == -1 ? relevantActions : actions.Skip(lastNewMicrobeActionIndex).Select(p => p.Data).ToList();
    }
}
