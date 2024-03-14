using Godot;

/// <summary>
///   Toggles fullscreen mode on a key press
/// </summary>
public partial class FullScreenToggle : NodeWithInput
{
    public override void _Ready()
    {
        // Keep this node running while paused
        ProcessMode = ProcessModeEnum.Always;
    }

    [RunOnKeyDown("toggle_fullscreen", OnlyUnhandled = false)]
    public void ToggleFullScreenPressed()
    {
        Settings.Instance.FullScreen.Value = !Settings.Instance.FullScreen.Value;
        GD.Print("Toggling fullscreen with keypress, new value: ", Settings.Instance.FullScreen.Value);
        Settings.Instance.ApplyWindowSettings();
    }
}
