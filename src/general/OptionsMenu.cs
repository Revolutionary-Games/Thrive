using System;
using Godot;

/// <summary>
///   Handles the logic for the options menu GUI
/// </summary>
public class OptionsMenu : Control
{
    [Export]
    public NodePath BackButtonPath;

    // Misc tab
    [Export]
    public NodePath PlayIntroPath;
    [Export]
    public NodePath PlayMicrobeIntroPath;

    private Button backButton;

    // Misc tab
    private CheckBox playIntro;
    private CheckBox playMicrobeIntro;

    [Signal]
    public delegate void OnOptionsClosed();

    public override void _Ready()
    {
        backButton = GetNode<Button>(BackButtonPath);

        // Misc
        playIntro = GetNode<CheckBox>(PlayIntroPath);
        playMicrobeIntro = GetNode<CheckBox>(PlayMicrobeIntroPath);
    }

    public override void _Process(float delta)
    {
    }

    /// <summary>
    ///   Overrides all the control values with the values from the given settings object
    /// </summary>
    public void SetSettingsFrom(Settings settings)
    {
        playIntro.Pressed = settings.PlayIntroVideo;
        playMicrobeIntro.Pressed = settings.PlayMicrobeIntroVideo;
    }

    private void OnBackPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // TODO: ask for saving the settings
        Settings.Instance.ApplyAll();

        if (!Settings.Instance.Save())
        {
            // TODO: show an error popup
            GD.PrintErr("Couldn't save the settings");
        }

        EmitSignal(nameof(OnOptionsClosed));
    }

    private void OnIntroToggled(bool pressed)
    {
        Settings.Instance.PlayIntroVideo = pressed;
    }

    private void OnMicrobeIntroToggled(bool pressed)
    {
        Settings.Instance.PlayMicrobeIntroVideo = pressed;
    }
}
