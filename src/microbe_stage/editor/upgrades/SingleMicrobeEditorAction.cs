using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class SingleMicrobeEditorAction<T> : MicrobeEditorAction
    where T : MicrobeEditorCombinableActionData
{
    [JsonProperty]
    private readonly Action<T> redo;

    [JsonProperty]
    private readonly Action<T> undo;

    public SingleMicrobeEditorAction(Action<T> redo, Action<T> undo, T data)
    {
        this.redo = redo;
        this.undo = undo;
        SingleData = data;
    }

    public T SingleData { get; }
    public override IEnumerable<MicrobeEditorCombinableActionData> Data => new[] { SingleData };

    public static implicit operator SingleMicrobeEditorAction<MicrobeEditorCombinableActionData>(
        SingleMicrobeEditorAction<T> x)
    {
        return new SingleMicrobeEditorAction<MicrobeEditorCombinableActionData>(data => x.redo((T)data),
            data => x.undo((T)data),
            x.SingleData);
    }

    public override void DoAction()
    {
        redo(SingleData);
    }

    public override void UndoAction()
    {
        undo(SingleData);
    }

    public override int CalculateCost()
    {
        return SingleData.CalculateCost();
    }
}
