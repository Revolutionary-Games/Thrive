using Godot;

/// <summary>
///   Toggles fullscreen mode on a key press
/// </summary>
[GodotAutoload]
public partial class FullScreenToggle : NodeWithInput
{
    public override void _Ready()
    {
        // TODO: isn't this unnecessary?
        // Keep this node running while paused
        ProcessMode = ProcessModeEnum.Always;
    }

    [RunOnKeyDown("toggle_fullscreen", OnlyUnhandled = false)]
    public void ToggleFullScreenPressed()
    {
        if (Engine.IsEditorHint())
        {
            GD.Print("Ignoring fullscreen change key press inside the editor");
            return;
        }

        switch (Settings.Instance.DisplayMode.Value)
        {
            case Settings.DisplayModeEnum.Windowed:
                Settings.Instance.DisplayMode.Value = Settings.DisplayModeEnum.Fullscreen;
                break;

            case Settings.DisplayModeEnum.Fullscreen:
                Settings.Instance.DisplayMode.Value = Settings.DisplayModeEnum.ExclusiveFullscreen;
                break;

            case Settings.DisplayModeEnum.ExclusiveFullscreen:
            default:
                Settings.Instance.DisplayMode.Value = Settings.DisplayModeEnum.Windowed;
                break;
        }

        GD.Print("Toggling fullscreen with keypress, new value: ", Settings.Instance.DisplayMode.Value);
        Settings.Instance.ApplyWindowSettings();
    }
}
