using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Saving;

/// <summary>
///   Handles the logic for the options menu GUI.
/// </summary>
public class OptionsMenu : ControlWithInput
{
    /*
      GUI Control Paths
    */

    // Options control buttons.

    [Export]
    public NodePath ResetButtonPath = null!;

    [Export]
    public NodePath SaveButtonPath = null!;

    // Tab selector buttons.
    [Export]
    public NodePath GraphicsButtonPath = null!;

    [Export]
    public NodePath SoundButtonPath = null!;

    [Export]
    public NodePath PerformanceButtonPath = null!;

    [Export]
    public NodePath InputsButtonPath = null!;

    [Export]
    public NodePath MiscButtonPath = null!;

    // Graphics tab.
    [Export]
    public NodePath GraphicsTabPath = null!;

    [Export]
    public NodePath VSyncPath = null!;

    [Export]
    public NodePath FullScreenPath = null!;

    [Export]
    public NodePath MSAAResolutionPath = null!;

    [Export]
    public NodePath ResolutionPath = null!;

    [Export]
    public NodePath MaxFramesPerSecondPath = null!;

    [Export]
    public NodePath ColourblindSettingPath = null!;

    [Export]
    public NodePath ChromaticAberrationSliderPath = null!;

    [Export]
    public NodePath ChromaticAberrationTogglePath = null!;

    [Export]
    public NodePath DisplayAbilitiesBarTogglePath = null!;

    [Export]
    public NodePath GUILightEffectsTogglePath = null!;

    // Sound tab.
    [Export]
    public NodePath SoundTabPath = null!;

    [Export]
    public NodePath MasterVolumePath = null!;

    [Export]
    public NodePath MasterMutedPath = null!;

    [Export]
    public NodePath MusicVolumePath = null!;

    [Export]
    public NodePath MusicMutedPath = null!;

    [Export]
    public NodePath AmbianceVolumePath = null!;

    [Export]
    public NodePath AmbianceMutedPath = null!;

    [Export]
    public NodePath SFXVolumePath = null!;

    [Export]
    public NodePath SFXMutedPath = null!;

    [Export]
    public NodePath GUIVolumePath = null!;

    [Export]
    public NodePath GUIMutedPath = null!;

    [Export]
    public NodePath AudioOutputDeviceSelectionPath = null!;

    [Export]
    public NodePath LanguageSelectionPath = null!;

    [Export]
    public NodePath ResetLanguageButtonPath = null!;

    [Export]
    public NodePath LanguageProgressLabelPath = null!;

    // Performance tab.
    [Export]
    public NodePath PerformanceTabPath = null!;

    [Export]
    public NodePath CloudIntervalPath = null!;

    [Export]
    public NodePath CloudResolutionTitlePath = null!;

    [Export]
    public NodePath CloudResolutionPath = null!;

    [Export]
    public NodePath RunAutoEvoDuringGameplayPath = null!;

    [Export]
    public NodePath DetectedCPUCountPath = null!;

    [Export]
    public NodePath ActiveThreadCountPath = null!;

    [Export]
    public NodePath AssumeHyperthreadingPath = null!;

    [Export]
    public NodePath UseManualThreadCountPath = null!;

    [Export]
    public NodePath ThreadCountSliderPath = null!;

    // Inputs tab.
    [Export]
    public NodePath InputsTabPath = null!;

    [Export]
    public NodePath InputGroupListPath = null!;

    // Misc tab.
    [Export]
    public NodePath MiscTabPath = null!;

    [Export]
    public NodePath PlayIntroPath = null!;

    [Export]
    public NodePath PlayMicrobeIntroPath = null!;

    [Export]
    public NodePath TutorialsEnabledOnNewGamePath = null!;

    [Export]
    public NodePath CheatsPath = null!;

    [Export]
    public NodePath AutoSavePath = null!;

    [Export]
    public NodePath MaxAutoSavesPath = null!;

    [Export]
    public NodePath MaxQuickSavesPath = null!;

    [Export]
    public NodePath BackConfirmationBoxPath = null!;

    [Export]
    public NodePath TutorialsEnabledPath = null!;

    [Export]
    public NodePath ScreenshotDirectoryWarningBoxPath = null!;

    [Export]
    public NodePath DefaultsConfirmationBoxPath = null!;

    [Export]
    public NodePath ErrorAcceptBoxPath = null!;

    [Export]
    public NodePath CustomUsernameEnabledPath = null!;

    [Export]
    public NodePath CustomUsernamePath = null!;

    [Export]
    public NodePath JSONDebugModePath = null!;

    [Export]
    public NodePath UnsavedProgressWarningPath = null!;

    private static readonly List<string> LanguagesCache = TranslationServer.GetLoadedLocales().Cast<string>()
        .OrderBy(i => i, StringComparer.InvariantCulture)
        .ToList();

    private static readonly List<string> AudioOutputDevicesCache = AudioServer
        .GetDeviceList().OfType<string>().Where(d => d != Constants.DEFAULT_AUDIO_OUTPUT_DEVICE_NAME)
        .Prepend(Constants.DEFAULT_AUDIO_OUTPUT_DEVICE_NAME).ToList();

    private Button resetButton = null!;
    private Button saveButton = null!;

    // Tab selector buttons
    private Button graphicsButton = null!;
    private Button soundButton = null!;
    private Button performanceButton = null!;
    private Button inputsButton = null!;
    private Button miscButton = null!;

