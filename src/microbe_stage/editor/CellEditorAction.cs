using System.Collections.Generic;

/// <summary>
///   Done actions are stored here to provide undo/redo functionality
/// </summary>
/// <remarks>
///   TODO: this probably needs to be split into separate classes to make saving work for these
/// </remarks>
public abstract class CellEditorAction : ReversibleAction
{
    public abstract IEnumerable<MicrobeEditorCombinableActionData> Data { get; }
    public abstract int CalculateCost();
}
