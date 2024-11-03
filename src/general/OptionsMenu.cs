﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Saving;

/// <summary>
///   Handles the logic for the options menu GUI.
/// </summary>
/// <remarks>
///   <para>
///     As this is a very large file, please pay close attention to ordering and grouping of variables and methods to
///     match the tab and order they exist in the scene.
///   </para>
/// </remarks>
public partial class OptionsMenu : ControlWithInput
{
    // GUI Control Paths

    // Options control buttons.

    [Export]
    public NodePath? BackButtonPath;

    [Export]
    public NodePath ResetButtonPath = null!;

    [Export]
    public NodePath SaveButtonPath = null!;

    // Tab selector buttons.
    [Export]
    public NodePath TabButtonsPath = null!;

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
    public NodePath ControllerPromptTypePath = null!;

    [Export]
    public NodePath DisplayAbilitiesBarTogglePath = null!;

    [Export]
    public NodePath DisplayBackgroundParticlesTogglePath = null!;

    [Export]
    public NodePath GUILightEffectsTogglePath = null!;

    [Export]
    public NodePath DisplayPartNamesTogglePath = null!;

    [Export]
    public NodePath DisplayMenu3DBackgroundsTogglePath = null!;

    [Export]
    public NodePath GpuNamePath = null!;

    [Export]
    public NodePath UsedRendererNamePath = null!;

    [Export]
    public NodePath VideoMemoryPath = null!;

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
    public NodePath RunGameSimulationMultithreadedPath = null!;

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

    [Export]
    public NodePath UseManualNativeThreadCountPath = null!;

    [Export]
    public NodePath NativeThreadCountSliderPath = null!;

    [Export]
    public NodePath MaxSpawnedEntitiesPath = null!;

    // Inputs tab.
    [Export]
    public NodePath InputsTabPath = null!;

    [Export]
    public NodePath MouseAxisSensitivitiesBoundPath = null!;

    [Export]
    public NodePath MouseHorizontalSensitivityPath = null!;

    [Export]
    public NodePath MouseHorizontalInvertedPath = null!;

    [Export]
    public NodePath MouseVerticalSensitivityPath = null!;

    [Export]
    public NodePath MouseVerticalInvertedPath = null!;

    [Export]
    public NodePath MouseWindowSizeScalingPath = null!;

    [Export]
    public NodePath MouseWindowSizeScalingWithLogicalSizePath = null!;

    [Export]
    public NodePath ControllerAxisSensitivitiesBoundPath = null!;

    [Export]
    public NodePath ControllerHorizontalSensitivityPath = null!;

    [Export]
    public NodePath ControllerHorizontalInvertedPath = null!;

    [Export]
    public NodePath ControllerVerticalSensitivityPath = null!;

    [Export]
    public NodePath ControllerVerticalInvertedPath = null!;

    [Export]
    public NodePath TwoDimensionalMovementPath = null!;

    [Export]
    public NodePath ThreeDimensionalMovementPath = null!;

    [Export]
    public NodePath MouseEdgePanEnabledPath = null!;

    [Export]
    public NodePath MouseEdgePanSensitivityPath = null!;

    [Export]
    public NodePath InputGroupListPath = null!;

    [Export]
    public NodePath DeadzoneConfigurationPopupPath = null!;

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
    public NodePath WebFeedsEnabledPath = null!;

    [Export]
    public NodePath ShowNewPatchNotesPath = null!;

    [Export]
    public NodePath DismissedNoticeCountPath = null!;

    [Export]
    public NodePath JSONDebugModePath = null!;

    [Export]
    public NodePath ScreenEffectSelectPath = null!;

    [Export]
    public NodePath CommitLabelPath = null!;

    [Export]
    public NodePath BuiltAtLabelPath = null!;

    [Export]
    public NodePath UnsavedProgressWarningPath = null!;

    [Export]
    public NodePath PatchNotesBoxPath = null!;

    [Export]
    public NodePath PatchNotesDisplayerPath = null!;

    private static readonly Lazy<List<string>> LanguagesCache = new(() =>
        TranslationServer.GetLoadedLocales().OrderBy(i => i, StringComparer.InvariantCulture).ToList());

    // TODO: this should be refreshed periodically to support user plugging in new devices
    private static readonly List<string> AudioOutputDevicesCache = AudioServer
        .GetOutputDeviceList().Where(d => d != Constants.DEFAULT_AUDIO_OUTPUT_DEVICE_NAME)
        .Prepend(Constants.DEFAULT_AUDIO_OUTPUT_DEVICE_NAME).ToList();

#pragma warning disable CA2213
    private Button backButton = null!;
    private Button resetButton = null!;
    private Button saveButton = null!;

    // Tab selector buttons
    private TabButtons tabButtons = null!;
    private Button graphicsButton = null!;
    private Button soundButton = null!;
    private Button performanceButton = null!;
    private Button inputsButton = null!;
    private Button miscButton = null!;

    // Graphics tab
    private Control graphicsTab = null!;
    private CheckBox vsync = null!;
    private CheckBox fullScreen = null!;
    private Label? resolution;
    private OptionButton msaaResolution = null!;
    private OptionButton maxFramesPerSecond = null!;
    private OptionButton colourblindSetting = null!;
    private CheckBox chromaticAberrationToggle = null!;
    private Slider chromaticAberrationSlider = null!;
    private OptionButton controllerPromptType = null!;
    private CheckBox displayAbilitiesHotBarToggle = null!;

    [Export]
    private CheckBox damageEffect = null!;

    [Export]
    private OptionButton strainVisibility = null!;

    private CheckBox displayBackgroundParticlesToggle = null!;
    private CheckBox guiLightEffectsToggle = null!;
    private CheckBox displayPartNamesToggle = null!;
    private CheckBox displayMenu3DBackgroundsToggle = null!;

    [Export]
    private Button displayMicrobeBackgroundDistortionToggle = null!;

    [Export]
    private Button bloomEffectToggle = null!;

    private Label gpuName = null!;
    private Label usedRendererName = null!;
    private Label videoMemory = null!;

    // Sound tab
    private Control soundTab = null!;
    private Slider masterVolume = null!;
    private CheckBox masterMuted = null!;
    private Slider musicVolume = null!;
    private CheckBox musicMuted = null!;
    private Slider ambianceVolume = null!;
    private CheckBox ambianceMuted = null!;
    private Slider sfxVolume = null!;
    private CheckBox sfxMuted = null!;
    private Slider guiVolume = null!;
    private CheckBox guiMuted = null!;
    private OptionButton audioOutputDeviceSelection = null!;
    private OptionButton languageSelection = null!;
    private Button resetLanguageButton = null!;
    private Label languageProgressLabel = null!;

    // Performance tab
    private Control performanceTab = null!;
    private OptionButton cloudInterval = null!;
    private VBoxContainer cloudResolutionTitle = null!;
    private OptionButton cloudResolution = null!;
    private CheckBox runAutoEvoDuringGameplay = null!;
    private CheckBox runGameSimulationMultithreaded = null!;
    private Label detectedCPUCount = null!;
    private Label activeThreadCount = null!;
    private CheckBox assumeHyperthreading = null!;
    private CheckBox useManualThreadCount = null!;
    private Slider threadCountSlider = null!;
    private CheckBox useManualNativeThreadCount = null!;
    private Slider nativeThreadCountSlider = null!;
    private OptionButton maxSpawnedEntities = null!;

    // Inputs tab
    private Control inputsTab = null!;

    private Button mouseAxisSensitivitiesBound = null!;
    private Slider mouseHorizontalSensitivity = null!;
    private Button mouseHorizontalInverted = null!;
    private Slider mouseVerticalSensitivity = null!;
    private Button mouseVerticalInverted = null!;
    private OptionButton mouseWindowSizeScaling = null!;
    private Button mouseWindowSizeScalingWithLogicalSize = null!;

