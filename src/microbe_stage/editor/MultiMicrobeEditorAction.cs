using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Combines multiple <see cref="MicrobeEditorAction"/>s into one singular action
/// </summary>
public class MultiMicrobeEditorAction : MicrobeEditorAction
{
    public MultiMicrobeEditorAction(params MicrobeEditorAction[] actions)
    {
        Actions = actions;
    }

    [JsonProperty]
    public IReadOnlyList<MicrobeEditorAction> Actions { get; private set; }

    public override IEnumerable<MicrobeEditorActionData> Data => Actions.SelectMany(a => a.Data);

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
