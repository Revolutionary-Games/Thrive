using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Done actions are stored here to provide undo/redo functionality
/// </summary>
/// <remarks>
///   TODO: this probably needs to be split into separate classes to make saving work for these
/// </remarks>
[JSONAlwaysDynamicType]
public abstract class CellEditorAction : ReversibleAction
{
    [JsonIgnore]
    public abstract IEnumerable<MicrobeEditorCombinableActionData> Data { get; }

    public abstract int CalculateCost();
}