    private Button controllerAxisSensitivitiesBound = null!;
    private Slider controllerHorizontalSensitivity = null!;
    private Button controllerHorizontalInverted = null!;
    private Slider controllerVerticalSensitivity = null!;
    private Button controllerVerticalInverted = null!;

    private OptionButton twoDimensionalMovement = null!;
    private OptionButton threeDimensionalMovement = null!;

    private Button mouseEdgePanEnabled = null!;
    private Slider mouseEdgePanSensitivity = null!;

    private InputGroupList inputGroupList = null!;

    private ControllerDeadzoneConfiguration deadzoneConfigurationPopup = null!;

    // Misc tab
    private Control miscTab = null!;
    private CheckBox playIntro = null!;
    private CheckBox playMicrobeIntro = null!;
    private CheckBox cheats = null!;
    private CheckBox tutorialsEnabledOnNewGame = null!;
    private CheckBox autoSave = null!;
    private SpinBox maxAutoSaves = null!;
    private SpinBox maxQuickSaves = null!;
    private CheckBox customUsernameEnabled = null!;
    private LineEdit customUsername = null!;
    private CheckBox webFeedsEnabled = null!;
    private CheckBox showNewPatchNotes = null!;
    private Label dismissedNoticeCount = null!;
    private OptionButton jsonDebugMode = null!;
    private OptionButton screenEffectSelect = null!;
    private Label commitLabel = null!;
    private Label builtAtLabel = null!;

    private CheckBox tutorialsEnabled = null!;
    private CheckBox unsavedProgressWarningEnabled = null!;

    // Confirmation Boxes
    private CustomConfirmationDialog screenshotDirectoryWarningBox = null!;
    private CustomWindow backConfirmationBox = null!;
    private CustomConfirmationDialog defaultsConfirmationBox = null!;
    private ErrorDialog errorAcceptBox = null!;

    private CustomWindow patchNotesBox = null!;
    private PatchNotesDisplayer patchNotesDisplayer = null!;
#pragma warning restore CA2213

    // Misc

    private OptionsMode optionsMode;
    private OptionsTab selectedOptionsTab;

    /// <summary>
    ///   Copy of the settings object that should match what is saved to the configuration file,
    ///   used for comparing and restoring to previous state.
    /// </summary>
    private Settings savedSettings = null!;

    private bool savedTutorialsEnabled;

    private GameProperties? gameProperties;

    private bool nodeReferencesResolved;

    private bool elementItemSelectionsInitialized;

    // Signals

    [Signal]
    public delegate void OnOptionsClosedEventHandler();

    public enum OptionsMode
    {
        MainMenu,
        InGame,
    }

    public enum OptionsTab
    {
        Graphics,
        Sound,
        Performance,
        Inputs,
        Miscellaneous,
    }

    private static List<string> Languages => LanguagesCache.Value;
    private static List<string> AudioOutputDevices => AudioOutputDevicesCache;

    public override void _Ready()
    {
        ResolveNodeReferences(true);

        if (IsVisibleInTree())
        {
            GD.Print("Immediately loading options menu items as it is visible in _Ready");
            InitializeOptionsSelections();
        }

        inputGroupList.OnControlsChanged += OnControlsChanged;

        deadzoneConfigurationPopup.OnDeadzonesConfirmed += OnDeadzoneConfigurationChanged;

        GetViewport().Connect(Viewport.SignalName.SizeChanged, new Callable(this, nameof(DisplayResolution)));

        selectedOptionsTab = OptionsTab.Graphics;
    }

