using Godot;

public class EditorCommonBottomLeftButtons : MarginContainer
{
    [Export]
    public NodePath MenuButtonPath = null!;

    [Export]
    public NodePath HelpButtonPath = null!;

    private TextureButton menuButton = null!;
    private TextureButton helpButton = null!;

    [Signal]
    public delegate void OnOpenMenu();

    [Signal]
    public delegate void OnOpenHelp();

    [Signal]
    public delegate void OnOpenStatistics();

    public override void _Ready()
    {
        base._Ready();

        menuButton = GetNode<TextureButton>(MenuButtonPath);
        helpButton = GetNode<TextureButton>(HelpButtonPath);

        helpButton.RegisterToolTipForControl("helpButton");
        menuButton.RegisterToolTipForControl("menuButton");
    }

    private void OnMenuButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnOpenMenu));
    }

    private void OnHelpButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnOpenHelp));
    }

    private void OnStatisticsButtonPressed()
    {
        // No need to play a sound as changing Thriveopedia page does it anyway
        EmitSignal(nameof(OnOpenStatistics));
    }
}
