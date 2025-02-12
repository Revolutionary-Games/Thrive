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

        if (Settings.Instance.DisplayMode.Value == DisplayServer.WindowMode.Fullscreen || Settings.Instance.DisplayMode.Value == DisplayServer.WindowMode.ExclusiveFullscreen)
        {
            Settings.Instance.DisplayMode.Value = DisplayServer.WindowMode.Windowed;
        }
        else
        {
            Settings.Instance.DisplayMode.Value = DisplayServer.WindowMode.Fullscreen;
        }

        GD.Print("Toggling fullscreen with keypress, new value: ", Settings.Instance.DisplayMode.Value);
        Settings.Instance.ApplyWindowSettings();
    }
}
