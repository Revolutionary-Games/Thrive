using System;
using Godot;

/// <summary>
///   Allows creating new cell types for things like spores.
/// </summary>
public partial class CellTypeMakerButton : Control
{
    [Signal]
    public delegate void OnClickedEventHandler();

    public void OnMainButtonClicked()
    {
        EmitSignal(SignalName.OnClicked);
    }
}
