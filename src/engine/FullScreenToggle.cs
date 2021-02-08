using Godot;

/// <summary>
///   Toggles fullscreen mode on a key press
/// </summary>
public class FullScreenToggle : NodeWithInput
{
    public override void _Ready()
    {
        // Keep this node running while paused
        PauseMode = PauseModeEnum.Process;
    }

    [RunOnKeyDown("toggle_fullscreen", OnlyUnhandled = false)]
    public void ToggleFullScreenPressed()
    {
        GD.Print("Toggling fullscreen with keypress");
        Settings.Instance.FullScreen.Value = !Settings.Instance.FullScreen.Value;
        Settings.Instance.ApplyWindowSettings();
    }
}
