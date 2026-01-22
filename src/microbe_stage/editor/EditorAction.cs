using System;
using System.Collections.Generic;

/// <summary>
///   Done editor actions are stored here to provide undo/redo functionality
/// </summary>
public abstract class EditorAction : ReversibleAction
{
    public abstract IEnumerable<EditorCombinableActionData> Data { get; }

    // Plan:
    // Then make it clear in renames that merging is purely to combine subsequent actions into a single undo/redo step

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

    /// <summary>
    ///   Copies the <see cref="Data"/> to the target collection in the most efficient way possible.
    ///   Doesn't clear the target.
    /// </summary>
    /// <param name="target">Where to copy the data from this</param>
    public abstract void CopyData(ICollection<EditorCombinableActionData> target);
}
