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

    public override void _Ready()
    {
        base._Ready();

        helpButton.RegisterToolTipForControl("helpButton");
        menuButton.RegisterToolTipForControl("menuButton");
    }

    private void OnMenuButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        PauseMenu.Instance.Open();
    }

    private void OnHelpButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        PauseMenu.Instance.OpenToHelp();
    }

    private void OnStatisticsButtonPressed()
    {
        // Bug is fixed in Thriveopedia about duplicate sound playing, so now we play sound here
        GUICommon.Instance.PlayButtonPressSound();
        ThriveopediaManager.OpenPage("CurrentWorld");
    }
}
