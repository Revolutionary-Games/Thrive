using Godot;

/// <summary>
///   Handles the logic for the options menu GUI
/// </summary>
public class OptionsMenu : Control
{
    [Export]
    public NodePath ResetToDefaultPath;

    // Tab buttons
    [Export]
    public NodePath GraphicsButtonPath;

    [Export]
    public NodePath SoundButtonPath;

    [Export]
    public NodePath PerformanceButtonPath;

    [Export]
    public NodePath MiscButtonPath;

    // Graphics tab
    [Export]
    public NodePath GraphicsTabPath;

    [Export]
    public NodePath VSyncPath;

    [Export]
    public NodePath FullScreenPath;

    [Export]
    public NodePath MSAAResolutionPath;

    [Export]
    public NodePath ColourblindSettingPath;

    [Export]
    public NodePath ChromaticAberrationSliderPath;

    [Export]
    public NodePath ChromaticAberrationTogglePath;

    // Sound tab
    [Export]
    public NodePath SoundTabPath;

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
    public NodePath PerformanceTabPath;

    [Export]
    public NodePath CloudIntervalPath;

    [Export]
    public NodePath CloudResolutionPath;

    // Misc tab
    [Export]
    public NodePath MiscTabPath;

    [Export]
    public NodePath PlayIntroPath;

    [Export]
    public NodePath PlayMicrobeIntroPath;

    [Export]
    public NodePath CheatsPath;

    [Export]
    public NodePath AutoSavePath;

    [Export]
    public NodePath MaxAutoSavesPath;

    [Export]
    public NodePath MaxQuickSavesPath;

    [Export]
    public NodePath ResetConfirmationBoxPath;

    private const float AUDIO_BAR_SCALE = 6.0f;

    // Alert Box
    private ConfirmationDialog resetConfirmationBox;

    // Tab buttons
    private Button graphicsButton;
    private Button soundButton;
    private Button performanceButton;
    private Button miscButton;

    // Graphics tab
    private Control graphicsTab;
    private CheckBox vsync;
    private CheckBox fullScreen;
    private OptionButton msaaResolution;
    private OptionButton colourblindSetting;
    private CheckBox chromaticAberrationToggle;
    private Slider chromaticAberrationSlider;

    // Sound tab
    private Control soundTab;
    private Slider masterVolume;
    private CheckBox masterMuted;
    private Slider musicVolume;
    private CheckBox musicMuted;

    // Performance tab
    private Control performanceTab;
    private OptionButton cloudInterval;
    private OptionButton cloudResolution;

    // Misc tab
    private Control miscTab;
    private CheckBox playIntro;
    private CheckBox playMicrobeIntro;
    private CheckBox cheats;
    private CheckBox autosave;
    private SpinBox maxAutosaves;
    private SpinBox maxQuicksaves;

    [Signal]
    public delegate void OnOptionsClosed();

    /// <summary>
    ///   Returns the place to save the new settings values
    /// </summary>
    public Settings Settings => Settings.Instance;

    public override void _Ready()
    {
        // Tab buttons
        graphicsButton = GetNode<Button>(GraphicsButtonPath);
        soundButton = GetNode<Button>(SoundButtonPath);
        performanceButton = GetNode<Button>(PerformanceButtonPath);
        miscButton = GetNode<Button>(MiscButtonPath);

        // Graphics
        graphicsTab = GetNode<Control>(GraphicsTabPath);
        vsync = GetNode<CheckBox>(VSyncPath);
        fullScreen = GetNode<CheckBox>(FullScreenPath);
        msaaResolution = GetNode<OptionButton>(MSAAResolutionPath);
        colourblindSetting = GetNode<OptionButton>(ColourblindSettingPath);
        chromaticAberrationToggle = GetNode<CheckBox>(ChromaticAberrationTogglePath);
        chromaticAberrationSlider = GetNode<Slider>(ChromaticAberrationSliderPath);

        // Sound
        soundTab = GetNode<Control>(SoundTabPath);
        masterVolume = GetNode<Slider>(MasterVolumePath);
        masterMuted = GetNode<CheckBox>(MasterMutedPath);
        musicVolume = GetNode<Slider>(MusicVolumePath);
        musicMuted = GetNode<CheckBox>(MusicMutedPath);

        // Performance
        performanceTab = GetNode<Control>(PerformanceTabPath);
        cloudInterval = GetNode<OptionButton>(CloudIntervalPath);
        cloudResolution = GetNode<OptionButton>(CloudResolutionPath);

        // Misc
        miscTab = GetNode<Control>(MiscTabPath);
        playIntro = GetNode<CheckBox>(PlayIntroPath);
        playMicrobeIntro = GetNode<CheckBox>(PlayMicrobeIntroPath);
        cheats = GetNode<CheckBox>(CheatsPath);
        autosave = GetNode<CheckBox>(AutoSavePath);
        maxAutosaves = GetNode<SpinBox>(MaxAutoSavesPath);
        maxQuicksaves = GetNode<SpinBox>(MaxQuickSavesPath);
        resetConfirmationBox = GetNode<ConfirmationDialog>(ResetConfirmationBoxPath);
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
        msaaResolution.Selected = MSAAResolutionToIndex(settings.MSAAResolution);
        colourblindSetting.Selected = settings.ColourblindSetting;
        chromaticAberrationToggle.Pressed = settings.ChromaticEnabled;
        chromaticAberrationSlider.Value = settings.ChromaticAmount;

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
        autosave.Pressed = settings.AutoSaveEnabled;
        maxAutosaves.Value = settings.MaxAutoSaves;
        maxAutosaves.Editable = settings.AutoSaveEnabled;
        maxQuicksaves.Value = settings.MaxQuickSaves;
    }

