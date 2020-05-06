using Godot;

/// <summary>
///   Shows a version label and updates the window title
/// </summary>
public class VersionNumber : Label
{
    public override void _Ready()
    {
        Text = Constants.Version;
        OS.SetWindowTitle("Thrive - " + Text);
    }
}