    // Graphics tab
    private Control graphicsTab = null!;
    private CustomCheckBox vsync = null!;
    private CustomCheckBox fullScreen = null!;
    private Label resolution = null!;
    private OptionButton msaaResolution = null!;
    private OptionButton maxFramesPerSecond = null!;
    private OptionButton colourblindSetting = null!;
    private CustomCheckBox chromaticAberrationToggle = null!;
    private Slider chromaticAberrationSlider = null!;
    private CustomCheckBox displayAbilitiesHotBarToggle = null!;
    private CustomCheckBox guiLightEffectsToggle = null!;

    // Sound tab
    private Control soundTab = null!;
    private Slider masterVolume = null!;
    private CustomCheckBox masterMuted = null!;
    private Slider musicVolume = null!;
    private CustomCheckBox musicMuted = null!;
    private Slider ambianceVolume = null!;
    private CustomCheckBox ambianceMuted = null!;
    private Slider sfxVolume = null!;
    private CustomCheckBox sfxMuted = null!;
    private Slider guiVolume = null!;
    private CustomCheckBox guiMuted = null!;
    private OptionButton audioOutputDeviceSelection = null!;
    private OptionButton languageSelection = null!;
    private Button resetLanguageButton = null!;
    private Label languageProgressLabel = null!;

    // Performance tab
    private Control performanceTab = null!;
    private OptionButton cloudInterval = null!;
    private VBoxContainer cloudResolutionTitle = null!;
    private OptionButton cloudResolution = null!;
    private CustomCheckBox runAutoEvoDuringGameplay = null!;
    private Label detectedCPUCount = null!;
    private Label activeThreadCount = null!;
    private CustomCheckBox assumeHyperthreading = null!;
    private CustomCheckBox useManualThreadCount = null!;
    private Slider threadCountSlider = null!;

    // Inputs tab
    private Control inputsTab = null!;
    private InputGroupList inputGroupList = null!;

    // Misc tab
    private Control miscTab = null!;
    private CustomCheckBox playIntro = null!;
    private CustomCheckBox playMicrobeIntro = null!;
    private CustomCheckBox cheats = null!;
    private CustomCheckBox tutorialsEnabledOnNewGame = null!;
    private CustomCheckBox autoSave = null!;
    private SpinBox maxAutoSaves = null!;
    private SpinBox maxQuickSaves = null!;
    private CustomCheckBox customUsernameEnabled = null!;
    private LineEdit customUsername = null!;
    private OptionButton jsonDebugMode = null!;

    private CustomCheckBox tutorialsEnabled = null!;
    private CustomCheckBox unsavedProgressWarningEnabled = null!;

    // Confirmation Boxes
    private CustomConfirmationDialog screenshotDirectoryWarningBox = null!;
    private CustomDialog backConfirmationBox = null!;
    private CustomConfirmationDialog defaultsConfirmationBox = null!;
    private ErrorDialog errorAcceptBox = null!;

    /*
      Misc
    */

    private OptionsMode optionsMode;
    private SelectedOptionsTab selectedOptionsTab;

    /// <summary>
    ///   Copy of the settings object that should match what is saved to the configuration file,
    ///   used for comparing and restoring to previous state.
    /// </summary>
    private Settings savedSettings = null!;

    private bool savedTutorialsEnabled;

    private GameProperties? gameProperties;

    /*
      Signals
    */

    [Signal]
    public delegate void OnOptionsClosed();

    public enum OptionsMode
    {
        MainMenu,
        InGame,
    }

    private enum SelectedOptionsTab
    {
        Graphics,
        Sound,
        Performance,
        Inputs,
        Miscellaneous,
    }

    private static List<string> Languages => LanguagesCache;
    private static List<string> AudioOutputDevices => AudioOutputDevicesCache;

