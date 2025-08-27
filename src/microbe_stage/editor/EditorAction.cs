using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Done editor actions are stored here to provide undo/redo functionality
/// </summary>
[JSONAlwaysDynamicType]
public abstract class EditorAction : ReversibleAction
{
    [JsonIgnore]
    public abstract IEnumerable<EditorCombinableActionData> Data { get; }

    // Plan:
    // Then make it clear in renames that merging is purely to combine subsequent actions into a single undo/redo step

    /// <summary>
    ///   Calculates the cost for this action.
    ///   Each action separately calculates their true cost by inspecting other performed actions
    /// </summary>
    /// <param name="history">History of performed actions before the current action</param>
    /// <param name="insertPosition">
    ///   The position this action would be put at, if less than the count of <see cref="history"/> then this means
    ///   that there are also future actions after this which may need to be considered in the cost.
    /// </param>
    /// <returns>True cost of this action given the other actions</returns>
    public abstract double CalculateCost(IReadOnlyList<EditorAction> history, int insertPosition);

    /// <summary>
    ///   Used to replace the data in this action with data that has been merged
    ///   (<see cref="CombinableActionData.TryMerge"/>) with other data
    /// </summary>
    /// <param name="newData">The new data to apply</param>
    /// <exception cref="InvalidOperationException">If the data is not compatible</exception>
    public abstract void ApplyMergedData(IEnumerable<EditorCombinableActionData> newData);

    /// <summary>
    ///   Internal method used by <see cref="ApplyMergedData"/> to allow <see cref="CombinedEditorAction"/> to apply
    ///   updated child object data.
    /// </summary>
    /// <returns>The number of items consumed</returns>
    public abstract int ApplyPartialMergedData(List<EditorCombinableActionData> newData, int startIndex);

    public abstract double GetBaseCost();

    /// <summary>
    ///   Copies the <see cref="Data"/> to the target collection in the most efficient way possible.
    /// </summary>
    /// <param name="target">Where to copy the data from this</param>
    public abstract void CopyData(ICollection<EditorCombinableActionData> target);
}