    private void SetSettingsTab(string tab)
    {
        graphicsTab.Hide();
        soundTab.Hide();
        performanceTab.Hide();
        miscTab.Hide();

        if (tab == "graphics")
        {
            graphicsTab.Show();
            graphicsButton.Pressed = true;
        }
        else if (tab == "sound")
        {
            soundTab.Show();
            soundButton.Pressed = true;
        }
        else if (tab == "performance")
        {
            performanceTab.Show();
            performanceButton.Pressed = true;
        }
        else if (tab == "miscellaneous")
        {
            miscTab.Show();
            miscButton.Pressed = true;
        }
        else
        {
            GD.PrintErr("Invalid tab");
        }
    }

    /// <summary>
    ///   Converts the slider value (0-100) to a DB adjustment for a sound channel
    /// </summary>
    private float ConvertSoundBarToDb(float value)
    {
        return (value - 100) / AUDIO_BAR_SCALE;
    }

    private float ConvertDBToSoundBar(float value)
    {
        return value * AUDIO_BAR_SCALE + 100;
    }

    private int CloudIntervalToIndex(float interval)
    {
        if (interval < 0.020f)
            return 0;

        if (interval == 0.020f)
            return 1;

        if (interval <= 0.040f)
            return 2;

        if (interval <= 0.1f)
            return 3;

        if (interval > 0.1f)
            return 4;

        GD.PrintErr("invalid cloud interval value");
        return -1;
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

        if (resolution <= 2)
        {
            return 1;
        }

        return 2;
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

    private int MSAAResolutionToIndex(Viewport.MSAA resolution)
    {
        if (resolution == Viewport.MSAA.Disabled)
            return 0;

        if (resolution == Viewport.MSAA.Msaa2x)
            return 1;

        if (resolution == Viewport.MSAA.Msaa4x)
            return 2;

        if (resolution == Viewport.MSAA.Msaa8x)
            return 3;

        if (resolution == Viewport.MSAA.Msaa16x)
            return 4;

        GD.PrintErr("invalid MSAA resolution value");
        return 0;
    }

    private Viewport.MSAA MSAAIndexToResolution(int index)
    {
        switch (index)
        {
            case 0:
                return Viewport.MSAA.Disabled;
            case 1:
                return Viewport.MSAA.Msaa2x;
            case 2:
                return Viewport.MSAA.Msaa4x;
            case 3:
                return Viewport.MSAA.Msaa8x;
            case 4:
                return Viewport.MSAA.Msaa16x;
            default:
                GD.PrintErr("invalid MSAA resolution index");
                return Viewport.MSAA.Disabled;
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

    private void ResetConfirmSelected()
    {
        Settings.ResetToDefaults();
        Settings.ApplyAll();
        SetSettingsFrom(Settings);
        resetConfirmationBox.Visible = false;
    }

    private void OnResetPressed()
    {
        resetConfirmationBox.Visible = true;
    }

    private void ResetConfirmHide()
    {
        resetConfirmationBox.Visible = false;
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

    private void OnMSAAResolutionSelected(int index)
    {
        Settings.MSAAResolution = MSAAIndexToResolution(index);
        Settings.ApplyGraphicsSettings();
    }

    private void OnColourblindSettingSelected(int index)
    {
        Settings.ColourblindSetting = index;
        Settings.ApplyGraphicsSettings();
    }

    private void OnChromaticAberrationToggled(bool toggle)
    {
        Settings.ChromaticEnabled = toggle;
    }

    private void OnChromaticAberrationValueChanged(float amount)
    {
        Settings.ChromaticAmount = amount;
    }

    private void OnAutoSaveToggled(bool pressed)
    {
        Settings.AutoSaveEnabled = pressed;
        maxAutosaves.Editable = pressed;
    }

    private void OnMaxAutoSavesValueChanged(float value)
    {
        Settings.MaxAutoSaves = (int)value;
    }

    private void OnMaxQuickSavesValueChanged(float value)
    {
        Settings.MaxQuickSaves = (int)value;
    }
}
