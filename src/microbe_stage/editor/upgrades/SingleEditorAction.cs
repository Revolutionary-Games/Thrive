using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   This action contains a single "action" in contrast to <see cref="CombinedEditorAction"/> which can
///   have a number of actions that are logically a single step.
/// </summary>
/// <typeparam name="T">Type of the action data to hold</typeparam>
[JSONAlwaysDynamicType]
public class SingleEditorAction<T> : EditorAction, IEnumerable<EditorCombinableActionData>
    where T : EditorCombinableActionData
{
    [JsonProperty]
    private readonly Action<T> redo;

    [JsonProperty]
    private readonly Action<T> undo;

    private List<EditorCombinableActionData>? temporaryMergedData;

    public SingleEditorAction(Action<T> redo, Action<T> undo, T data)
    {
        this.redo = redo;
        this.undo = undo;
        SingleData = data;
    }

    [JsonProperty]
    public T SingleData { get; private set; }

    [JsonIgnore]
    public override IEnumerable<EditorCombinableActionData> Data => this;

    public static implicit operator SingleEditorAction<EditorCombinableActionData>(SingleEditorAction<T> action)
    {
        return new SingleEditorAction<EditorCombinableActionData>(d => action.redo((T)d),
            d => action.undo((T)d), action.SingleData);
    }

    public override void DoAction()
    {
        redo(SingleData);
    }

    public override void UndoAction()
    {
        undo(SingleData);
    }

    public override double GetBaseCost()
    {
        return SingleData.GetBaseCost();
    }

    public override double CalculateCost(IReadOnlyList<EditorAction> history, int insertPosition)
    {
        // Note due to this object reuse this method is not re-entrant, but this approach is used to avoid short-lived
        // allocations
        temporaryMergedData ??= new List<EditorCombinableActionData>(history.Count);
        temporaryMergedData.Clear();

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            history[i].CopyData(temporaryMergedData);
        }

        return SingleData.CalculateCost(temporaryMergedData, insertPosition);
    }

    public override void ApplyMergedData(IEnumerable<EditorCombinableActionData> newData)
    {
        bool applied = false;

        foreach (var data in newData)
        {
            if (applied)
                throw new InvalidOperationException("Single editor action can only take a single merged action");

            SingleData = (T)data;
            applied = true;
        }

        if (!applied)
            throw new InvalidOperationException("Merged data didn't contain anything");
    }

    public override int ApplyPartialMergedData(List<EditorCombinableActionData> newData, int startIndex)
    {
        // To support removing actions from combined action, we need to skip applying here if the item is wrong data
        // which indicates that we want to be deleted
        if (newData[startIndex] is T compatibleData)
        {
            SingleData = compatibleData;
            return 1;
        }

        return 0;
    }

    public override void CopyData(ICollection<EditorCombinableActionData> target)
    {
        target.Add(SingleData);
    }

    public IEnumerator<EditorCombinableActionData> GetEnumerator()
    {
        // Creating an enumerator like this should use a minimal amount of allocated memory
        yield return SingleData;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        yield return SingleData;
    }
}
