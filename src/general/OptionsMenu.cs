using System;
using Godot;

/// <summary>
///   Handles the logic for the options menu GUI.
/// </summary>
public class OptionsMenu : Control
{
    /*
      GUI Control Paths
    */

    // Options control buttons.

    [Export]
    public NodePath ResetButtonPath;

    [Export]
    public NodePath SaveButtonPath;

    // Tab selector buttons.
    [Export]
    public NodePath GraphicsButtonPath;

    [Export]
    public NodePath SoundButtonPath;

    [Export]
    public NodePath PerformanceButtonPath;

    [Export]
    public NodePath MiscButtonPath;

    // Graphics tab.
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

    // Sound tab.
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

    // Performance tab.
    [Export]
    public NodePath PerformanceTabPath;

    [Export]
    public NodePath CloudIntervalPath;

    [Export]
    public NodePath CloudResolutionPath;

    // Misc tab.
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
    public NodePath BackConfirmationBoxPath;

    [Export]
    public NodePath DefaultsConfirmationBoxPath;

    private const float AUDIO_BAR_SCALE = 6f;
    private Button resetButton;
    private Button saveButton;

    // Tab selector buttons
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

    // Confirmation Boxes
    private ConfirmationDialog backConfirmationBox;
    private ConfirmationDialog defaultsConfirmationBox;

    /*
      Misc
    */

    /// <summary>
    ///   Copy of the settings object that should match what is saved to the configuration file,
    ///   used for comparing and restoring to previous state.
    /// </summary>
    private Settings savedSettings;

    private SelectedOptionsTab selectedOptionsTab;

    /*
      Signals
    */

    [Signal]
    public delegate void OnOptionsClosed();

    private enum SelectedOptionsTab
    {
        Graphics,
        Sound,
        Performance,
        Miscellaneous,
    }

    /// <summary>
    ///   Returns the place to save the new settings values
    /// </summary>
    public Settings Settings => Settings.Instance;

    public override void _Ready()
    {
        // Options control buttons
        resetButton = GetNode<Button>(ResetButtonPath);
        saveButton = GetNode<Button>(SaveButtonPath);

        // Tab selector buttons
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

        backConfirmationBox = GetNode<ConfirmationDialog>(BackConfirmationBoxPath);
        defaultsConfirmationBox = GetNode<ConfirmationDialog>(DefaultsConfirmationBoxPath);

        selectedOptionsTab = SelectedOptionsTab.Graphics;

        // Copy settings from the singleton to serve as a copy of the last saved settings.
        savedSettings = Settings.Instance.Clone();

        // Set the initial state of the options controls to match the settings data.
        ApplySettingsToControls(savedSettings);
        CompareSettings();
    }

    public override void _Process(float delta)
    {
    }

    /// <summary>
    ///   Applies the values of the specified settings object to all corresponding menu controls.
    /// </summary>
    public void ApplySettingsToControls(Settings settings)
    {
        // Graphics
        vsync.Pressed = settings.VSync;
        fullScreen.Pressed = settings.FullScreen;
        msaaResolution.Selected = MSAAResolutionToIndex(settings.MSAAResolution);
        colourblindSetting.Selected = settings.ColourblindSetting;
        chromaticAberrationSlider.Value = settings.ChromaticAmount;
        chromaticAberrationToggle.Pressed = settings.ChromaticEnabled;

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
        // Convert from the string binding to an enum.
        SelectedOptionsTab selection = (SelectedOptionsTab)Enum.Parse(typeof(SelectedOptionsTab), tab);

        // Pressing the same button that's already active, so just return.
        if (selection == selectedOptionsTab)
        {
            return;
        }

        graphicsTab.Hide();
        soundTab.Hide();
        performanceTab.Hide();
        miscTab.Hide();

        switch (selection)
        {
            case SelectedOptionsTab.Graphics:
                graphicsTab.Show();
                graphicsButton.Pressed = true;
                break;
            case SelectedOptionsTab.Sound:
                soundTab.Show();
                soundButton.Pressed = true;
                break;
            case SelectedOptionsTab.Performance:
                performanceTab.Show();
                performanceButton.Pressed = true;
                break;
            case SelectedOptionsTab.Miscellaneous:
                miscTab.Show();
                miscButton.Pressed = true;
                break;
            default:
                GD.PrintErr("Invalid tab");
                break;
        }

        GUICommon.Instance.PlayButtonPressSound();
        selectedOptionsTab = selection;
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

    private void CompareSettings()
    {
        // Enable the save and reset buttons if the current setting values differ from the saved ones.
        if (Settings.Instance == savedSettings)
        {
            // Settings match
            resetButton.Disabled = true;
            saveButton.Disabled = true;
        }
        else
        {
            // Settings differ
            resetButton.Disabled = false;
            saveButton.Disabled = false;
        }
    }

    /*
      GUI Control Callbacks
    */

    private void OnBackPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // If any settings have been changed, show a dialogue asking if the changes should be kept or
        // discarded.
        if (Settings.Instance != savedSettings)
        {
            backConfirmationBox.PopupCenteredMinsize();
            return;
        }

        EmitSignal(nameof(OnOptionsClosed));
    }

