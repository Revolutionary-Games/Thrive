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

    public abstract int CalculateCost();

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
}
