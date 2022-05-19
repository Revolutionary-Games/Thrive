using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   This action contains a single "action" in contrast to <see cref="CombinedEditorAction"/> which can
///   have a number of actions that are logically a single step.
/// </summary>
/// <typeparam name="T">Type of the action data to hold</typeparam>
[JSONAlwaysDynamicType]
public class SingleEditorAction<T> : EditorAction
    where T : EditorCombinableActionData
{
    [JsonProperty]
    private readonly Action<T> redo;

    [JsonProperty]
    private readonly Action<T> undo;

    public SingleEditorAction(Action<T> redo, Action<T> undo, T data)
    {
        this.redo = redo;
        this.undo = undo;
        SingleData = data;
    }

    [JsonProperty]
    public T SingleData { get; private set; }

    [JsonIgnore]
    public override IEnumerable<EditorCombinableActionData> Data => new[] { SingleData };

    public static implicit operator SingleEditorAction<EditorCombinableActionData>(SingleEditorAction<T> x)
    {
        return new SingleEditorAction<EditorCombinableActionData>(data => x.redo((T)data),
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
