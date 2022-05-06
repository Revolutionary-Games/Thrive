using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   This action contains a single "action" in contrast to <see cref="CombinedMicrobeEditorAction"/> which can
///   have a number of actions that are logically a single step.
/// </summary>
/// <typeparam name="T">Type of the action data to hold</typeparam>
public class SingleCellEditorAction<T> : CellEditorAction
    where T : MicrobeEditorCombinableActionData
{
    [JsonProperty]
    private readonly Action<T> redo;

    [JsonProperty]
    private readonly Action<T> undo;

    public SingleCellEditorAction(Action<T> redo, Action<T> undo, T data)
    {
        this.redo = redo;
        this.undo = undo;
        SingleData = data;
    }

    public T SingleData { get; }
    public override IEnumerable<MicrobeEditorCombinableActionData> Data => new[] { SingleData };

    public static implicit operator SingleCellEditorAction<MicrobeEditorCombinableActionData>(
        SingleCellEditorAction<T> x)
    {
        return new SingleCellEditorAction<MicrobeEditorCombinableActionData>(data => x.redo((T)data),
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
