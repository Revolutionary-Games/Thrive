using Godot;

/// <summary>
///   Partial class: FPS Counter
///   Shows FPS at top left of the screen. Toggled with F3 (default keybinding)
/// </summary>
public partial class DebugOverlay
{
    [Export]
    public NodePath FPSDisplayLabelPath = null!;

    private Label fpsDisplayLabel = null!;
}
