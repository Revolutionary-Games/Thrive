using System.Collections.Generic;

/// <summary>
///   Done actions are stored here to provide undo/redo functionality
/// </summary>
public abstract class MicrobeEditorAction : ReversibleAction
{
    public abstract IEnumerable<MicrobeEditorActionData> Data { get; }
    public abstract int CalculateCost();
}
