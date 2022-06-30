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

    public override void ApplyMergedData(IEnumerable<EditorCombinableActionData> newData)
    {
        var newDataList = newData.ToList();

        if (newDataList.Count < 1)
            throw new InvalidOperationException("Merged data didn't contain anything");

        int consumedItems = ApplyPartialMergedData(newDataList, 0);

        if (consumedItems != newDataList.Count)
            throw new InvalidOperationException("Merged data didn't contain right number of data items");
    }

    public override int ApplyPartialMergedData(List<EditorCombinableActionData> newData, int startIndex)
    {
        int index = 0;

        foreach (var subAction in actions.ToList())
        {
            int applied = subAction.ApplyPartialMergedData(newData, startIndex + index);

            if (applied < 1)
            {
                // This sub action didn't have data to apply to it, so destroy it as it is wanted to be removed
                actions.Remove(subAction);
            }
            else
            {
                index += applied;
            }
        }

        if (Actions.Count < 1)
            throw new InvalidOperationException("Merging data into a combined action caused it to become empty");

        return index;
    }
}
