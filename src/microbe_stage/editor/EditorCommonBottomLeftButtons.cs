using Godot;

/// <summary>
///   The bottom left buttons that are common to all editor types. Not to be confused with
///   <see cref="EditorComponentBottomLeftButtons"/>.
/// </summary>
public partial class EditorCommonBottomLeftButtons : MarginContainer
{
#pragma warning disable CA2213
    [Export]
    private TextureButton menuButton = null!;

    [Export]
    private TextureButton helpButton = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void OnOpenMenuEventHandler();

    [Signal]
    public delegate void OnOpenHelpEventHandler();

    public override void _Ready()
    {
        base._Ready();

        helpButton.RegisterToolTipForControl("helpButton");
        menuButton.RegisterToolTipForControl("menuButton");
    }

    private void OnMenuButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnOpenMenu);
    }

    private void OnHelpButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnOpenHelp);
    }

    private void OnStatisticsButtonPressed()
    {
        ThriveopediaManager.OpenPage("CurrentWorld");
    }
}
