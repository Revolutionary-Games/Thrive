using System;
using Newtonsoft.Json;

public class SingleMicrobeEditorAction : MicrobeEditorAction
{
    [JsonProperty]
    private readonly Action<SingleMicrobeEditorAction> redo;

    [JsonProperty]
    private readonly Action<SingleMicrobeEditorAction> undo;

    private MicrobeEditorActionData data;

    public SingleMicrobeEditorAction(Action<SingleMicrobeEditorAction> redo, Action<SingleMicrobeEditorAction> undo,
        MicrobeEditorActionData data)
    {
        this.redo = redo;
        this.undo = undo;
        this.data = data;
    }

    public override MicrobeEditorActionData MicrobeData
    {
        get => data;
        set => data = value;
    }

    public override void DoAction()
    {
        redo(this);
    }

    public override void UndoAction()
    {
        undo(this);
    }
}
