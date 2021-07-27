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
        return GetActionHistorySinceLastNewMicrobePress().Count;
    }

    private List<MicrobeEditorAction> GetActionHistorySinceLastNewMicrobePress()
    {
        var relevantActions = actions.Take(actionIndex).ToList();
        var lastNewMicrobeActionIndex = relevantActions.FindLastIndex(p => p.Data is NewMicrobeActionData);
        return lastNewMicrobeActionIndex == -1 ? relevantActions : actions.Skip(lastNewMicrobeActionIndex).ToList();
    }
}