    public void ResolveNodeReferences(bool calledFromReady)
    {
        if (nodeReferencesResolved)
            return;

        // Options control buttons
        backButton = GetNode<Button>(BackButtonPath);
        resetButton = GetNode<Button>(ResetButtonPath);
        saveButton = GetNode<Button>(SaveButtonPath);

        // Tab selector buttons
        tabButtons = GetNode<TabButtons>(TabButtonsPath);

        // When _Ready is called the tab buttons will have been adjusted, so how we find the buttons needs different
        // approaches based on how early this is called
        if (calledFromReady)
        {
            graphicsButton = GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, GraphicsButtonPath));
            soundButton = GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, SoundButtonPath));
            performanceButton =
                GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, PerformanceButtonPath));
            inputsButton = GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, InputsButtonPath));
            miscButton = GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, MiscButtonPath));
        }
        else
        {
            graphicsButton = GetNode<Button>(GraphicsButtonPath);
            soundButton = GetNode<Button>(SoundButtonPath);
            performanceButton = GetNode<Button>(PerformanceButtonPath);
            inputsButton = GetNode<Button>(InputsButtonPath);
            miscButton = GetNode<Button>(MiscButtonPath);
        }

        // Graphics
        graphicsTab = GetNode<Control>(GraphicsTabPath);
        vsync = GetNode<CheckBox>(VSyncPath);
        fullScreen = GetNode<CheckBox>(FullScreenPath);
        msaaResolution = GetNode<OptionButton>(MSAAResolutionPath);
        resolution = GetNode<Label>(ResolutionPath);
        maxFramesPerSecond = GetNode<OptionButton>(MaxFramesPerSecondPath);
        colourblindSetting = GetNode<OptionButton>(ColourblindSettingPath);
        chromaticAberrationToggle = GetNode<CheckBox>(ChromaticAberrationTogglePath);
        chromaticAberrationSlider = GetNode<Slider>(ChromaticAberrationSliderPath);
        controllerPromptType = GetNode<OptionButton>(ControllerPromptTypePath);
        displayAbilitiesHotBarToggle = GetNode<CheckBox>(DisplayAbilitiesBarTogglePath);
        displayBackgroundParticlesToggle = GetNode<CheckBox>(DisplayBackgroundParticlesTogglePath);
        guiLightEffectsToggle = GetNode<CheckBox>(GUILightEffectsTogglePath);
        displayPartNamesToggle = GetNode<CheckBox>(DisplayPartNamesTogglePath);
        displayMenu3DBackgroundsToggle = GetNode<CheckBox>(DisplayMenu3DBackgroundsTogglePath);
        gpuName = GetNode<Label>(GpuNamePath);
        usedRendererName = GetNode<Label>(UsedRendererNamePath);
        videoMemory = GetNode<Label>(VideoMemoryPath);

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
        audioOutputDeviceSelection = GetNode<OptionButton>(AudioOutputDeviceSelectionPath);
        languageSelection = GetNode<OptionButton>(LanguageSelectionPath);
        resetLanguageButton = GetNode<Button>(ResetLanguageButtonPath);
        languageProgressLabel = GetNode<Label>(LanguageProgressLabelPath);

        // Performance
        performanceTab = GetNode<Control>(PerformanceTabPath);
        cloudInterval = GetNode<OptionButton>(CloudIntervalPath);
        cloudResolutionTitle = GetNode<VBoxContainer>(CloudResolutionTitlePath);
        cloudResolution = GetNode<OptionButton>(CloudResolutionPath);
        runAutoEvoDuringGameplay = GetNode<CheckBox>(RunAutoEvoDuringGameplayPath);
        runGameSimulationMultithreaded = GetNode<CheckBox>(RunGameSimulationMultithreadedPath);
        detectedCPUCount = GetNode<Label>(DetectedCPUCountPath);
        activeThreadCount = GetNode<Label>(ActiveThreadCountPath);
        assumeHyperthreading = GetNode<CheckBox>(AssumeHyperthreadingPath);
        useManualThreadCount = GetNode<CheckBox>(UseManualThreadCountPath);
        threadCountSlider = GetNode<Slider>(ThreadCountSliderPath);
        useManualNativeThreadCount = GetNode<CheckBox>(UseManualNativeThreadCountPath);
        nativeThreadCountSlider = GetNode<Slider>(NativeThreadCountSliderPath);
        maxSpawnedEntities = GetNode<OptionButton>(MaxSpawnedEntitiesPath);

        // Inputs
        inputsTab = GetNode<Control>(InputsTabPath);
        mouseAxisSensitivitiesBound = GetNode<Button>(MouseAxisSensitivitiesBoundPath);
        mouseHorizontalSensitivity = GetNode<Slider>(MouseHorizontalSensitivityPath);
        mouseHorizontalInverted = GetNode<Button>(MouseHorizontalInvertedPath);
        mouseVerticalSensitivity = GetNode<Slider>(MouseVerticalSensitivityPath);
        mouseVerticalInverted = GetNode<Button>(MouseVerticalInvertedPath);
        mouseWindowSizeScaling = GetNode<OptionButton>(MouseWindowSizeScalingPath);
        mouseWindowSizeScalingWithLogicalSize = GetNode<Button>(MouseWindowSizeScalingWithLogicalSizePath);

        controllerAxisSensitivitiesBound = GetNode<Button>(ControllerAxisSensitivitiesBoundPath);
        controllerHorizontalSensitivity = GetNode<Slider>(ControllerHorizontalSensitivityPath);
        controllerHorizontalInverted = GetNode<Button>(ControllerHorizontalInvertedPath);
        controllerVerticalSensitivity = GetNode<Slider>(ControllerVerticalSensitivityPath);
        controllerVerticalInverted = GetNode<Button>(ControllerVerticalInvertedPath);

        twoDimensionalMovement = GetNode<OptionButton>(TwoDimensionalMovementPath);
        threeDimensionalMovement = GetNode<OptionButton>(ThreeDimensionalMovementPath);

        mouseEdgePanEnabled = GetNode<Button>(MouseEdgePanEnabledPath);
        mouseEdgePanSensitivity = GetNode<Slider>(MouseEdgePanSensitivityPath);

        inputGroupList = GetNode<InputGroupList>(InputGroupListPath);

        deadzoneConfigurationPopup = GetNode<ControllerDeadzoneConfiguration>(DeadzoneConfigurationPopupPath);

        // Misc
        miscTab = GetNode<Control>(MiscTabPath);
        playIntro = GetNode<CheckBox>(PlayIntroPath);
        playMicrobeIntro = GetNode<CheckBox>(PlayMicrobeIntroPath);
        tutorialsEnabledOnNewGame = GetNode<CheckBox>(TutorialsEnabledOnNewGamePath);
        cheats = GetNode<CheckBox>(CheatsPath);
        autoSave = GetNode<CheckBox>(AutoSavePath);
        maxAutoSaves = GetNode<SpinBox>(MaxAutoSavesPath);
        maxQuickSaves = GetNode<SpinBox>(MaxQuickSavesPath);
        tutorialsEnabled = GetNode<CheckBox>(TutorialsEnabledPath);
        customUsernameEnabled = GetNode<CheckBox>(CustomUsernameEnabledPath);
        customUsername = GetNode<LineEdit>(CustomUsernamePath);
        webFeedsEnabled = GetNode<CheckBox>(WebFeedsEnabledPath);
        showNewPatchNotes = GetNode<CheckBox>(ShowNewPatchNotesPath);
        dismissedNoticeCount = GetNode<Label>(DismissedNoticeCountPath);
        jsonDebugMode = GetNode<OptionButton>(JSONDebugModePath);
        screenEffectSelect = GetNode<OptionButton>(ScreenEffectSelectPath);
        commitLabel = GetNode<Label>(CommitLabelPath);
        builtAtLabel = GetNode<Label>(BuiltAtLabelPath);
        builtAtLabel.RegisterCustomFocusDrawer();
        unsavedProgressWarningEnabled = GetNode<CheckBox>(UnsavedProgressWarningPath);

        screenshotDirectoryWarningBox = GetNode<CustomConfirmationDialog>(ScreenshotDirectoryWarningBoxPath);
        backConfirmationBox = GetNode<CustomWindow>(BackConfirmationBoxPath);
        defaultsConfirmationBox = GetNode<CustomConfirmationDialog>(DefaultsConfirmationBoxPath);
        errorAcceptBox = GetNode<ErrorDialog>(ErrorAcceptBoxPath);
        patchNotesBox = GetNode<CustomWindow>(PatchNotesBoxPath);
        patchNotesDisplayer = GetNode<PatchNotesDisplayer>(PatchNotesDisplayerPath);

        nodeReferencesResolved = true;
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        ResolveNodeReferences(false);

        cloudResolutionTitle.RegisterToolTipForControl("cloudResolution", "options", false);
        guiLightEffectsToggle.RegisterToolTipForControl("guiLightEffects", "options", false);
        assumeHyperthreading.RegisterToolTipForControl("assumeHyperthreading", "options", false);
        unsavedProgressWarningEnabled.RegisterToolTipForControl("unsavedProgressWarning", "options", false);

        Localization.Instance.OnTranslationsChanged += OnTranslationsChanged;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        Localization.Instance.OnTranslationsChanged -= OnTranslationsChanged;

        cloudResolutionTitle.UnRegisterToolTipForControl("cloudResolution", "options");
        guiLightEffectsToggle.UnRegisterToolTipForControl("guiLightEffects", "options");
        assumeHyperthreading.UnRegisterToolTipForControl("assumeHyperthreading", "options");
        unsavedProgressWarningEnabled.UnRegisterToolTipForControl("unsavedProgressWarning", "options");
    }

    /// <summary>
    ///   Opens the options menu with main menu configuration settings.
    /// </summary>
    public void OpenFromMainMenu()
    {
        // Shouldn't do anything if options is already open.
        if (Visible)
            return;

        InitializeOptionsSelections();

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

        InitializeOptionsSelections();

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
            tutorialsEnabled.ButtonPressed = savedTutorialsEnabled;

        ApplySettingsToControls(savedSettings);
        UpdateResetSaveButtonState();

        Show();
    }

    /// <summary>
    ///   Applies the values of a settings object to all corresponding options menu GUI controls.
    /// </summary>
    public void ApplySettingsToControls(Settings settings)
    {
        // TODO: all of these changes cause Godot change callbacks which in turn cause settings comparisons
        // that is not efficient at all so instead we should set a flag here and ignore settings compare calls
        // while it is active

        var simulationParameters = SimulationParameters.Instance;

        // Graphics
        vsync.ButtonPressed = settings.VSync;
        fullScreen.ButtonPressed = settings.FullScreen;
        msaaResolution.Selected = MSAAResolutionToIndex(settings.MSAAResolution);
        maxFramesPerSecond.Selected = MaxFPSValueToIndex(settings.MaxFramesPerSecond);
        colourblindSetting.Selected = settings.ColourblindSetting;
        chromaticAberrationSlider.Value = settings.ChromaticAmount;
        chromaticAberrationToggle.ButtonPressed = settings.ChromaticEnabled;
        controllerPromptType.Selected = ControllerPromptTypeToIndex(settings.ControllerPromptType);
        displayAbilitiesHotBarToggle.ButtonPressed = settings.DisplayAbilitiesHotBar;
        damageEffect.ButtonPressed = settings.ScreenDamageEffect;
        strainVisibility.Selected = (int)settings.StrainBarVisibilityMode.Value;
        displayBackgroundParticlesToggle.ButtonPressed = settings.DisplayBackgroundParticles;
        displayMicrobeBackgroundDistortionToggle.ButtonPressed = settings.MicrobeDistortionStrength.Value > 0;
        guiLightEffectsToggle.ButtonPressed = settings.GUILightEffectsEnabled;
        displayPartNamesToggle.ButtonPressed = settings.DisplayPartNames;
        displayMenu3DBackgroundsToggle.ButtonPressed = settings.Menu3DBackgroundEnabled;
        bloomEffectToggle.ButtonPressed = settings.BloomEnabled;
        DisplayResolution();
        DisplayGpuInfo();

        // Sound
        masterVolume.Value = ConvertDbToSoundBar(settings.VolumeMaster);
        masterMuted.ButtonPressed = settings.VolumeMasterMuted;
        musicVolume.Value = ConvertDbToSoundBar(settings.VolumeMusic);
        musicMuted.ButtonPressed = settings.VolumeMusicMuted;
        ambianceVolume.Value = ConvertDbToSoundBar(settings.VolumeAmbiance);
        ambianceMuted.ButtonPressed = settings.VolumeAmbianceMuted;
        sfxVolume.Value = ConvertDbToSoundBar(settings.VolumeSFX);
        sfxMuted.ButtonPressed = settings.VolumeSFXMuted;
        guiVolume.Value = ConvertDbToSoundBar(settings.VolumeGUI);
        guiMuted.ButtonPressed = settings.VolumeGUIMuted;
        UpdateSelectedLanguage(settings);
        UpdateSelectedAudioOutputDevice(settings);

        // Hide or show the reset language button based on the selected language
        resetLanguageButton.Visible = settings.SelectedLanguage.Value != null &&
            settings.SelectedLanguage.Value != Settings.DefaultLanguage;
        UpdateCurrentLanguageProgress();

        // Performance
        cloudInterval.Selected = CloudIntervalToIndex(settings.CloudUpdateInterval);
        cloudResolution.Selected = CloudResolutionToIndex(settings.CloudResolution);
        runAutoEvoDuringGameplay.ButtonPressed = settings.RunAutoEvoDuringGamePlay;
        runGameSimulationMultithreaded.ButtonPressed = settings.RunGameSimulationMultithreaded;
        assumeHyperthreading.ButtonPressed = settings.AssumeCPUHasHyperthreading;
        useManualThreadCount.ButtonPressed = settings.UseManualThreadCount;
        threadCountSlider.Value = settings.ThreadCount;
        threadCountSlider.Editable = settings.UseManualThreadCount;
        useManualNativeThreadCount.ButtonPressed = settings.UseManualNativeThreadCount;
        nativeThreadCountSlider.Value = settings.NativeThreadCount;
        nativeThreadCountSlider.Editable = settings.UseManualNativeThreadCount;
        maxSpawnedEntities.Selected = MaxEntitiesValueToIndex(settings.MaxSpawnedEntities);

        UpdateDetectedCPUCount();

        // Input
        mouseAxisSensitivitiesBound.ButtonPressed =
            settings.HorizontalMouseLookSensitivity.Equals(settings.VerticalMouseLookSensitivity);
        mouseHorizontalSensitivity.Value = MouseInputSensitivityToBarValue(settings.HorizontalMouseLookSensitivity);
        mouseHorizontalInverted.ButtonPressed = settings.InvertHorizontalMouseLook;
        mouseVerticalSensitivity.Editable = !mouseAxisSensitivitiesBound.ButtonPressed;
        mouseVerticalSensitivity.FocusMode =
            mouseAxisSensitivitiesBound.ButtonPressed ? FocusModeEnum.Click : FocusModeEnum.All;
        mouseVerticalSensitivity.Value = MouseInputSensitivityToBarValue(settings.VerticalMouseLookSensitivity);
        mouseVerticalInverted.ButtonPressed = settings.InvertVerticalMouseLook;
        mouseWindowSizeScaling.Selected = MouseInputScalingToIndex(settings.ScaleMouseInputByWindowSize);
        mouseWindowSizeScalingWithLogicalSize.ButtonPressed = settings.InputWindowSizeIsLogicalSize;

        controllerAxisSensitivitiesBound.ButtonPressed =
            settings.HorizontalControllerLookSensitivity.Equals(settings.VerticalControllerLookSensitivity);
        controllerHorizontalSensitivity.Value =
            ControllerInputSensitivityToBarValue(settings.HorizontalControllerLookSensitivity);
        controllerHorizontalInverted.ButtonPressed = settings.InvertHorizontalControllerLook;
        controllerVerticalSensitivity.Editable = !controllerAxisSensitivitiesBound.ButtonPressed;
        controllerVerticalSensitivity.Value =
            ControllerInputSensitivityToBarValue(settings.VerticalControllerLookSensitivity);
        controllerVerticalInverted.ButtonPressed = settings.InvertVerticalControllerLook;

        twoDimensionalMovement.Selected = Movement2DToIndex(settings.TwoDimensionalMovement);
        threeDimensionalMovement.Selected = Movement3DToIndex(settings.ThreeDimensionalMovement);

        mouseEdgePanEnabled.ButtonPressed = settings.PanStrategyViewWithMouse;
        mouseEdgePanSensitivity.Value = settings.PanStrategyViewMouseSpeed;
        mouseEdgePanSensitivity.Editable = mouseEdgePanEnabled.ButtonPressed;
        mouseEdgePanSensitivity.FocusMode = mouseEdgePanEnabled.ButtonPressed ? FocusModeEnum.All : FocusModeEnum.Click;

        BuildInputRebindControls();

        // Misc
        playIntro.ButtonPressed = settings.PlayIntroVideo;
        playMicrobeIntro.ButtonPressed = settings.PlayMicrobeIntroVideo;
        tutorialsEnabledOnNewGame.ButtonPressed = settings.TutorialsEnabled;
        cheats.ButtonPressed = settings.CheatsEnabled;
        autoSave.ButtonPressed = settings.AutoSaveEnabled;
        maxAutoSaves.Value = settings.MaxAutoSaves;
        maxAutoSaves.Editable = settings.AutoSaveEnabled;
        maxQuickSaves.Value = settings.MaxQuickSaves;
        customUsernameEnabled.ButtonPressed = settings.CustomUsernameEnabled;
        customUsername.Text = settings.CustomUsername.Value != null ?
            settings.CustomUsername :
            Settings.EnvironmentUserName;
        customUsername.Editable = settings.CustomUsernameEnabled;
        webFeedsEnabled.ButtonPressed = settings.ThriveNewsFeedEnabled;
        showNewPatchNotes.ButtonPressed = settings.ShowNewPatchNotes;
        jsonDebugMode.Selected = JSONDebugModeToIndex(settings.JSONDebugMode);
        screenEffectSelect.Selected = settings.CurrentScreenEffect.Value != null ?
            settings.CurrentScreenEffect.Value.Index :
            simulationParameters.GetScreenEffectByIndex(0).Index;
        unsavedProgressWarningEnabled.ButtonPressed = settings.ShowUnsavedProgressWarning;

        UpdateDismissedNoticeCount();
        UpdateShownCommit();
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

    public void SelectOptionsTab(OptionsTab tab)
    {
        ChangeSettingsTab(tab.ToString());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (BackButtonPath != null)
            {
                BackButtonPath.Dispose();
                ResetButtonPath.Dispose();
                SaveButtonPath.Dispose();
                TabButtonsPath.Dispose();
                GraphicsButtonPath.Dispose();
                SoundButtonPath.Dispose();
                PerformanceButtonPath.Dispose();
                InputsButtonPath.Dispose();
                MiscButtonPath.Dispose();
                GraphicsTabPath.Dispose();
                VSyncPath.Dispose();
                FullScreenPath.Dispose();
                MSAAResolutionPath.Dispose();
                ResolutionPath.Dispose();
                MaxFramesPerSecondPath.Dispose();
                ColourblindSettingPath.Dispose();
                ChromaticAberrationSliderPath.Dispose();
                ChromaticAberrationTogglePath.Dispose();
                ControllerPromptTypePath.Dispose();
                DisplayAbilitiesBarTogglePath.Dispose();
                DisplayBackgroundParticlesTogglePath.Dispose();
                GUILightEffectsTogglePath.Dispose();
                DisplayPartNamesTogglePath.Dispose();
                DisplayMenu3DBackgroundsTogglePath.Dispose();
                GpuNamePath.Dispose();
                UsedRendererNamePath.Dispose();
                VideoMemoryPath.Dispose();
                SoundTabPath.Dispose();
                MasterVolumePath.Dispose();
                MasterMutedPath.Dispose();
                MusicVolumePath.Dispose();
                MusicMutedPath.Dispose();
                AmbianceVolumePath.Dispose();
                AmbianceMutedPath.Dispose();
                SFXVolumePath.Dispose();
                SFXMutedPath.Dispose();
                GUIVolumePath.Dispose();
                GUIMutedPath.Dispose();
                AudioOutputDeviceSelectionPath.Dispose();
                LanguageSelectionPath.Dispose();
                ResetLanguageButtonPath.Dispose();
                LanguageProgressLabelPath.Dispose();
                PerformanceTabPath.Dispose();
                CloudIntervalPath.Dispose();
                CloudResolutionTitlePath.Dispose();
                CloudResolutionPath.Dispose();
                RunAutoEvoDuringGameplayPath.Dispose();
                RunGameSimulationMultithreadedPath.Dispose();
                DetectedCPUCountPath.Dispose();
                ActiveThreadCountPath.Dispose();
                AssumeHyperthreadingPath.Dispose();
                UseManualThreadCountPath.Dispose();
                ThreadCountSliderPath.Dispose();
                UseManualNativeThreadCountPath.Dispose();
                NativeThreadCountSliderPath.Dispose();
                MaxSpawnedEntitiesPath.Dispose();
                InputsTabPath.Dispose();
                MouseAxisSensitivitiesBoundPath.Dispose();
                MouseHorizontalSensitivityPath.Dispose();
                MouseHorizontalInvertedPath.Dispose();
                MouseVerticalSensitivityPath.Dispose();
                MouseVerticalInvertedPath.Dispose();
                MouseWindowSizeScalingPath.Dispose();
                MouseWindowSizeScalingWithLogicalSizePath.Dispose();
                ControllerAxisSensitivitiesBoundPath.Dispose();
                ControllerHorizontalSensitivityPath.Dispose();
                ControllerHorizontalInvertedPath.Dispose();
                ControllerVerticalSensitivityPath.Dispose();
                ControllerVerticalInvertedPath.Dispose();
                TwoDimensionalMovementPath.Dispose();
                ThreeDimensionalMovementPath.Dispose();
                MouseEdgePanEnabledPath.Dispose();
                MouseEdgePanSensitivityPath.Dispose();
                InputGroupListPath.Dispose();
                DeadzoneConfigurationPopupPath.Dispose();
                MiscTabPath.Dispose();
                PlayIntroPath.Dispose();
                PlayMicrobeIntroPath.Dispose();
                TutorialsEnabledOnNewGamePath.Dispose();
                CheatsPath.Dispose();
                AutoSavePath.Dispose();
                MaxAutoSavesPath.Dispose();
                MaxQuickSavesPath.Dispose();
                BackConfirmationBoxPath.Dispose();
                TutorialsEnabledPath.Dispose();
                ScreenshotDirectoryWarningBoxPath.Dispose();
                DefaultsConfirmationBoxPath.Dispose();
                ErrorAcceptBoxPath.Dispose();
                PatchNotesBoxPath.Dispose();
                PatchNotesDisplayerPath.Dispose();
                CustomUsernameEnabledPath.Dispose();
                CustomUsernamePath.Dispose();
                WebFeedsEnabledPath.Dispose();
                ShowNewPatchNotesPath.Dispose();
                DismissedNoticeCountPath.Dispose();
                JSONDebugModePath.Dispose();
                ScreenEffectSelectPath.Dispose();
                CommitLabelPath.Dispose();
                BuiltAtLabelPath.Dispose();
                UnsavedProgressWarningPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void InitializeOptionsSelections()
    {
        if (elementItemSelectionsInitialized)
            return;

        elementItemSelectionsInitialized = true;

        LoadLanguages();
        LoadAudioOutputDevices();
        LoadScreenEffects();
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
                {
                    tutorialsEnabled.Show();
                }
                else
                {
                    tutorialsEnabled.Hide();
                }

                optionsMode = OptionsMode.InGame;
                break;
            }

            default:
                throw new ArgumentException("Options menu SwitchMode called with an invalid mode argument");
        }
    }

    /// <summary>
    ///   Displays the current viewport resolution
    /// </summary>
    private void DisplayResolution()
    {
        if (resolution == null)
            return;

        var screenResolution = DisplayServer.WindowGetSize().AsFloats() * DisplayServer.ScreenGetScale();
        resolution.Text = Localization.Translate("AUTO_RESOLUTION")
            .FormatSafe(screenResolution.X, screenResolution.Y);
    }

    /// <summary>
    ///   Displays the GPU name, the display driver name and used video memory
    /// </summary>
    private void DisplayGpuInfo()
    {
        gpuName.Text = RenderingServer.GetVideoAdapterName();

        switch (FeatureInformation.GetVideoDriver())
        {
            case OS.RenderingDriver.Vulkan:
                usedRendererName.Text = Localization.Translate("DISPLAY_DRIVER_VULKAN");
                break;
            case OS.RenderingDriver.Opengl3:
                usedRendererName.Text = Localization.Translate("DISPLAY_DRIVER_OPENGL");
                break;
            default:
                // An unknown display driver is being used
                usedRendererName.Text = Localization.Translate("UNKNOWN_DISPLAY_DRIVER");
                break;
        }

        float videoMemoryInMebibytes = RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.VideoMemUsed);

        // Convert to mebibytes
        videoMemoryInMebibytes /= Constants.MEBIBYTE;

        // Round to 2 places after the floating point
        videoMemory.Text = Localization.Translate("VIDEO_MEMORY_MIB")
            .FormatSafe(Math.Round(videoMemoryInMebibytes, 2));
    }

    private void OnTranslationsChanged()
    {
        BuildInputRebindControls();
        UpdateDefaultAudioOutputDeviceText();
        DisplayResolution();
        DisplayGpuInfo();
    }

    /// <summary>
    ///   Changes the active settings tab that is displayed, or returns if the tab is already active.
    /// </summary>
    private void ChangeSettingsTab(string newTabName)
    {
        // Convert from the string binding to an enum.
        OptionsTab selection = (OptionsTab)Enum.Parse(typeof(OptionsTab), newTabName);

        // Pressing the same button that's already active, so just return.
        if (selection == selectedOptionsTab)
            return;

        graphicsTab.Hide();
        soundTab.Hide();
        performanceTab.Hide();
        inputsTab.Hide();
        miscTab.Hide();

        var invalidNodePath = new NodePath();
        backButton.FocusNeighborTop = invalidNodePath;
        backButton.FocusPrevious = invalidNodePath;

        switch (selection)
        {
            case OptionsTab.Graphics:
                graphicsTab.Show();
                graphicsButton.ButtonPressed = true;
                break;
            case OptionsTab.Sound:
                soundTab.Show();
                soundButton.ButtonPressed = true;
                break;
            case OptionsTab.Performance:
                performanceTab.Show();
                performanceButton.ButtonPressed = true;
                break;
            case OptionsTab.Inputs:
                inputsTab.Show();
                inputsButton.ButtonPressed = true;

                // This needs different neighbours here to not mess with the inputs list as badly
                var neighbourPath = mouseAxisSensitivitiesBound.GetPath();
                backButton.FocusNeighborTop = neighbourPath;
                backButton.FocusPrevious = neighbourPath;

                break;
            case OptionsTab.Miscellaneous:
                miscTab.Show();
                miscButton.ButtonPressed = true;
                break;
            default:
                GD.PrintErr("Invalid tab");
                break;
        }

        GUICommon.Instance.PlayButtonPressSound();
        selectedOptionsTab = selection;
    }

    /// <summary>
    ///   Converts the slider value (0-100) to a dB adjustment for a sound channel
    /// </summary>
    private float ConvertSoundBarToDb(float value)
    {
        return Mathf.LinearToDb(value / 100.0f);
    }

    private float ConvertDbToSoundBar(float value)
    {
        return Mathf.DbToLinear(value) * 100.0f;
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

    private int MaxEntitiesIndexToValue(int index)
    {
        switch (index)
        {
            case 0:
                return Constants.TINY_MAX_SPAWNED_ENTITIES;
            case 1:
                return Constants.VERY_SMALL_MAX_SPAWNED_ENTITIES;
            case 2:
                return Constants.SMALL_MAX_SPAWNED_ENTITIES;
            case 3:
                return Constants.NORMAL_MAX_SPAWNED_ENTITIES;
            case 4:
                return Constants.LARGE_MAX_SPAWNED_ENTITIES;
            case 5:
                return Constants.VERY_LARGE_MAX_SPAWNED_ENTITIES;
            case 6:
                return Constants.HUGE_MAX_SPAWNED_ENTITIES;
            case 7:
                return Constants.EXTREME_MAX_SPAWNED_ENTITIES;
            default:
                GD.PrintErr("invalid max entities count index");
                return Constants.NORMAL_MAX_SPAWNED_ENTITIES;
        }
    }

    private int MaxEntitiesValueToIndex(int value)
    {
        switch (value)
        {
            case Constants.TINY_MAX_SPAWNED_ENTITIES:
                return 0;
            case Constants.VERY_SMALL_MAX_SPAWNED_ENTITIES:
                return 1;
            case Constants.SMALL_MAX_SPAWNED_ENTITIES:
                return 2;
            case Constants.NORMAL_MAX_SPAWNED_ENTITIES:
                return 3;
            case Constants.LARGE_MAX_SPAWNED_ENTITIES:
                return 4;
            case Constants.VERY_LARGE_MAX_SPAWNED_ENTITIES:
                return 5;
            case Constants.HUGE_MAX_SPAWNED_ENTITIES:
                return 6;
            case Constants.EXTREME_MAX_SPAWNED_ENTITIES:
                return 7;
            default:
                GD.PrintErr("invalid max entities count value (using closest value)");
                return MaxEntitiesValueToIndex(ListUtils.FindClosestValue(value, Constants.TINY_MAX_SPAWNED_ENTITIES,
                    Constants.VERY_SMALL_MAX_SPAWNED_ENTITIES, Constants.SMALL_MAX_SPAWNED_ENTITIES,
                    Constants.NORMAL_MAX_SPAWNED_ENTITIES, Constants.LARGE_MAX_SPAWNED_ENTITIES,
                    Constants.VERY_LARGE_MAX_SPAWNED_ENTITIES, Constants.HUGE_MAX_SPAWNED_ENTITIES,
                    Constants.EXTREME_MAX_SPAWNED_ENTITIES));
        }
    }

    private int MSAAResolutionToIndex(Viewport.Msaa resolution)
    {
        if (resolution == Viewport.Msaa.Disabled)
            return 0;

        if (resolution == Viewport.Msaa.Msaa2X)
            return 1;

        if (resolution == Viewport.Msaa.Msaa4X)
            return 2;

        if (resolution == Viewport.Msaa.Msaa8X)
            return 3;

        GD.PrintErr("invalid MSAA resolution value");
        return 0;
    }

    private Viewport.Msaa MSAAIndexToResolution(int index)
    {
        switch (index)
        {
            case 0:
                return Viewport.Msaa.Disabled;
            case 1:
                return Viewport.Msaa.Msaa2X;
            case 2:
                return Viewport.Msaa.Msaa4X;
            case 3:
                return Viewport.Msaa.Msaa8X;
            default:
                GD.PrintErr("invalid MSAA resolution index");
                return Viewport.Msaa.Disabled;
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
    ///   The sensitivity bars go from 0 to 100, but those aren't suitable scales for the input values so this converts
    ///   between them
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The reason this is done is that Godot sliders really don't like having really small fractions in them
    ///   </para>
    /// </remarks>
    /// <param name="value">The input sensitivity value</param>
    /// <returns>Value in range 0-100 to be used for a slider</returns>
    private int MouseInputSensitivityToBarValue(float value)
    {
        int converted = (int)(value / Constants.MOUSE_INPUT_SENSITIVITY_STEP);

        return Math.Clamp(converted, 0, 100);
    }

    /// <summary>
    ///   Variant of <see cref="MouseInputSensitivityToBarValue"/> for controller inputs
    /// </summary>
    private float MouseInputBarValueToSensitivity(float value)
    {
        return value * Constants.MOUSE_INPUT_SENSITIVITY_STEP;
    }

    private int ControllerInputSensitivityToBarValue(float value)
    {
        int converted = (int)(value / Constants.CONTROLLER_INPUT_SENSITIVITY_STEP);

        return Math.Clamp(converted, 0, 100);
    }

    private float ControllerInputBarValueToSensitivity(float value)
    {
        return value * Constants.CONTROLLER_INPUT_SENSITIVITY_STEP;
    }

    private int MouseInputScalingToIndex(MouseInputScaling scaling)
    {
        switch (scaling)
        {
            case MouseInputScaling.None:
                return 0;
            case MouseInputScaling.Scale:
                return 1;
            case MouseInputScaling.ScaleReverse:
                return 2;
        }

        GD.PrintErr("invalid MouseInputScaling value");
        return 0;
    }

    private MouseInputScaling MouseInputScalingIndexToEnum(int index)
    {
        switch (index)
        {
            case 0:
                return MouseInputScaling.None;
            case 1:
                return MouseInputScaling.Scale;
            case 2:
                return MouseInputScaling.ScaleReverse;
            default:
                GD.PrintErr("invalid MouseInputScaling index");
                return MouseInputScaling.ScaleReverse;
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
        // Only update the text when the button is populated (otherwise this triggers an error when exiting the editor)
        if (audioOutputDeviceSelection.ItemCount > 0)
        {
            audioOutputDeviceSelection.SetItemText(0, Localization.Translate("DEFAULT_AUDIO_OUTPUT_DEVICE"));
        }
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

    private void LoadScreenEffects()
    {
        var screenEffects = SimulationParameters.Instance.GetAllScreenEffects();

        foreach (var effect in screenEffects.OrderBy(p => p.Index))
        {
            // The untranslated name will be translated automatically by Godot during runtime
            screenEffectSelect.AddItem(effect.UntranslatedName);
        }
    }

    private void UpdateCurrentLanguageProgress()
    {
        string locale = TranslationServer.GetLocale();
        float progress = 100 * SimulationParameters.Instance.GetTranslationsInfo().TranslationProgress[locale];

        string textFormat;

        if (progress >= 0 && progress < Constants.TRANSLATION_VERY_INCOMPLETE_THRESHOLD)
        {
            textFormat = Localization.Translate("LANGUAGE_TRANSLATION_PROGRESS_REALLY_LOW");
        }
        else if (progress >= 0 && progress < Constants.TRANSLATION_INCOMPLETE_THRESHOLD)
        {
            textFormat = Localization.Translate("LANGUAGE_TRANSLATION_PROGRESS_LOW");
        }
        else
        {
            textFormat = Localization.Translate("LANGUAGE_TRANSLATION_PROGRESS");
        }

        languageProgressLabel.Text = textFormat.FormatSafe(MathF.Floor(progress));
    }

    // GUI Control Callbacks

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

        ClearInputRebindingControls();
        EmitSignal(SignalName.OnOptionsClosed);
        return true;
    }

    private void UpdateDetectedCPUCount()
    {
        detectedCPUCount.Text = TaskExecutor.CPUCount.ToString(CultureInfo.CurrentCulture);

        threadCountSlider.MinValue = TaskExecutor.MinimumThreadCount;
        threadCountSlider.MaxValue = TaskExecutor.MaximumThreadCount;
        nativeThreadCountSlider.MinValue = 1;
        nativeThreadCountSlider.MaxValue = TaskExecutor.MaximumThreadCount;

        int threads;

        if (Settings.Instance.UseManualThreadCount)
        {
            threads = Settings.Instance.ThreadCount;
        }
        else
        {
            threads = TaskExecutor.GetWantedThreadCount(Settings.Instance.AssumeCPUHasHyperthreading,
                Settings.Instance.RunAutoEvoDuringGamePlay);

            activeThreadCount.Text = threads.ToString(CultureInfo.CurrentCulture);
            threadCountSlider.Value = threads;
        }

        int nativeThreads;

        if (!Settings.Instance.UseManualNativeThreadCount.Value)
        {
            nativeThreads = TaskExecutor.CalculateNativeThreadCountFromManagedThreads(threads);
            nativeThreadCountSlider.Value = nativeThreads;
        }
        else
        {
            nativeThreads = Settings.Instance.NativeThreadCount;
        }

        activeThreadCount.Text = $"{threads}+{nativeThreads}";
    }

    private void UpdateDismissedNoticeCount()
    {
        dismissedNoticeCount.Text = Settings.Instance.PermanentlyDismissedNotices.Value.Count.ToString();
    }

    private void UpdateShownCommit()
    {
        var info = SimulationParameters.Instance.GetBuildInfoIfExists();

#if DEBUG
        var prefix = Localization.Translate("UNCERTAIN_VERSION_WARNING") + "\n";
#else
        var prefix = string.Empty;
#endif

        if (info == null)
        {
            builtAtLabel.Text = string.Empty;

            if (!string.IsNullOrEmpty(Constants.VersionCommit))
            {
                commitLabel.Text = Constants.VersionCommit;
                return;
            }

            commitLabel.Text = Localization.Translate("UNKNOWN_VERSION");
            return;
        }

        commitLabel.Text = prefix + info.Commit;

        var time = info.BuiltAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

        builtAtLabel.Text = time;
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
            tutorialsEnabled.ButtonPressed = savedTutorialsEnabled;
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
        EmitSignal(SignalName.OnOptionsClosed);
    }

    private void BackDiscardSelected()
    {
        Settings.Instance.LoadFromObject(savedSettings);
        Settings.Instance.ApplyAll();
        ApplySettingsToControls(Settings.Instance);

        if (optionsMode == OptionsMode.InGame)
        {
            gameProperties!.TutorialState.Enabled = savedTutorialsEnabled;
            tutorialsEnabled.ButtonPressed = savedTutorialsEnabled;
        }

        backConfirmationBox.Hide();

        UpdateResetSaveButtonState();
        EmitSignal(SignalName.OnOptionsClosed);
    }

    private void BackCancelSelected()
    {
        backConfirmationBox.Hide();
    }

    private void InputDefaultsConfirm()
    {
        // TODO: should this also reset the input sensitivity values? currently this only resets key bindings
        // and the button text has been updated to reflect this
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

    private void OnDamageEffectToggled(bool pressed)
    {
        Settings.Instance.ScreenDamageEffect.Value = pressed;

        UpdateResetSaveButtonState();
    }

    private void OnStrainVisibilityModeSelected(int index)
    {
        Settings.Instance.StrainBarVisibilityMode.Value = (Settings.StrainBarVisibility)index;

        UpdateResetSaveButtonState();
    }

    private int ControllerPromptTypeToIndex(ControllerType controllerType)
    {
        // This is done like this to ensure that invalid values don't get converted to out of range values (for
        // example when settings might be loaded that were saved by a newer Thrive version)
        switch (controllerType)
        {
            case ControllerType.Automatic:
            case ControllerType.Xbox360:
            case ControllerType.XboxOne:
            case ControllerType.XboxSeriesX:
            case ControllerType.PlayStation3:
            case ControllerType.PlayStation4:
            case ControllerType.PlayStation5:
                return (int)controllerType;
            default:
                GD.PrintErr("Invalid controller type value");
                return 0;
        }
    }

    private ControllerType ControllerIndexToPromptType(int index)
    {
        if (index is >= 0 and <= (int)ControllerType.PlayStation5)
        {
            return (ControllerType)index;
        }

        GD.PrintErr("Invalid controller type index");
        return ControllerType.Automatic;
    }

    private void OnControllerTypeSelected(int index)
    {
        Settings.Instance.ControllerPromptType.Value = ControllerIndexToPromptType(index);

        UpdateResetSaveButtonState();
    }

    private void OnDisplayBackgroundParticlesToggled(bool toggle)
    {
        Settings.Instance.DisplayBackgroundParticles.Value = toggle;

        UpdateResetSaveButtonState();
    }

    private void OnMicrobeBackgroundDistortionToggled(bool toggle)
    {
        if (toggle)
        {
            Settings.Instance.MicrobeDistortionStrength.Value = Constants.DEFAULT_MICROBE_DISTORTION_STRENGHT;
        }
        else
        {
            Settings.Instance.MicrobeDistortionStrength.Value = 0;
        }

        UpdateResetSaveButtonState();
    }

    private void OnGUILightEffectsToggled(bool toggle)
    {
        Settings.Instance.GUILightEffectsEnabled.Value = toggle;

        UpdateResetSaveButtonState();
    }

    private void OnDisplayPartNamesToggled(bool toggle)
    {
        Settings.Instance.DisplayPartNames.Value = toggle;

        UpdateResetSaveButtonState();
    }

    private void OnDisplay3DMenuBackgroundsToggled(bool toggle)
    {
        Settings.Instance.Menu3DBackgroundEnabled.Value = toggle;

        UpdateResetSaveButtonState();
    }

    private void OnBloomToggled(bool toggle)
    {
        Settings.Instance.BloomEnabled.Value = toggle;

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

    private void OnRunSimulationMultithreadedToggled(bool pressed)
    {
        Settings.Instance.RunGameSimulationMultithreaded.Value = pressed;

        UpdateResetSaveButtonState();
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

        // Apply the current value to the slider to make sure it is showing the actual setting value
        if (pressed)
        {
            threadCountSlider.Value = Settings.Instance.ThreadCount.Value;
        }
    }

    private void OnManualThreadCountChanged(float value)
    {
        // Ignore setting these things when we are using automatic thread count to prevent unnecessarily settings
        // being detected as changed
        if (!Settings.Instance.UseManualThreadCount.Value)
            return;

        int threads = Math.Clamp((int)value, TaskExecutor.MinimumThreadCount, TaskExecutor.MaximumThreadCount);
        Settings.Instance.ThreadCount.Value = threads;
        Settings.Instance.ApplyThreadSettings();

        UpdateResetSaveButtonState();
        UpdateDetectedCPUCount();
    }

    private void OnManualNativeThreadsToggled(bool pressed)
    {
        Settings.Instance.UseManualNativeThreadCount.Value = pressed;
        Settings.Instance.ApplyThreadSettings();

        nativeThreadCountSlider.Editable = pressed;

        UpdateResetSaveButtonState();
        UpdateDetectedCPUCount();

        if (pressed)
        {
            nativeThreadCountSlider.Value = Settings.Instance.NativeThreadCount.Value;
        }
    }

    private void OnManualNativeThreadCountChanged(float value)
    {
        if (!Settings.Instance.UseManualNativeThreadCount.Value)
            return;

        Settings.Instance.NativeThreadCount.Value = (int)value;
        Settings.Instance.ApplyThreadSettings();

        UpdateResetSaveButtonState();
        UpdateDetectedCPUCount();
    }

    private void OnMaxSpawnedEntitiesSelected(int index)
    {
        Settings.Instance.MaxSpawnedEntities.Value = MaxEntitiesIndexToValue(index);

        UpdateResetSaveButtonState();
    }

    // Input Callbacks
    private void OnMouseAxesBoundToggled(bool pressed)
    {
        mouseVerticalSensitivity.Editable = !pressed;
        mouseVerticalSensitivity.FocusMode = pressed ? FocusModeEnum.Click : FocusModeEnum.All;

        if (pressed)
        {
            mouseVerticalSensitivity.Value = mouseHorizontalSensitivity.Value;
        }
    }

    private void OnMouseHorizontalSensitivityChanged(float value)
    {
        Settings.Instance.HorizontalMouseLookSensitivity.Value = MouseInputBarValueToSensitivity(value);

        if (mouseAxisSensitivitiesBound.ButtonPressed)
        {
            mouseVerticalSensitivity.Value = mouseHorizontalSensitivity.Value;
        }

        UpdateResetSaveButtonState();
    }

    private void OnInvertedMouseHorizontalToggled(bool pressed)
    {
        Settings.Instance.InvertHorizontalMouseLook.Value = pressed;

        UpdateResetSaveButtonState();
    }

    private void OnMouseVerticalSensitivityChanged(float value)
    {
        Settings.Instance.VerticalMouseLookSensitivity.Value = MouseInputBarValueToSensitivity(value);

        UpdateResetSaveButtonState();
    }

    private void OnInvertedMouseVerticalToggled(bool pressed)
    {
        Settings.Instance.InvertVerticalMouseLook.Value = pressed;

        UpdateResetSaveButtonState();
    }

    private void OnMouseSensitivityScaleModeSelected(int index)
    {
        Settings.Instance.ScaleMouseInputByWindowSize.Value = MouseInputScalingIndexToEnum(index);

        UpdateResetSaveButtonState();
    }

    private void OnMouseScaleLogicalWindowSizeToggled(bool pressed)
    {
        Settings.Instance.InputWindowSizeIsLogicalSize.Value = pressed;

        UpdateResetSaveButtonState();
    }

    private void OnControllerAxesBoundToggled(bool pressed)
    {
        controllerVerticalSensitivity.Editable = !pressed;

        if (pressed)
        {
            controllerVerticalSensitivity.Value = controllerHorizontalSensitivity.Value;
        }
    }

    private void OnControllerHorizontalSensitivityChanged(float value)
    {
        Settings.Instance.HorizontalControllerLookSensitivity.Value = ControllerInputBarValueToSensitivity(value);

        if (controllerAxisSensitivitiesBound.ButtonPressed)
        {
            controllerVerticalSensitivity.Value = value;
        }

        UpdateResetSaveButtonState();
    }

    private void OnInvertedControllerHorizontalToggled(bool pressed)
    {
        Settings.Instance.InvertHorizontalControllerLook.Value = pressed;

        UpdateResetSaveButtonState();
    }

    private void OnControllerVerticalSensitivityChanged(float value)
    {
        Settings.Instance.VerticalControllerLookSensitivity.Value = ControllerInputBarValueToSensitivity(value);

        UpdateResetSaveButtonState();
    }

    private void OnInvertedControllerVerticalToggled(bool pressed)
    {
        Settings.Instance.InvertVerticalControllerLook.Value = pressed;

        UpdateResetSaveButtonState();
    }

    private int Movement2DToIndex(TwoDimensionalMovementMode movementMode)
    {
        switch (movementMode)
        {
            case TwoDimensionalMovementMode.Automatic:
            case TwoDimensionalMovementMode.PlayerRelative:
            case TwoDimensionalMovementMode.ScreenRelative:
                return (int)movementMode;
            default:
                GD.PrintErr("Invalid 2D movement type value");
                return 0;
        }
    }

    private TwoDimensionalMovementMode Movement2DIndexToType(int index)
    {
        if (index is >= 0 and <= (int)TwoDimensionalMovementMode.ScreenRelative)
        {
            return (TwoDimensionalMovementMode)index;
        }

        GD.PrintErr("Invalid movement 2D type index");
        return TwoDimensionalMovementMode.Automatic;
    }

    private void OnMovement2DTypeSelected(int index)
    {
        Settings.Instance.TwoDimensionalMovement.Value = Movement2DIndexToType(index);

        UpdateResetSaveButtonState();
    }

    private int Movement3DToIndex(ThreeDimensionalMovementMode movementMode)
    {
        switch (movementMode)
        {
            case ThreeDimensionalMovementMode.ScreenRelative:
            case ThreeDimensionalMovementMode.WorldRelative:
                return (int)movementMode;
            default:
                GD.PrintErr("Invalid 3D movement type value");
                return 0;
        }
    }

    private ThreeDimensionalMovementMode Movement3DIndexToMovementType(int index)
    {
        if (index is >= 0 and <= (int)ThreeDimensionalMovementMode.WorldRelative)
        {
            return (ThreeDimensionalMovementMode)index;
        }

        GD.PrintErr("Invalid 3D movement type index");
        return ThreeDimensionalMovementMode.ScreenRelative;
    }

    private void OnMovement3DTypeSelected(int index)
    {
        Settings.Instance.ThreeDimensionalMovement.Value = Movement3DIndexToMovementType(index);

        UpdateResetSaveButtonState();
    }

    private void OnMouseEdgePanToggled(bool pressed)
    {
        Settings.Instance.PanStrategyViewWithMouse.Value = pressed;
        mouseEdgePanSensitivity.Editable = pressed;
        mouseEdgePanSensitivity.FocusMode = pressed ? FocusModeEnum.All : FocusModeEnum.Click;

        UpdateResetSaveButtonState();
    }

    private void OnMouseEdgePanSensitivityChanged(float value)
    {
        Settings.Instance.PanStrategyViewMouseSpeed.Value = value;

        UpdateResetSaveButtonState();
    }

    private void OnOpenDeadzoneConfigurationPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        deadzoneConfigurationPopup.PopupCenteredShrink();
    }

    private void OnDeadzoneConfigurationChanged(List<float> deadzones)
    {
        Settings.Instance.ControllerAxisDeadzoneAxes.Value = deadzones;

        UpdateResetSaveButtonState();
    }

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

    private void OnScreenEffectSelected(int index)
    {
        var effect = SimulationParameters.Instance.GetScreenEffectByIndex(index);

        if (effect == SimulationParameters.Instance.GetScreenEffectByIndex(0))
            effect = null;

        Settings.Instance.CurrentScreenEffect.Value = effect;

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

    private void ClearInputRebindingControls()
    {
        inputGroupList.ClearGroupList();
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

    private void OnWebFeedsEnabledToggled(bool pressed)
    {
        Settings.Instance.ThriveNewsFeedEnabled.Value = pressed;

        UpdateResetSaveButtonState();
    }

    private void OnPatchNotesEnabledToggled(bool pressed)
    {
        Settings.Instance.ShowNewPatchNotes.Value = pressed;

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

    private void OnResetDismissedPopups()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Settings.Instance.PermanentlyDismissedNotices.Value = new HashSet<DismissibleNotice>();

        UpdateResetSaveButtonState();
        UpdateDismissedNoticeCount();
    }

    private void OnOpenPatchNotesPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        patchNotesDisplayer.ShowLatest();
        patchNotesBox.PopupCenteredShrink();
    }
}
