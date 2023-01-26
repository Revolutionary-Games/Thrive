using Godot;

public class EditorCommonBottomLeftButtons : MarginContainer
{
    [Export]
    public NodePath? MenuButtonPath;

    [Export]
    public NodePath HelpButtonPath = null!;

#pragma warning disable CA2213
    private TextureButton menuButton = null!;
    private TextureButton helpButton = null!;
#pragma warning restore CA2213

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (MenuButtonPath != null)
            {
                MenuButtonPath.Dispose();
                HelpButtonPath.Dispose();
            }
        }

        base.Dispose(disposing);
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
