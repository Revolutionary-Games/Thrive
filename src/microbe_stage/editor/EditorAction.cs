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
}