    private void OnResetPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Restore and apply the old saved settings.
        Settings.Instance.LoadFromObject(savedSettings);
        Settings.ApplyAll();
        ApplySettingsToControls(Settings.Instance);

        CompareSettings();
    }

    private void OnSavePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Save the new settings to the config file.
        if (!Settings.Save())
        {
            GD.PrintErr("Failed to save new options menu settings.");
            return;
        }

        // Copy over the new saved settings.
        savedSettings = Settings.Instance.Clone();

        CompareSettings();
    }

    private void OnDefaultsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        defaultsConfirmationBox.PopupCenteredMinsize();
    }

    private void BackConfirmSelected()
    {
        Settings.Instance.LoadFromObject(savedSettings);
        Settings.ApplyAll();
        ApplySettingsToControls(Settings.Instance);

        CompareSettings();

        EmitSignal(nameof(OnOptionsClosed));
    }

    private void DefaultsConfirmSelected()
    {
        // Sets active settings to default values and applies them to the options controls.
        Settings.Instance.LoadDefaults();
        Settings.ApplyAll();
        ApplySettingsToControls(Settings.Instance);

        CompareSettings();
    }

    // Graphics Button Callbacks
    private void OnFullScreenToggled(bool pressed)
    {
        Settings.Instance.FullScreen = pressed;
        Settings.ApplyWindowSettings();

        CompareSettings();
    }

    private void OnVSyncToggled(bool pressed)
    {
        Settings.Instance.VSync = pressed;
        Settings.ApplyWindowSettings();

        CompareSettings();
    }

    private void OnMSAAResolutionSelected(int index)
    {
        Settings.Instance.MSAAResolution = MSAAIndexToResolution(index);
        Settings.ApplyGraphicsSettings();

        CompareSettings();
    }

    private void OnColourblindSettingSelected(int index)
    {
        Settings.Instance.ColourblindSetting = index;
        Settings.ApplyGraphicsSettings();

        CompareSettings();
    }

    private void OnChromaticAberrationToggled(bool toggle)
    {
        Settings.Instance.ChromaticEnabled = toggle;

        CompareSettings();
    }

    private void OnChromaticAberrationValueChanged(float amount)
    {
        Settings.Instance.ChromaticAmount = amount;

        CompareSettings();
    }

    // Sound Button Callbacks
    private void OnMasterMutedToggled(bool pressed)
    {
        Settings.Instance.VolumeMasterMuted = pressed;
        Settings.ApplySoundSettings();

        CompareSettings();
    }

    private void OnMusicMutedToggled(bool pressed)
    {
        Settings.Instance.VolumeMusicMuted = pressed;
        Settings.ApplySoundSettings();

        CompareSettings();
    }

    private void OnMasterVolumeChanged(float value)
    {
        Settings.Instance.VolumeMaster = ConvertSoundBarToDb(value);
        Settings.ApplySoundSettings();

        CompareSettings();
    }

    private void OnMusicVolumeChanged(float value)
    {
        Settings.Instance.VolumeMusic = ConvertSoundBarToDb(value);
        Settings.ApplySoundSettings();

        CompareSettings();
    }

    // Performance Button Callbacks
    private void OnCloudIntervalSelected(int index)
    {
        Settings.Instance.CloudUpdateInterval = CloudIndexToInterval(index);

        CompareSettings();
    }

    private void OnCloudResolutionSelected(int index)
    {
        Settings.Instance.CloudResolution = CloudIndexToResolution(index);

        CompareSettings();
    }

    // Misc Button Callbacks
    private void OnIntroToggled(bool pressed)
    {
        Settings.Instance.PlayIntroVideo = pressed;

        CompareSettings();
    }

    private void OnMicrobeIntroToggled(bool pressed)
    {
        Settings.Instance.PlayMicrobeIntroVideo = pressed;

        CompareSettings();
    }

    private void OnCheatsToggled(bool pressed)
    {
        Settings.Instance.CheatsEnabled = pressed;

        CompareSettings();
    }

    private void OnAutoSaveToggled(bool pressed)
    {
        Settings.Instance.AutoSaveEnabled = pressed;
        maxAutosaves.Editable = pressed;

        CompareSettings();
    }

    private void OnMaxAutoSavesValueChanged(float value)
    {
        Settings.Instance.MaxAutoSaves = (int)value;

        CompareSettings();
    }

    private void OnMaxQuickSavesValueChanged(float value)
    {
        Settings.Instance.MaxQuickSaves = (int)value;

        CompareSettings();
    }
}
