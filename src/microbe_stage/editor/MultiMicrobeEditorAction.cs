using System;
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
        if (actions.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(actions),
                $"Cannot create a {nameof(MultiMicrobeEditorAction)} without any {nameof(MicrobeEditorAction)}s");
        }

        Actions = actions;
    }

    [JsonProperty]
    public IReadOnlyList<MicrobeEditorAction> Actions { get; private set; }

    public override MicrobeEditorActionData MicrobeData
    {
        get => Actions.Last().MicrobeData;
        set => Actions.Last().MicrobeData = value;
    }

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
}
