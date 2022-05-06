using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Combines multiple <see cref="CellEditorAction"/>s into one singular action
/// </summary>
public class CombinedMicrobeEditorAction : CellEditorAction
{
    public CombinedMicrobeEditorAction(params CellEditorAction[] actions)
    {
        Actions = actions;
    }

    [JsonProperty]
    public IReadOnlyList<CellEditorAction> Actions { get; private set; }

    public override IEnumerable<MicrobeEditorCombinableActionData> Data => Actions.SelectMany(a => a.Data);

    public override void DoAction()
    {
        foreach (var action in Actions)
            action.DoAction();
    }

    public override void UndoAction()
    {
        foreach (var action in Actions.Reverse())
            action.UndoAction();
    }

    public override int CalculateCost()
    {
        return Actions.Sum(a => a.CalculateCost());
    }
}
