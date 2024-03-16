using Godot;

/// <summary>
///   The bottom left buttons that are common to all editor types. Not to be confused with
///   <see cref="EditorComponentBottomLeftButtons"/>.
/// </summary>
public partial class EditorCommonBottomLeftButtons : MarginContainer
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
    public delegate void OnOpenMenuEventHandler();

    [Signal]
    public delegate void OnOpenHelpEventHandler();

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
