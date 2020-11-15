using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Environment = System.Environment;

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

    [Export]
    public NodePath AmbianceVolumePath;

    [Export]
    public NodePath AmbianceMutedPath;

    [Export]
    public NodePath SFXVolumePath;

    [Export]
    public NodePath SFXMutedPath;

    [Export]
    public NodePath GUIVolumePath;

    [Export]
    public NodePath GUIMutedPath;

    // Performance tab.
    [Export]
    public NodePath PerformanceTabPath;

    [Export]
    public NodePath CloudIntervalPath;

    [Export]
    public NodePath CloudResolutionPath;

    [Export]
    public NodePath RunAutoEvoDuringGameplayPath;

    // Misc tab.
    [Export]
    public NodePath MiscTabPath;

    [Export]
    public NodePath PlayIntroPath;

    [Export]
    public NodePath PlayMicrobeIntroPath;

    [Export]
    public NodePath TutorialsEnabledOnNewGamePath;

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
    public NodePath TutorialsEnabledPath;

    [Export]
    public NodePath DefaultsConfirmationBoxPath;

    [Export]
    public NodePath ErrorAcceptBoxPath;

    [Export]
    public NodePath LanguageSelectionPath;

    [Export]
    public NodePath ResetLanguageButtonPath;

    [Export]
    public NodePath CustomUsernameEnabledPath;

    [Export]
    public NodePath CustomUsernamePath;

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
    private Slider ambianceVolume;
    private CheckBox ambianceMuted;
    private Slider sfxVolume;
    private CheckBox sfxMuted;
    private Slider guiVolume;
    private CheckBox guiMuted;
    private OptionButton languageSelection;
    private Button resetLanguageButton;
    private List<string> languages;

    // Performance tab
    private Control performanceTab;
    private OptionButton cloudInterval;
    private OptionButton cloudResolution;
    private CheckBox runAutoEvoDuringGameplay;

    // Misc tab
    private Control miscTab;
    private CheckBox playIntro;
    private CheckBox playMicrobeIntro;
    private CheckBox cheats;
    private CheckBox tutorialsEnabledOnNewGame;
    private CheckBox autosave;
    private SpinBox maxAutosaves;
    private SpinBox maxQuicksaves;
    private CheckBox customUsernameEnabled;
    private LineEdit customUsername;

    private CheckBox tutorialsEnabled;

    // Confirmation Boxes
    private WindowDialog backConfirmationBox;
    private ConfirmationDialog defaultsConfirmationBox;
    private AcceptDialog errorAcceptBox;

    /*
      Misc
    */

    private OptionsMode optionsMode;
    private SelectedOptionsTab selectedOptionsTab;

    /// <summary>
    ///   Copy of the settings object that should match what is saved to the configuration file,
    ///   used for comparing and restoring to previous state.
    /// </summary>
    private Settings savedSettings;

    private bool savedTutorialsEnabled;

    private GameProperties gameProperties;

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
        Miscellaneous,
    }

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
        ambianceVolume = GetNode<Slider>(AmbianceVolumePath);
        ambianceMuted = GetNode<CheckBox>(AmbianceMutedPath);
        sfxVolume = GetNode<Slider>(SFXVolumePath);
        sfxMuted = GetNode<CheckBox>(SFXMutedPath);
        guiVolume = GetNode<Slider>(GUIVolumePath);
        guiMuted = GetNode<CheckBox>(GUIMutedPath);
        languageSelection = GetNode<OptionButton>(LanguageSelectionPath);
        resetLanguageButton = GetNode<Button>(ResetLanguageButtonPath);
        LoadLanguages(languageSelection);

        // Performance
        performanceTab = GetNode<Control>(PerformanceTabPath);
        cloudInterval = GetNode<OptionButton>(CloudIntervalPath);
        cloudResolution = GetNode<OptionButton>(CloudResolutionPath);
        runAutoEvoDuringGameplay = GetNode<CheckBox>(RunAutoEvoDuringGameplayPath);

        // Misc
        miscTab = GetNode<Control>(MiscTabPath);
        playIntro = GetNode<CheckBox>(PlayIntroPath);
        playMicrobeIntro = GetNode<CheckBox>(PlayMicrobeIntroPath);
        tutorialsEnabledOnNewGame = GetNode<CheckBox>(TutorialsEnabledOnNewGamePath);
        cheats = GetNode<CheckBox>(CheatsPath);
        autosave = GetNode<CheckBox>(AutoSavePath);
        maxAutosaves = GetNode<SpinBox>(MaxAutoSavesPath);
        maxQuicksaves = GetNode<SpinBox>(MaxQuickSavesPath);
        tutorialsEnabled = GetNode<CheckBox>(TutorialsEnabledPath);
        customUsernameEnabled = GetNode<CheckBox>(CustomUsernameEnabledPath);
        customUsername = GetNode<LineEdit>(CustomUsernamePath);

        backConfirmationBox = GetNode<WindowDialog>(BackConfirmationBoxPath);
        defaultsConfirmationBox = GetNode<ConfirmationDialog>(DefaultsConfirmationBoxPath);
        errorAcceptBox = GetNode<AcceptDialog>(ErrorAcceptBoxPath);

        selectedOptionsTab = SelectedOptionsTab.Graphics;
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
        colourblindSetting.Selected = settings.ColourblindSetting;
        chromaticAberrationSlider.Value = settings.ChromaticAmount;
        chromaticAberrationToggle.Pressed = settings.ChromaticEnabled;

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

        // Hide or show the reset language button based on the selected language
        resetLanguageButton.Visible = settings.SelectedLanguage.Value != null &&
            settings.SelectedLanguage.Value != Settings.DefaultLanguage;

        // Performance
        cloudInterval.Selected = CloudIntervalToIndex(settings.CloudUpdateInterval);
        cloudResolution.Selected = CloudResolutionToIndex(settings.CloudResolution);
        runAutoEvoDuringGameplay.Pressed = settings.RunAutoEvoDuringGamePlay;

        // Misc
        playIntro.Pressed = settings.PlayIntroVideo;
        playMicrobeIntro.Pressed = settings.PlayMicrobeIntroVideo;
        tutorialsEnabledOnNewGame.Pressed = settings.TutorialsEnabled;
        cheats.Pressed = settings.CheatsEnabled;
        autosave.Pressed = settings.AutoSaveEnabled;
        maxAutosaves.Value = settings.MaxAutoSaves;
        maxAutosaves.Editable = settings.AutoSaveEnabled;
        maxQuicksaves.Value = settings.MaxQuickSaves;
        customUsernameEnabled.Pressed = settings.CustomUsernameEnabled;
        customUsername.Text = settings.CustomUsername.Value != null ?
            settings.CustomUsername :
            Environment.UserName;
        customUsername.Editable = settings.CustomUsernameEnabled;
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
                if (!gameProperties.FreeBuild)
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
            if (gameProperties.TutorialState.Enabled != savedTutorialsEnabled)
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

    private void LoadLanguages(OptionButton optionButton)
    {
        languages = TranslationServer.GetLoadedLocales().Cast<string>().OrderBy(i => i, StringComparer.InvariantCulture)
            .ToList();

        foreach (var locale in languages)
        {
            optionButton.AddItem(locale);
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
        if (!CompareSettings())
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
        Settings.Instance.ApplyAll();
        ApplySettingsToControls(Settings.Instance);

        if (optionsMode == OptionsMode.InGame)
        {
            gameProperties.TutorialState.Enabled = savedTutorialsEnabled;
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
            errorAcceptBox.PopupCenteredMinsize();
            return;
        }

        // Copy over the new saved settings.
        savedSettings = Settings.Instance.Clone();

        if (optionsMode == OptionsMode.InGame)
            savedTutorialsEnabled = gameProperties.TutorialState.Enabled;

        UpdateResetSaveButtonState();
    }

    private void OnDefaultsPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        defaultsConfirmationBox.PopupCenteredMinsize();
    }

    private void BackSaveSelected()
    {
        // Save the new settings to the config file.
        if (!Settings.Instance.Save())
        {
            GD.PrintErr("Failed to save new options menu settings to configuration file.");
            backConfirmationBox.Hide();
            errorAcceptBox.PopupCenteredMinsize();

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
            gameProperties.TutorialState.Enabled = savedTutorialsEnabled;
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

        UpdateResetSaveButtonState();
    }

    private void OnAutoSaveToggled(bool pressed)
    {
        Settings.Instance.AutoSaveEnabled.Value = pressed;
        maxAutosaves.Editable = pressed;

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
        gameProperties.TutorialState.Enabled = pressed;

        UpdateResetSaveButtonState();
    }

    private void OnCustomUsernameEnabledToggled(bool pressed)
    {
        Settings.Instance.CustomUsernameEnabled.Value = pressed;
        customUsername.Editable = pressed;

        UpdateResetSaveButtonState();
    }

    private void OnCustomUsernameTextChanged(string text)
    {
        if (text.Equals(Environment.UserName, StringComparison.CurrentCulture))
        {
            Settings.Instance.CustomUsername.Value = null;
        }
        else
        {
            Settings.Instance.CustomUsername.Value = text;
        }

        UpdateResetSaveButtonState();
    }

    private void OnLanguageSettingSelected(int item)
    {
        Settings.Instance.SelectedLanguage.Value = languageSelection.GetItemText(item);
        resetLanguageButton.Visible = true;

        Settings.Instance.ApplyLanguageSettings();
        UpdateResetSaveButtonState();
    }

    private void OnResetLanguagePressed()
    {
        Settings.Instance.SelectedLanguage.Value = null;
        resetLanguageButton.Visible = false;

        Settings.Instance.ApplyLanguageSettings();
        UpdateSelectedLanguage(Settings.Instance);
        UpdateResetSaveButtonState();
    }

    private void UpdateSelectedLanguage(Settings settings)
    {
        if (string.IsNullOrEmpty(settings.SelectedLanguage.Value))
        {
            int index = languages.IndexOf(Settings.DefaultLanguage);

            // Inexact match to match things like "fi_FI"
            if (index == -1 && Settings.DefaultLanguage.Contains("_"))
            {
                index = languages.IndexOf(Settings.DefaultLanguage.Split("_")[0]);
            }

            // English is the default language, if the user's default locale didn't match anything
            if (index < 0)
            {
                index = languages.IndexOf("en");
            }

            languageSelection.Selected = index;
        }
        else
        {
            languageSelection.Selected = languages.IndexOf(settings.SelectedLanguage.Value);
        }
    }
}
