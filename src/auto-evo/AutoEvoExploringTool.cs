using System;
using Godot;

public class AutoEvoExploringTool : ControlWithInput
{
    [Signal]
    public delegate void OnAutoEvoExploringToolClosed();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    public void OpenFromMainMenu()
    {
        if (Visible)
            return;

        Show();
    }

    [RunOnKeyDown("ui_cancel")]
    private void OnBackButtonPressed()
    {
        if (!Visible)
            return;

        EmitSignal(nameof(OnAutoEvoExploringToolClosed));
    }
}
