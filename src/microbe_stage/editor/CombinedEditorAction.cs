using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Combines multiple <see cref="EditorAction"/>s into one singular action to act as a single unit in the undo system
/// </summary>
[JSONAlwaysDynamicType]
public class CombinedEditorAction : EditorAction
{
    [JsonProperty]
    private List<EditorAction> actions;

    public CombinedEditorAction(params EditorAction[] actions)
    {
        if (actions.Length < 1)
            throw new ArgumentException("Actions can't be empty");

        this.actions = actions.ToList();
    }

    [JsonConstructor]
    public CombinedEditorAction(IEnumerable<EditorAction> actions)
    {
        this.actions = actions.ToList();

        if (Actions.Count < 1)
            throw new ArgumentException("Actions can't be empty");
    }

    [JsonIgnore]
    public IReadOnlyList<EditorAction> Actions => actions;

    [JsonIgnore]
    public override IEnumerable<EditorCombinableActionData> Data => Actions.SelectMany(a => a.Data);

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