    public override void _Ready()
    {
        // Options control buttons
        resetButton = GetNode<Button>(ResetButtonPath);
        saveButton = GetNode<Button>(SaveButtonPath);

        // Tab selector buttons
        graphicsButton = GetNode<Button>(GraphicsButtonPath);
        soundButton = GetNode<Button>(SoundButtonPath);
        performanceButton = GetNode<Button>(PerformanceButtonPath);
        inputsButton = GetNode<Button>(InputsButtonPath);
        miscButton = GetNode<Button>(MiscButtonPath);

        // Graphics
        graphicsTab = GetNode<Control>(GraphicsTabPath);
        vsync = GetNode<CustomCheckBox>(VSyncPath);
        fullScreen = GetNode<CustomCheckBox>(FullScreenPath);
        msaaResolution = GetNode<OptionButton>(MSAAResolutionPath);
        resolution = GetNode<Label>(ResolutionPath);
        maxFramesPerSecond = GetNode<OptionButton>(MaxFramesPerSecondPath);
        colourblindSetting = GetNode<OptionButton>(ColourblindSettingPath);
        chromaticAberrationToggle = GetNode<CustomCheckBox>(ChromaticAberrationTogglePath);
        chromaticAberrationSlider = GetNode<Slider>(ChromaticAberrationSliderPath);
        displayAbilitiesHotBarToggle = GetNode<CustomCheckBox>(DisplayAbilitiesBarTogglePath);
        guiLightEffectsToggle = GetNode<CustomCheckBox>(GUILightEffectsTogglePath);

        // Sound
        soundTab = GetNode<Control>(SoundTabPath);
        masterVolume = GetNode<Slider>(MasterVolumePath);
        masterMuted = GetNode<CustomCheckBox>(MasterMutedPath);
        musicVolume = GetNode<Slider>(MusicVolumePath);
        musicMuted = GetNode<CustomCheckBox>(MusicMutedPath);
        ambianceVolume = GetNode<Slider>(AmbianceVolumePath);
        ambianceMuted = GetNode<CustomCheckBox>(AmbianceMutedPath);
        sfxVolume = GetNode<Slider>(SFXVolumePath);
        sfxMuted = GetNode<CustomCheckBox>(SFXMutedPath);
        guiVolume = GetNode<Slider>(GUIVolumePath);
        guiMuted = GetNode<CustomCheckBox>(GUIMutedPath);
        audioOutputDeviceSelection = GetNode<OptionButton>(AudioOutputDeviceSelectionPath);
        languageSelection = GetNode<OptionButton>(LanguageSelectionPath);
        resetLanguageButton = GetNode<Button>(ResetLanguageButtonPath);
        languageProgressLabel = GetNode<Label>(LanguageProgressLabelPath);

        LoadLanguages();
        LoadAudioOutputDevices();

        // Performance
        performanceTab = GetNode<Control>(PerformanceTabPath);
        cloudInterval = GetNode<OptionButton>(CloudIntervalPath);
        cloudResolutionTitle = GetNode<VBoxContainer>(CloudResolutionTitlePath);
        cloudResolution = GetNode<OptionButton>(CloudResolutionPath);
        runAutoEvoDuringGameplay = GetNode<CustomCheckBox>(RunAutoEvoDuringGameplayPath);
        detectedCPUCount = GetNode<Label>(DetectedCPUCountPath);
        activeThreadCount = GetNode<Label>(ActiveThreadCountPath);
        assumeHyperthreading = GetNode<CustomCheckBox>(AssumeHyperthreadingPath);
        useManualThreadCount = GetNode<CustomCheckBox>(UseManualThreadCountPath);
        threadCountSlider = GetNode<Slider>(ThreadCountSliderPath);

        // Inputs
        inputsTab = GetNode<Control>(InputsTabPath);
        inputGroupList = GetNode<InputGroupList>(InputGroupListPath);
        inputGroupList.OnControlsChanged += OnControlsChanged;

        // Misc
        miscTab = GetNode<Control>(MiscTabPath);
        playIntro = GetNode<CustomCheckBox>(PlayIntroPath);
        playMicrobeIntro = GetNode<CustomCheckBox>(PlayMicrobeIntroPath);
        tutorialsEnabledOnNewGame = GetNode<CustomCheckBox>(TutorialsEnabledOnNewGamePath);
        cheats = GetNode<CustomCheckBox>(CheatsPath);
        autoSave = GetNode<CustomCheckBox>(AutoSavePath);
        maxAutoSaves = GetNode<SpinBox>(MaxAutoSavesPath);
        maxQuickSaves = GetNode<SpinBox>(MaxQuickSavesPath);
        tutorialsEnabled = GetNode<CustomCheckBox>(TutorialsEnabledPath);
        customUsernameEnabled = GetNode<CustomCheckBox>(CustomUsernameEnabledPath);
        customUsername = GetNode<LineEdit>(CustomUsernamePath);
        jsonDebugMode = GetNode<OptionButton>(JSONDebugModePath);
        unsavedProgressWarningEnabled = GetNode<CustomCheckBox>(UnsavedProgressWarningPath);

        screenshotDirectoryWarningBox = GetNode<CustomConfirmationDialog>(ScreenshotDirectoryWarningBoxPath);
        backConfirmationBox = GetNode<CustomDialog>(BackConfirmationBoxPath);
        defaultsConfirmationBox = GetNode<CustomConfirmationDialog>(DefaultsConfirmationBoxPath);
        errorAcceptBox = GetNode<ErrorDialog>(ErrorAcceptBoxPath);

        selectedOptionsTab = SelectedOptionsTab.Graphics;

        cloudResolutionTitle.RegisterToolTipForControl("cloudResolution", "options");
        guiLightEffectsToggle.RegisterToolTipForControl("guiLightEffects", "options");
        assumeHyperthreading.RegisterToolTipForControl("assumeHyperthreading", "options");
        unsavedProgressWarningEnabled.RegisterToolTipForControl("unsavedProgressWarning", "options");
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            BuildInputRebindControls();
            UpdateDefaultAudioOutputDeviceText();
            DisplayResolution();
        }
        else if (what == NotificationResized)
        {
            DisplayResolution();
        }
    }

    /// <summary>
    ///   Opens the options menu with main menu configuration settings.
    /// </summary>
    public void OpenFromMainMenu()
    {
        // Shouldn't do anything if options is already open.
        if (Visible)
            return;

        // Copy the live game settings so we can check against them for changes.
        savedSettings = Settings.Instance.Clone();

        // Set the mode to the one we opened with, and disable any options that should only be visible in game.
        SwitchMode(OptionsMode.MainMenu);

        // Set the state of the gui controls to match the settings.
        ApplySettingsToControls(savedSettings);
        UpdateResetSaveButtonState();

        Show();
    }

    /// <summary>
    ///   Opens the options menu with in game settings.
    /// </summary>
    public void OpenFromInGame(GameProperties gameProperties)
    {
        // Shouldn't do anything if options is already open.
        if (Visible)
            return;

        // Copy the live game settings so we can check against them for changes.
        savedSettings = Settings.Instance.Clone();
        savedTutorialsEnabled = gameProperties.TutorialState.Enabled;

        // Need a reference to game properties in the current game for later comparisons.
        this.gameProperties = gameProperties;

        // Set the mode to the one we opened with, and show/hide any options that should only be visible
        // when the options menu is opened from in-game.
        SwitchMode(OptionsMode.InGame);

        // Set the state of the gui controls to match the settings.
        if (savedTutorialsEnabled)
            tutorialsEnabled.Pressed = savedTutorialsEnabled;

        ApplySettingsToControls(savedSettings);
        UpdateResetSaveButtonState();

        Show();
    }

    /// <summary>
    ///   Applies the values of a settings object to all corresponding options menu GUI controls.
    /// </summary>
    public void ApplySettingsToControls(Settings settings)
    {
        // Graphics
        vsync.Pressed = settings.VSync;
        fullScreen.Pressed = settings.FullScreen;
        msaaResolution.Selected = MSAAResolutionToIndex(settings.MSAAResolution);
        maxFramesPerSecond.Selected = MaxFPSValueToIndex(settings.MaxFramesPerSecond);
        colourblindSetting.Selected = settings.ColourblindSetting;
        chromaticAberrationSlider.Value = settings.ChromaticAmount;
        chromaticAberrationToggle.Pressed = settings.ChromaticEnabled;
        displayAbilitiesHotBarToggle.Pressed = settings.DisplayAbilitiesHotBar;
        guiLightEffectsToggle.Pressed = settings.GUILightEffectsEnabled;
        DisplayResolution();

        // Sound
        masterVolume.Value = ConvertDBToSoundBar(settings.VolumeMaster);
        masterMuted.Pressed = settings.VolumeMasterMuted;
        musicVolume.Value = ConvertDBToSoundBar(settings.VolumeMusic);
        musicMuted.Pressed = settings.VolumeMusicMuted;
        ambianceVolume.Value = ConvertDBToSoundBar(settings.VolumeAmbiance);
        ambianceMuted.Pressed = settings.VolumeAmbianceMuted;
        sfxVolume.Value = ConvertDBToSoundBar(settings.VolumeSFX);
        sfxMuted.Pressed = settings.VolumeSFXMuted;
        guiVolume.Value = ConvertDBToSoundBar(settings.VolumeGUI);
        guiMuted.Pressed = settings.VolumeGUIMuted;
        UpdateSelectedLanguage(settings);
        UpdateSelectedAudioOutputDevice(settings);

        // Hide or show the reset language button based on the selected language
        resetLanguageButton.Visible = settings.SelectedLanguage.Value != null &&
            settings.SelectedLanguage.Value != Settings.DefaultLanguage;
        UpdateCurrentLanguageProgress();

        // Performance
        cloudInterval.Selected = CloudIntervalToIndex(settings.CloudUpdateInterval);
        cloudResolution.Selected = CloudResolutionToIndex(settings.CloudResolution);
        runAutoEvoDuringGameplay.Pressed = settings.RunAutoEvoDuringGamePlay;
        assumeHyperthreading.Pressed = settings.AssumeCPUHasHyperthreading;
        useManualThreadCount.Pressed = settings.UseManualThreadCount;
        threadCountSlider.Value = settings.ThreadCount;
        threadCountSlider.Editable = settings.UseManualThreadCount;

        UpdateDetectedCPUCount();

        // Input
        BuildInputRebindControls();

        // Misc
        playIntro.Pressed = settings.PlayIntroVideo;
        playMicrobeIntro.Pressed = settings.PlayMicrobeIntroVideo;
        tutorialsEnabledOnNewGame.Pressed = settings.TutorialsEnabled;
        cheats.Pressed = settings.CheatsEnabled;
        autoSave.Pressed = settings.AutoSaveEnabled;
        maxAutoSaves.Value = settings.MaxAutoSaves;
        maxAutoSaves.Editable = settings.AutoSaveEnabled;
        maxQuickSaves.Value = settings.MaxQuickSaves;
        customUsernameEnabled.Pressed = settings.CustomUsernameEnabled;
        customUsername.Text = settings.CustomUsername.Value != null ?
            settings.CustomUsername :
            Settings.EnvironmentUserName;
        customUsername.Editable = settings.CustomUsernameEnabled;
        jsonDebugMode.Selected = JSONDebugModeToIndex(settings.JSONDebugMode);
        unsavedProgressWarningEnabled.Pressed = settings.ShowUnsavedProgressWarning;
    }

    [RunOnKeyDown("ui_cancel", Priority = Constants.SUBMENU_CANCEL_PRIORITY)]
    public bool OnEscapePressed()
    {
        // Only handle keypress when visible
        if (!Visible)
            return false;

        if (!Exit())
        {
            // We are prevented from exiting, consume this input
            return true;
        }

        // If it is opened from InGame then let pause menu hide too.
        if (optionsMode == OptionsMode.InGame)
        {
            return false;
        }

        return true;
    }

    private void SwitchMode(OptionsMode mode)
    {
        switch (mode)
        {
            case OptionsMode.MainMenu:
            {
                tutorialsEnabled.Hide();
                optionsMode = OptionsMode.MainMenu;
                break;
            }

            case OptionsMode.InGame:
            {
                // Current game tutorial option shouldn't be visible in freebuild mode.
                if (!gameProperties!.FreeBuild)
                    tutorialsEnabled.Show();
                else
                    tutorialsEnabled.Hide();

                optionsMode = OptionsMode.InGame;
                break;
            }

            default:
                throw new ArgumentException("Options menu SwitchMode called with an invalid mode argument");
        }
    }

    /// <summary>
    /// Displays the current viewport resolutuion
    /// </summary>
    private void DisplayResolution()
    {
        if (resolution == null)
            return;
        var screenResolution = GetViewportRect().Size;
        resolution.Text = string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("AUTO_RESOLUTION"), screenResolution.x, screenResolution.y);
    }

    /// <summary>
    ///   Changes the active settings tab that is displayed, or returns if the tab is already active.
    /// </summary>
    private void ChangeSettingsTab(string newTabName)
    {
        // Convert from the string binding to an enum.
        SelectedOptionsTab selection = (SelectedOptionsTab)Enum.Parse(typeof(SelectedOptionsTab), newTabName);

        // Pressing the same button that's already active, so just return.
        if (selection == selectedOptionsTab)
            return;

        graphicsTab.Hide();
        soundTab.Hide();
        performanceTab.Hide();
        inputsTab.Hide();
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
            case SelectedOptionsTab.Inputs:
                inputsTab.Show();
                inputsButton.Pressed = true;
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
        return GD.Linear2Db(value / 100.0f);
    }

    private float ConvertDBToSoundBar(float value)
    {
        return GD.Db2Linear(value) * 100.0f;
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

    private int MaxFPSValueToIndex(int value)
    {
        switch (value)
        {
            case 30:
                return 0;
            case 60:
                return 1;
            case 90:
                return 2;
            case 120:
                return 3;
            case 144:
                return 4;
            case 240:
                return 5;
            case 360:
                return 6;
            case 1000:
                return 7;
            case 0:
                return 8;
            default:
                GD.PrintErr("invalid max frames per second value");
                return 6;
        }
    }

    private int MaxFPSIndexToValue(int index)
    {
        switch (index)
        {
            case 0:
                return 30;
            case 1:
                return 60;
            case 2:
                return 90;
            case 3:
                return 120;
            case 4:
                return 144;
            case 5:
                return 240;
            case 6:
                return 360;
            case 7:
                return 1000;
            case 8:
                return 0;
            default:
                GD.PrintErr("invalid max frames per second index");
                return 360;
        }
    }

    private int JSONDebugModeToIndex(JSONDebug.DebugMode mode)
    {
        switch (mode)
        {
            case JSONDebug.DebugMode.AlwaysDisabled:
                return 2;
            case JSONDebug.DebugMode.Automatic:
                return 0;
            case JSONDebug.DebugMode.AlwaysEnabled:
                return 1;
        }

        GD.PrintErr("invalid JSON debug mode value");
        return 0;
    }

    private JSONDebug.DebugMode JSONDebugIndexToMode(int index)
    {
        switch (index)
        {
            case 0:
                return JSONDebug.DebugMode.Automatic;
            case 1:
                return JSONDebug.DebugMode.AlwaysEnabled;
            case 2:
                return JSONDebug.DebugMode.AlwaysDisabled;
            default:
                GD.PrintErr("invalid JSON debug mode index");
                return JSONDebug.DebugMode.Automatic;
        }
    }

    /// <summary>
    ///   Returns whether current settings match their saved originals. Settings that are
    ///   inactive due to a different options menu mode will not be used in the comparison.
    /// </summary>
    private bool CompareSettings()
    {
        // Compare global settings.
        if (Settings.Instance != savedSettings)
            return false;

        // If we're in game we need to compare the tutorials enabled state as well.
        if (optionsMode == OptionsMode.InGame)
        {
            if (gameProperties!.TutorialState.Enabled != savedTutorialsEnabled)
            {
                return false;
            }
        }

        // All active settings match.
        return true;
    }

    private void UpdateResetSaveButtonState()
    {
        // Enable the save and reset buttons if the current setting values differ from the saved ones.
        bool result = CompareSettings();

        resetButton.Disabled = result;
        saveButton.Disabled = result;
    }

    private void UpdateDefaultAudioOutputDeviceText()
    {
        audioOutputDeviceSelection.SetItemText(0, TranslationServer.Translate("DEFAULT_AUDIO_OUTPUT_DEVICE"));
    }

    private void LoadAudioOutputDevices()
    {
        foreach (var audioOutputDevice in AudioOutputDevices)
        {
            audioOutputDeviceSelection.AddItem(audioOutputDevice);
        }

        UpdateDefaultAudioOutputDeviceText();
    }

    private void LoadLanguages()
    {
        foreach (var locale in Languages)
        {
            var currentCulture = Settings.GetCultureInfo(locale);
            var native = Settings.GetLanguageNativeNameOverride(locale) ?? currentCulture.NativeName;
            languageSelection.AddItem(locale + " - " + native);
        }
    }

    private void UpdateCurrentLanguageProgress()
    {
        string locale = TranslationServer.GetLocale();
        float progress = 100 * SimulationParameters.Instance.GetTranslationsInfo().TranslationProgress[locale];

        string textFormat;

        if (progress >= 0 && progress < Constants.TRANSLATION_VERY_INCOMPLETE_THRESHOLD)
        {
            textFormat = TranslationServer.Translate("LANGUAGE_TRANSLATION_PROGRESS_REALLY_LOW");
        }
        else if (progress >= 0 && progress < Constants.TRANSLATION_INCOMPLETE_THRESHOLD)
        {
            textFormat = TranslationServer.Translate("LANGUAGE_TRANSLATION_PROGRESS_LOW");
        }
        else
        {
            textFormat = TranslationServer.Translate("LANGUAGE_TRANSLATION_PROGRESS");
        }

        languageProgressLabel.Text = string.Format(CultureInfo.CurrentCulture, textFormat, Mathf.Floor(progress));
    }

    /*
      GUI Control Callbacks
    */

    private void OnBackPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        Exit();
    }

    private bool Exit()
    {
        // If any settings have been changed, show a dialogue asking if the changes should be kept or
        // discarded.
        if (!CompareSettings())
        {
            backConfirmationBox.PopupCenteredShrink();
            return false;
        }

        EmitSignal(nameof(OnOptionsClosed));
        return true;
    }

    private void UpdateDetectedCPUCount()
    {
        detectedCPUCount.Text = TaskExecutor.CPUCount.ToString(CultureInfo.CurrentCulture);

        if (Settings.Instance.UseManualThreadCount)
        {
            activeThreadCount.Text = Settings.Instance.ThreadCount.Value.ToString(CultureInfo.CurrentCulture);
        }
        else
        {
            int threads = TaskExecutor.GetWantedThreadCount(Settings.Instance.AssumeCPUHasHyperthreading,
                Settings.Instance.RunAutoEvoDuringGamePlay);

            activeThreadCount.Text = threads.ToString(CultureInfo.CurrentCulture);
            threadCountSlider.Value = threads;
        }

        threadCountSlider.MinValue = TaskExecutor.MinimumThreadCount;
        threadCountSlider.MaxValue = TaskExecutor.MaximumThreadCount;
    }

    private void OnResetPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Restore and apply the old saved settings.
        Settings.Instance.LoadFromObject(savedSettings);
        Settings.Instance.ApplyAll();
        ApplySettingsToControls(Settings.Instance);

        if (optionsMode == OptionsMode.InGame)
        {
            gameProperties!.TutorialState.Enabled = savedTutorialsEnabled;
            tutorialsEnabled.Pressed = savedTutorialsEnabled;
        }

        UpdateResetSaveButtonState();
    }

    private void OnSavePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Save the new settings to the config file.
        if (!Settings.Instance.Save())
        {
            GD.PrintErr("Failed to save new options menu settings to configuration file.");
            errorAcceptBox.PopupCenteredShrink();
            return;
        }

        // Copy over the new saved settings.
        savedSettings = Settings.Instance.Clone();

        if (optionsMode == OptionsMode.InGame)
            savedTutorialsEnabled = gameProperties!.TutorialState.Enabled;

        UpdateResetSaveButtonState();
    }

    private void OnDefaultsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        defaultsConfirmationBox.PopupCenteredShrink();
    }

    private void BackSaveSelected()
    {
        // Save the new settings to the config file.
        if (!Settings.Instance.Save())
        {
            GD.PrintErr("Failed to save new options menu settings to configuration file.");
            backConfirmationBox.Hide();
            errorAcceptBox.PopupCenteredShrink();

            return;
        }

        // Copy over the new saved settings.
        savedSettings = Settings.Instance.Clone();
        backConfirmationBox.Hide();

        UpdateResetSaveButtonState();
        EmitSignal(nameof(OnOptionsClosed));
    }

    private void BackDiscardSelected()
    {
        Settings.Instance.LoadFromObject(savedSettings);
        Settings.Instance.ApplyAll();
        ApplySettingsToControls(Settings.Instance);

        if (optionsMode == OptionsMode.InGame)
        {
            gameProperties!.TutorialState.Enabled = savedTutorialsEnabled;
            tutorialsEnabled.Pressed = savedTutorialsEnabled;
        }

        backConfirmationBox.Hide();

        UpdateResetSaveButtonState();
        EmitSignal(nameof(OnOptionsClosed));
    }

    private void BackCancelSelected()
    {
        backConfirmationBox.Hide();
    }

    private void InputDefaultsConfirm()
    {
        Settings.Instance.CurrentControls.Value = Settings.GetDefaultControls();
        Settings.Instance.ApplyInputSettings();
        BuildInputRebindControls();

        UpdateResetSaveButtonState();
    }

    private void DefaultsConfirmSelected()
    {
        // Sets active settings to default values and applies them to the options controls.
        Settings.Instance.LoadDefaults();
        Settings.Instance.ApplyAll();
        ApplySettingsToControls(Settings.Instance);

        UpdateResetSaveButtonState();
    }

    // Graphics Button Callbacks
    private void OnFullScreenToggled(bool pressed)
    {
        Settings.Instance.FullScreen.Value = pressed;
        Settings.Instance.ApplyWindowSettings();

        UpdateResetSaveButtonState();
    }

    private void OnVSyncToggled(bool pressed)
    {
        Settings.Instance.VSync.Value = pressed;
        Settings.Instance.ApplyWindowSettings();

        UpdateResetSaveButtonState();
    }

    private void OnMSAAResolutionSelected(int index)
    {
        Settings.Instance.MSAAResolution.Value = MSAAIndexToResolution(index);
        Settings.Instance.ApplyGraphicsSettings();

        UpdateResetSaveButtonState();
    }

    private void OnMaxFramesPerSecondSelected(int index)
    {
        Settings.Instance.MaxFramesPerSecond.Value = MaxFPSIndexToValue(index);
        Settings.Instance.ApplyGraphicsSettings();

        UpdateResetSaveButtonState();
    }

    private void OnColourblindSettingSelected(int index)
    {
        Settings.Instance.ColourblindSetting.Value = index;
        Settings.Instance.ApplyGraphicsSettings();

        UpdateResetSaveButtonState();
    }

    private void OnChromaticAberrationToggled(bool toggle)
    {
        Settings.Instance.ChromaticEnabled.Value = toggle;

        UpdateResetSaveButtonState();
    }

    private void OnChromaticAberrationValueChanged(float amount)
    {
        Settings.Instance.ChromaticAmount.Value = amount;

        UpdateResetSaveButtonState();
    }

    private void OnDisplayAbilitiesHotBarToggled(bool toggle)
    {
        Settings.Instance.DisplayAbilitiesHotBar.Value = toggle;

        UpdateResetSaveButtonState();
    }

    private void OnGUILightEffectsToggled(bool toggle)
    {
        Settings.Instance.GUILightEffectsEnabled.Value = toggle;

        UpdateResetSaveButtonState();
    }

    // Sound Button Callbacks
    private void OnMasterVolumeChanged(float value)
    {
        Settings.Instance.VolumeMaster.Value = ConvertSoundBarToDb(value);
        Settings.Instance.ApplySoundSettings();

        UpdateResetSaveButtonState();
    }

    private void OnMasterMutedToggled(bool pressed)
    {
        Settings.Instance.VolumeMasterMuted.Value = pressed;
        Settings.Instance.ApplySoundSettings();

        UpdateResetSaveButtonState();
    }

    private void OnMusicVolumeChanged(float value)
    {
        Settings.Instance.VolumeMusic.Value = ConvertSoundBarToDb(value);
        Settings.Instance.ApplySoundSettings();

        UpdateResetSaveButtonState();
    }

    private void OnMusicMutedToggled(bool pressed)
    {
        Settings.Instance.VolumeMusicMuted.Value = pressed;
        Settings.Instance.ApplySoundSettings();

        UpdateResetSaveButtonState();
    }

    private void OnAmbianceVolumeChanged(float value)
    {
        Settings.Instance.VolumeAmbiance.Value = ConvertSoundBarToDb(value);
        Settings.Instance.ApplySoundSettings();

        UpdateResetSaveButtonState();
    }

    private void OnAmbianceMutedToggled(bool pressed)
    {
        Settings.Instance.VolumeAmbianceMuted.Value = pressed;
        Settings.Instance.ApplySoundSettings();

        UpdateResetSaveButtonState();
    }

    private void OnSFXVolumeChanged(float value)
    {
        Settings.Instance.VolumeSFX.Value = ConvertSoundBarToDb(value);
        Settings.Instance.ApplySoundSettings();

        UpdateResetSaveButtonState();
    }

    private void OnSFXMutedToggled(bool pressed)
    {
        Settings.Instance.VolumeSFXMuted.Value = pressed;
        Settings.Instance.ApplySoundSettings();

        UpdateResetSaveButtonState();
    }

    private void OnGUIVolumeChanged(float value)
    {
        Settings.Instance.VolumeGUI.Value = ConvertSoundBarToDb(value);
        Settings.Instance.ApplySoundSettings();

        UpdateResetSaveButtonState();
    }

    private void OnGUIMutedToggled(bool pressed)
    {
        Settings.Instance.VolumeGUIMuted.Value = pressed;
        Settings.Instance.ApplySoundSettings();

        UpdateResetSaveButtonState();
    }

    // Performance Button Callbacks
    private void OnCloudIntervalSelected(int index)
    {
        Settings.Instance.CloudUpdateInterval.Value = CloudIndexToInterval(index);

        UpdateResetSaveButtonState();
    }

    private void OnCloudResolutionSelected(int index)
    {
        Settings.Instance.CloudResolution.Value = CloudIndexToResolution(index);

        UpdateResetSaveButtonState();
    }

    private void OnAutoEvoToggled(bool pressed)
    {
        Settings.Instance.RunAutoEvoDuringGamePlay.Value = pressed;

        UpdateResetSaveButtonState();
        UpdateDetectedCPUCount();
    }

    private void OnHyperthreadingToggled(bool pressed)
    {
        Settings.Instance.AssumeCPUHasHyperthreading.Value = pressed;
        Settings.Instance.ApplyThreadSettings();

        UpdateResetSaveButtonState();
        UpdateDetectedCPUCount();
    }

    private void OnManualThreadsToggled(bool pressed)
    {
        Settings.Instance.UseManualThreadCount.Value = pressed;
        Settings.Instance.ApplyThreadSettings();

        threadCountSlider.Editable = pressed;

        UpdateResetSaveButtonState();
        UpdateDetectedCPUCount();
    }

    private void OnManualThreadCountChanged(float value)
    {
        int threads = Mathf.Clamp((int)value, TaskExecutor.MinimumThreadCount, TaskExecutor.MaximumThreadCount);
        Settings.Instance.ThreadCount.Value = threads;
        Settings.Instance.ApplyThreadSettings();

        UpdateResetSaveButtonState();
        UpdateDetectedCPUCount();
    }

    // Input Callbacks
    private void OnControlsChanged(InputDataList data)
    {
        Settings.Instance.CurrentControls.Value = data;
        Settings.Instance.ApplyInputSettings();
        UpdateResetSaveButtonState();
    }

    // Misc Button Callbacks
    private void OnIntroToggled(bool pressed)
    {
        Settings.Instance.PlayIntroVideo.Value = pressed;

        UpdateResetSaveButtonState();
    }

    private void OnMicrobeIntroToggled(bool pressed)
    {
        Settings.Instance.PlayMicrobeIntroVideo.Value = pressed;

        UpdateResetSaveButtonState();
    }

    private void OnTutorialsOnNewGameToggled(bool pressed)
    {
        Settings.Instance.TutorialsEnabled.Value = pressed;

        UpdateResetSaveButtonState();
    }

    private void OnCheatsToggled(bool pressed)
    {
        Settings.Instance.CheatsEnabled.Value = pressed;
        if (!pressed)
        {
            CheatManager.OnCheatsDisabled();
        }

        UpdateResetSaveButtonState();
    }

    private void OnAutoSaveToggled(bool pressed)
    {
        Settings.Instance.AutoSaveEnabled.Value = pressed;
        maxAutoSaves.Editable = pressed;

        UpdateResetSaveButtonState();
    }

    private void OnMaxAutoSavesValueChanged(float value)
    {
        Settings.Instance.MaxAutoSaves.Value = (int)value;

        UpdateResetSaveButtonState();
    }

    private void OnMaxQuickSavesValueChanged(float value)
    {
        Settings.Instance.MaxQuickSaves.Value = (int)value;

        UpdateResetSaveButtonState();
    }

    private void OnTutorialsEnabledToggled(bool pressed)
    {
        if (gameProperties == null)
        {
            GD.PrintErr("Game tutorials toggle signal received but game properties is null");
            return;
        }

        gameProperties.TutorialState.Enabled = pressed;

        UpdateResetSaveButtonState();
    }

    private void OnJSONDebugModeSelected(int index)
    {
        Settings.Instance.JSONDebugMode.Value = JSONDebugIndexToMode(index);

        UpdateResetSaveButtonState();
    }

    private void OnUnsavedProgressWarningToggled(bool pressed)
    {
        Settings.Instance.ShowUnsavedProgressWarning.Value = pressed;

        UpdateResetSaveButtonState();
    }

    private void BuildInputRebindControls()
    {
        inputGroupList.InitGroupList();
    }

    private void OnOpenScreenshotFolder()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (!FolderHelpers.OpenFolder(Constants.SCREENSHOT_FOLDER))
            screenshotDirectoryWarningBox.PopupCenteredShrink();
    }

    private void OnCustomUsernameEnabledToggled(bool pressed)
    {
        Settings.Instance.CustomUsernameEnabled.Value = pressed;
        customUsername.Editable = pressed;

        UpdateResetSaveButtonState();
    }

    private void OnCustomUsernameTextChanged(string text)
    {
        if (text.Equals(Settings.EnvironmentUserName, StringComparison.CurrentCulture))
        {
            Settings.Instance.CustomUsername.Value = null;
        }
        else
        {
            Settings.Instance.CustomUsername.Value = text;
        }

        UpdateResetSaveButtonState();
    }

    private void OnAudioOutputDeviceSettingSelected(int item)
    {
        Settings.Instance.SelectedAudioOutputDevice.Value = AudioOutputDevices[item];

        Settings.Instance.ApplyAudioOutputDeviceSettings();
        UpdateResetSaveButtonState();
    }

    private void OnLanguageSettingSelected(int item)
    {
        Settings.Instance.SelectedLanguage.Value = Languages[item];
        resetLanguageButton.Visible = true;

        Settings.Instance.ApplyLanguageSettings();
        UpdateResetSaveButtonState();
        UpdateCurrentLanguageProgress();
    }

    private void OnResetLanguagePressed()
    {
        Settings.Instance.SelectedLanguage.Value = null;
        resetLanguageButton.Visible = false;

        Settings.Instance.ApplyLanguageSettings();
        UpdateSelectedLanguage(Settings.Instance);
        UpdateResetSaveButtonState();
        UpdateCurrentLanguageProgress();
    }

    private void OnTranslationSitePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        OS.ShellOpen("https://translate.revolutionarygamesstudio.com/engage/thrive/");
    }

    private void UpdateSelectedAudioOutputDevice(Settings settings)
    {
        audioOutputDeviceSelection.Selected = AudioOutputDevices.IndexOf(settings.SelectedAudioOutputDevice.Value ??
            Constants.DEFAULT_AUDIO_OUTPUT_DEVICE_NAME);
    }

    private void UpdateSelectedLanguage(Settings settings)
    {
        languageSelection.Selected = Languages.IndexOf(settings.SelectedLanguage.Value ?? Settings.DefaultLanguage);
    }

    private void OnLogButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        FolderHelpers.OpenFolder(Constants.LOGS_FOLDER);
    }
}
