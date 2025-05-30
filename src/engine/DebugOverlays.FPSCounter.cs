﻿using Godot;

/// <summary>
///   Partial class: FPS Counter
///   Shows FPS at top left of the screen. Toggled with F3 (default keybinding)
/// </summary>
public partial class DebugOverlays
{
#pragma warning disable CA2213
    [Export]
    private Label fpsDisplayLabel = null!;
#pragma warning restore CA2213

    private void UpdateFPS()
    {
        fpsDisplayLabel.Text = Localization.Translate("FPS").FormatSafe(Engine.GetFramesPerSecond());
    }
}
