using System;
using Godot;

/// <summary>
///   Handles the logic for the options menu GUI
/// </summary>
public class OptionsMenu : Control
{
    [Export]
    public NodePath BackButtonPath;

    // Sound tab
    [Export]
    public NodePath MasterVolumePath;
    [Export]
    public NodePath MasterMutedPath;
    [Export]
    public NodePath MusicVolumePath;
    [Export]
    public NodePath MusicMutedPath;

    // Misc tab
    [Export]
    public NodePath PlayIntroPath;
    [Export]
    public NodePath PlayMicrobeIntroPath;
    [Export]
    public NodePath CheatsPath;

    private const float AUDIO_BAR_SCALE = 6.0f;

    private Button backButton;

    // Sound tab
    private Slider masterVolume;
    private CheckBox masterMuted;
    private Slider musicVolume;
    private CheckBox musicMuted;

    // Misc tab
    private CheckBox playIntro;
    private CheckBox playMicrobeIntro;
    private CheckBox cheats;

    [Signal]
    public delegate void OnOptionsClosed();

    /// <summary>
    ///   Returns the place to save the new settings values
    /// </summary>
    public Settings Settings
    {
        get => Settings.Instance;
    }

    public override void _Ready()
    {
        backButton = GetNode<Button>(BackButtonPath);

        // Sound
        masterVolume = GetNode<Slider>(MasterVolumePath);
        masterMuted = GetNode<CheckBox>(MasterMutedPath);
        musicVolume = GetNode<Slider>(MusicVolumePath);
        musicMuted = GetNode<CheckBox>(MusicMutedPath);

        // Misc
        playIntro = GetNode<CheckBox>(PlayIntroPath);
        playMicrobeIntro = GetNode<CheckBox>(PlayMicrobeIntroPath);
        cheats = GetNode<CheckBox>(CheatsPath);
    }

    public override void _Process(float delta)
    {
    }

    /// <summary>
    ///   Overrides all the control values with the values from the given settings object
    /// </summary>
    public void SetSettingsFrom(Settings settings)
    {
        // Sound
        masterVolume.Value = ConvertDBToSoundBar(settings.VolumeMaster);
        masterMuted.Pressed = settings.VolumeMasterMuted;
        musicVolume.Value = ConvertDBToSoundBar(settings.VolumeMusic);
        musicMuted.Pressed = settings.VolumeMusicMuted;

        // Misc
        playIntro.Pressed = settings.PlayIntroVideo;
        playMicrobeIntro.Pressed = settings.PlayMicrobeIntroVideo;
        cheats.Pressed = settings.CheatsEnabled;
    }

    /// <summary>
    ///   Converts the slider value (0-100) to a DB adjustement for a sound channel
    /// </summary>
    private float ConvertSoundBarToDb(float value)
    {
        return (value - 100) / AUDIO_BAR_SCALE;
    }

    private float ConvertDBToSoundBar(float value)
    {
        return (value * AUDIO_BAR_SCALE) + 100;
    }

    /*
      GUI callbacks
    */

    private void OnBackPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // TODO: ask for saving the settings
        Settings.ApplyAll();

        if (!Settings.Save())
        {
            // TODO: show an error popup
            GD.PrintErr("Couldn't save the settings");
        }

        EmitSignal(nameof(OnOptionsClosed));
    }

    private void OnIntroToggled(bool pressed)
    {
        Settings.PlayIntroVideo = pressed;
    }

    private void OnMicrobeIntroToggled(bool pressed)
    {
        Settings.PlayMicrobeIntroVideo = pressed;
    }

    private void OnCheatsToggled(bool pressed)
    {
        Settings.CheatsEnabled = pressed;
    }

    private void OnMasterMutedToggled(bool pressed)
    {
        Settings.VolumeMasterMuted = pressed;
        Settings.ApplySoundLevels();
    }

    private void OnMusicMutedToggled(bool pressed)
    {
        Settings.VolumeMusicMuted = pressed;
        Settings.ApplySoundLevels();
    }

    private void OnMasterVolumeChanged(float value)
    {
        Settings.VolumeMaster = ConvertSoundBarToDb(value);
        Settings.ApplySoundLevels();
    }

    private void OnMusicVolumeChanged(float value)
    {
        Settings.VolumeMusic = ConvertSoundBarToDb(value);
        Settings.ApplySoundLevels();
    }
}
