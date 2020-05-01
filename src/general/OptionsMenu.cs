using System;
using Godot;

/// <summary>
///   Handles the logic for the options menu GUI
/// </summary>
public class OptionsMenu : Control
{
    [Export]
    public NodePath ResetToDefaultPath;

    // Graphics tab
    [Export]
    public NodePath VSyncPath;
    [Export]
    public NodePath FullScreenPath;

    // Sound tab
    [Export]
    public NodePath MasterVolumePath;
    [Export]
    public NodePath MasterMutedPath;
    [Export]
    public NodePath MusicVolumePath;
    [Export]
    public NodePath MusicMutedPath;

    // Performance tab
    [Export]
    public NodePath CloudIntervalPath;
    [Export]
    public NodePath CloudResolutionPath;

    // Misc tab
    [Export]
    public NodePath PlayIntroPath;
    [Export]
    public NodePath PlayMicrobeIntroPath;
    [Export]
    public NodePath CheatsPath;

    private const float AUDIO_BAR_SCALE = 6.0f;

    // Graphics tab
    private CheckBox vsync;
    private CheckBox fullScreen;

    // Sound tab
    private Slider masterVolume;
    private CheckBox masterMuted;
    private Slider musicVolume;
    private CheckBox musicMuted;

    // Performance tab
    private OptionButton cloudInterval;
    private OptionButton cloudResolution;

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
        // Graphics
        vsync = GetNode<CheckBox>(VSyncPath);
        fullScreen = GetNode<CheckBox>(FullScreenPath);

        // Sound
        masterVolume = GetNode<Slider>(MasterVolumePath);
        masterMuted = GetNode<CheckBox>(MasterMutedPath);
        musicVolume = GetNode<Slider>(MusicVolumePath);
        musicMuted = GetNode<CheckBox>(MusicMutedPath);

        // Performance
        cloudInterval = GetNode<OptionButton>(CloudIntervalPath);
        cloudResolution = GetNode<OptionButton>(CloudResolutionPath);

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
        // Graphics
        vsync.Pressed = Settings.VSync;
        fullScreen.Pressed = Settings.FullScreen;

        // Sound
        masterVolume.Value = ConvertDBToSoundBar(settings.VolumeMaster);
        masterMuted.Pressed = settings.VolumeMasterMuted;
        musicVolume.Value = ConvertDBToSoundBar(settings.VolumeMusic);
        musicMuted.Pressed = settings.VolumeMusicMuted;

        // Performance
        cloudInterval.Selected = CloudIntervalToIndex(settings.CloudUpdateInterval);
        cloudResolution.Selected = CloudResolutionToIndex(settings.CloudResolution);

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

    private int CloudIntervalToIndex(float interval)
    {
        if (interval < 0.020f)
        {
            return 0;
        }
        else if (interval == 0.020f)
        {
            return 1;
        }
        else if (interval <= 0.040f)
        {
            return 2;
        }
        else if (interval <= 0.1f)
        {
            return 3;
        }
        else if (interval > 0.1f)
        {
            return 4;
        }
        else
        {
            GD.PrintErr("invalid cloud interval value");
            return -1;
        }
    }

    private float CloudIndexToInterval(int index)
    {
        switch (index)
        {
            case 0:
                return 0;
            case 1:
                return 0.020f;
            case 2:
                return 0.040f;
            case 3:
                return 0.1f;
            case 4:
                return 0.25f;
            default:
                GD.PrintErr("invalid cloud interval index");
                return 0.040f;
        }
    }

    private int CloudResolutionToIndex(int resolution)
    {
        if (resolution <= 1)
        {
            return 0;
        }
        else if (resolution <= 2)
        {
            return 1;
        }
        else if (resolution > 2)
        {
            return 2;
        }
        else
        {
            GD.PrintErr("invalid cloud resolution value");
            return -1;
        }
    }

    private int CloudIndexToResolution(int index)
    {
        switch (index)
        {
            case 0:
                return 1;
            case 1:
                return 2;
            case 2:
                return 4;
            default:
                GD.PrintErr("invalid cloud resolution index");
                return 2;
        }
    }

    /*
      GUI callbacks
    */

    private void OnBackPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // TODO: only save if something was changed

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

    private void OnVSyncToggled(bool pressed)
    {
        Settings.VSync = pressed;
        Settings.ApplyWindowSettings();
    }

    private void OnFullScreenToggled(bool pressed)
    {
        Settings.FullScreen = pressed;
        Settings.ApplyWindowSettings();
    }

    private void OnCloudIntervalSelected(int index)
    {
        Settings.CloudUpdateInterval = CloudIndexToInterval(index);
    }

    private void OnCloudResolutionSelected(int index)
    {
        Settings.CloudResolution = CloudIndexToResolution(index);
    }
}
