using Godot;

/// <summary>
///   Shows a version label
/// </summary>
public class VersionNumber : Label
{
    public override void _Ready()
    {
        Text = Constants.Version;
    }
}
