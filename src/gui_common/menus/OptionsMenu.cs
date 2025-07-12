using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Saving;
using Tutorial;

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
    /// <summary>
    ///   Sliders aren't disabled by default (or at least the ones most recently added) when the relevant option is
    ///   disabled as navigation focus isn't super clear in those cases due to:
    ///   TODO: https://github.com/Revolutionary-Games/Thrive/issues/4010
    /// </summary>
    [Export]
    public bool DisableInactiveSliders;

    private static readonly Lazy<List<string>> LanguagesCache = new(() =>
        TranslationServer.GetLoadedLocales().OrderBy(i => i, StringComparer.InvariantCulture).ToList());

    // TODO: this should be refreshed periodically to support user plugging in new devices
    private static readonly List<string> AudioOutputDevicesCache = AudioServer
        .GetOutputDeviceList().Where(d => d != Constants.DEFAULT_AUDIO_OUTPUT_DEVICE_NAME)
        .Prepend(Constants.DEFAULT_AUDIO_OUTPUT_DEVICE_NAME).ToList();

#pragma warning disable CA2213
    [Export]
    private Button backButton = null!;

    [Export]
    private Button resetButton = null!;

    [Export]
    private Button saveButton = null!;

    // Tab selector buttons
    [Export]
    private TabButtons tabButtons = null!;

    [Export]
    private Button graphicsButton = null!;

    [Export]
    private Button soundButton = null!;

    [Export]
    private Button performanceButton = null!;

    [Export]
    private Button inputsButton = null!;

    [Export]
    private Button miscButton = null!;

    // Graphics tab
    [Export]
    private Control graphicsTab = null!;

    [Export]
    private CheckBox vsync = null!;

    [Export]
    private Label? resolution;

    [Export]
    private OptionButton displayMode = null!;

    [Export]
    private OptionButton antiAliasingMode = null!;

    [Export]
    private Control msaaSection = null!;

    [Export]
    private OptionButton msaaResolution = null!;

    [Export]
    private Slider renderScale = null!;

    [Export]
    private Label renderScaleLabel = null!;

    [Export]
    private OptionButton upscalingMethod = null!;

    [Export]
    private Slider upscalingSharpening = null!;

    [Export]
    private OptionButton maxFramesPerSecond = null!;

    [Export]
    private OptionButton colourblindSetting = null!;

    [Export]
    private CheckBox chromaticAberrationToggle = null!;

    [Export]
    private Slider chromaticAberrationSlider = null!;

    [Export]
    private OptionButton controllerPromptType = null!;

    [Export]
    private CheckBox displayAbilitiesHotBarToggle = null!;

    [Export]
    private OptionButton anisotropicFilterLevel = null!;

    [Export]
    private CheckBox damageEffect = null!;

    [Export]
    private OptionButton strainVisibility = null!;

    [Export]
    private CheckBox displayBackgroundParticlesToggle = null!;

    [Export]
    private CheckBox guiLightEffectsToggle = null!;

    [Export]
    private CheckBox displayPartNamesToggle = null!;

    [Export]
    private CheckBox displayMenu3DBackgroundsToggle = null!;

    [Export]
    private Button displayMicrobeBackgroundDistortionToggle = null!;

    [Export]
    private Button lowQualityBackgroundBlurToggle = null!;

    [Export]
    private Button bloomEffectToggle = null!;

    [Export]
    private Slider bloomSlider = null!;

    [Export]
    private Slider blurSlider = null!;

    [Export]
    private Label gpuName = null!;

    [Export]
    private Label usedRendererName = null!;

    [Export]
    private Label videoMemory = null!;

    // Sound tab
    [Export]
    private Control soundTab = null!;

    [Export]
    private Slider masterVolume = null!;

    [Export]
    private CheckBox masterMuted = null!;

    [Export]
    private Slider musicVolume = null!;

    [Export]
    private CheckBox musicMuted = null!;

    [Export]
    private Slider ambianceVolume = null!;

    [Export]
    private CheckBox ambianceMuted = null!;

    [Export]
    private Slider sfxVolume = null!;

    [Export]
    private CheckBox sfxMuted = null!;

    [Export]
    private Slider guiVolume = null!;

    [Export]
    private CheckBox guiMuted = null!;

    [Export]
    private OptionButton audioOutputDeviceSelection = null!;

    [Export]
    private OptionButton languageSelection = null!;

    [Export]
    private Button resetLanguageButton = null!;

    [Export]
    private Label languageProgressLabel = null!;

    // Performance tab
    [Export]
    private Control performanceTab = null!;

    [Export]
    private OptionButton cloudInterval = null!;

    [Export]
    private VBoxContainer cloudResolutionTitle = null!;

    [Export]
    private OptionButton cloudResolution = null!;

    [Export]
    private CheckBox runAutoEvoDuringGameplay = null!;

    [Export]
    private CheckBox runGameSimulationMultithreaded = null!;

    [Export]
    private Label detectedCPUCount = null!;

    [Export]
    private Label activeThreadCount = null!;

    [Export]
    private CheckBox assumeHyperthreading = null!;

    [Export]
    private CheckBox useManualThreadCount = null!;

    [Export]
    private Slider threadCountSlider = null!;

    [Export]
    private CheckBox useManualNativeThreadCount = null!;

    [Export]
    private Slider nativeThreadCountSlider = null!;

    [Export]
    private OptionButton maxSpawnedEntities = null!;

    [Export]
    private CheckBox useDiskCaching = null!;

    [Export]
    private Slider maxCacheSizeSlider = null!;

    [Export]
    private Label maxCacheSizeLabel = null!;

    [Export]
    private Label currentCacheSize = null!;

    // Advanced cache settings
    [Export]
    private Slider maxMemoryCacheTimeSlider = null!;

    [Export]
    private Label maxMemoryCacheTimeLabel = null!;

    [Export]
    private Slider maxDiskCacheTimeSlider = null!;

    [Export]
    private Label maxDiskCacheTimeLabel = null!;

    [Export]
    private Slider maxMemoryItemsSlider = null!;

    [Export]
    private Label maxMemoryItemsLabel = null!;

    [Export]
    private Slider maxMemoryOnlyCacheTimeSlider = null!;

    [Export]
    private Label maxMemoryOnlyCacheTimeLabel = null!;

    // Inputs tab
    [Export]
    private Control inputsTab = null!;

    [Export]
    private Button mouseAxisSensitivitiesBound = null!;

    [Export]
    private Slider mouseHorizontalSensitivity = null!;

    [Export]
    private Button mouseHorizontalInverted = null!;

    [Export]
    private Slider mouseVerticalSensitivity = null!;

    [Export]
    private Button mouseVerticalInverted = null!;

    [Export]
    private OptionButton mouseWindowSizeScaling = null!;

    [Export]
    private Button mouseWindowSizeScalingWithLogicalSize = null!;

    [Export]
    private Button controllerAxisSensitivitiesBound = null!;

    [Export]
    private Slider controllerHorizontalSensitivity = null!;

    [Export]
    private Button controllerHorizontalInverted = null!;

    [Export]
    private Slider controllerVerticalSensitivity = null!;

    [Export]
    private Button controllerVerticalInverted = null!;

    [Export]
    private OptionButton twoDimensionalMovement = null!;

    [Export]
    private OptionButton threeDimensionalMovement = null!;

    [Export]
    private Button mouseEdgePanEnabled = null!;

    [Export]
    private Slider mouseEdgePanSensitivity = null!;

    [Export]
    private InputGroupList inputGroupList = null!;

    [Export]
    private ControllerDeadzoneConfiguration deadzoneConfigurationPopup = null!;

    // Misc tab
    [Export]
    private Control miscTab = null!;

    [Export]
    private CheckBox playIntro = null!;

    [Export]
    private CheckBox playMicrobeIntro = null!;

    [Export]
    private CheckBox cheats = null!;

    [Export]
    private CheckBox tutorialsEnabledOnNewGame = null!;

    [Export]
    private CheckBox autoSave = null!;

    [Export]
    private SpinBox maxAutoSaves = null!;

    [Export]
    private SpinBox maxQuickSaves = null!;

    [Export]
    private CheckBox customUsernameEnabled = null!;

    [Export]
    private LineEdit customUsername = null!;

    [Export]
    private CheckBox webFeedsEnabled = null!;

    [Export]
    private Button microbeRippleEffect = null!;

    [Export]
    private Button microbeCameraTilt = null!;

    [Export]
    private CheckBox showNewPatchNotes = null!;

    [Export]
    private Label dismissedNoticeCount = null!;

    [Export]
    private OptionButton jsonDebugMode = null!;

    [Export]
    private OptionButton screenEffectSelect = null!;

    [Export]
    private Label commitLabel = null!;

    [Export]
    private Label builtAtLabel = null!;

    [Export]
    private CheckBox tutorialsEnabled = null!;

    [Export]
    private CheckBox unsavedProgressWarningEnabled = null!;

    // Confirmation Boxes
    [Export]
    private CustomConfirmationDialog screenshotDirectoryWarningBox = null!;

    [Export]
    private CustomWindow backConfirmationBox = null!;

    [Export]
    private CustomConfirmationDialog defaultsConfirmationBox = null!;

    [Export]
    private ErrorDialog errorAcceptBox = null!;

    [Export]
    private CustomWindow patchNotesBox = null!;

    [Export]
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

    private double displayedCacheSize = -1;

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

        builtAtLabel.RegisterCustomFocusDrawer();
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
    ///   A few checks need to re-run periodically so this does that. This uses physics process purely to run less
    ///   often than on each frame.
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        UpdateCurrentCacheSize();
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
        displayMode.Selected = DisplayModeToIndex(settings.DisplayMode);
        antiAliasingMode.Selected = AntiAliasingModeToIndex(settings.AntiAliasing);
        msaaResolution.Selected = MSAAResolutionToIndex(settings.MSAAResolution);
        anisotropicFilterLevel.Selected = AnisotropicFilterLevelToIndex(settings.AnisotropicFilterLevel);
        maxFramesPerSecond.Selected = MaxFPSValueToIndex(settings.MaxFramesPerSecond);
        renderScale.Value = settings.RenderScale;
        upscalingMethod.Selected = UpscalingMethodValueToIndex(settings.UpscalingMethod);
        upscalingSharpening.Value = settings.UpscalingSharpening;
        upscalingSharpening.Editable = settings.UpscalingMethod.Value != Settings.UpscalingMode.Bilinear;
        colourblindSetting.Selected = settings.ColourblindSetting;
        chromaticAberrationSlider.Value = settings.ChromaticAmount;
        chromaticAberrationToggle.ButtonPressed = settings.ChromaticEnabled;
        chromaticAberrationSlider.Editable = settings.ChromaticEnabled || !DisableInactiveSliders;
        controllerPromptType.Selected = ControllerPromptTypeToIndex(settings.ControllerPromptType);
        displayAbilitiesHotBarToggle.ButtonPressed = settings.DisplayAbilitiesHotBar;
        damageEffect.ButtonPressed = settings.ScreenDamageEffect;
        strainVisibility.Selected = (int)settings.StrainBarVisibilityMode.Value;
        displayBackgroundParticlesToggle.ButtonPressed = settings.DisplayBackgroundParticles;
        displayMicrobeBackgroundDistortionToggle.ButtonPressed = settings.MicrobeDistortionStrength.Value > 0;
        lowQualityBackgroundBlurToggle.ButtonPressed = settings.MicrobeBackgroundBlurLowQuality;
        microbeRippleEffect.ButtonPressed = settings.MicrobeRippleEffect;
        microbeCameraTilt.ButtonPressed = settings.MicrobeCameraTilt;
        guiLightEffectsToggle.ButtonPressed = settings.GUILightEffectsEnabled;
        displayPartNamesToggle.ButtonPressed = settings.DisplayPartNames;
        displayMenu3DBackgroundsToggle.ButtonPressed = settings.Menu3DBackgroundEnabled;
        bloomEffectToggle.ButtonPressed = settings.BloomEnabled;
        bloomSlider.Value = settings.BloomStrength;
        bloomSlider.Editable = settings.BloomEnabled || !DisableInactiveSliders;
        blurSlider.Value = settings.MicrobeBackgroundBlurStrength;
        DisplayResolution();
        DisplayGpuInfo();
        UpdateRenderScale();
        UpdateMSAAVisibility();

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
        useDiskCaching.ButtonPressed = settings.UseDiskCache;
        maxCacheSizeSlider.Value = settings.DiskCacheSize;
        maxMemoryCacheTimeSlider.Value = settings.DiskMemoryCachePortionTime;
        maxDiskCacheTimeSlider.Value = settings.DiskCacheMaxTime;
        maxMemoryItemsSlider.Value = settings.MemoryCacheMaxSize;
        maxMemoryOnlyCacheTimeSlider.Value = settings.MemoryOnlyCacheTime;

        UpdateDetectedCPUCount();
        UpdateMaxCacheSize();
        UpdateMaxDiskCacheItemTime();
        UpdateMaxMemoryItems();
        UpdateDiskMemoryPortionCacheTime();
        UpdateMemoryOnlyCacheTime();
        ApplyCacheSliderEnabledStates();

        UpdateCurrentCacheSize();

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

        // Lock out some graphics settings based on the used renderer
        if (FeatureInformation.GetVideoDriver() == OS.RenderingDriver.Opengl3)
        {
            upscalingMethod.Disabled = true;
            upscalingSharpening.Editable = false;
        }
        else
        {
            upscalingMethod.Disabled = false;
        }
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

        // Apply scaling
        var scale = renderScale.Value;

        var adjusted = screenResolution * (float)scale;

        resolution.Text = Localization.Translate("AUTO_RESOLUTION")
            .FormatSafe(Math.Round(adjusted.X), Math.Round(adjusted.Y));
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

    private int DisplayModeToIndex(Settings.DisplayModeEnum mode)
    {
        switch (mode)
        {
            case Settings.DisplayModeEnum.Windowed:
                return 0;
            case Settings.DisplayModeEnum.Fullscreen:
                return 1;
            case Settings.DisplayModeEnum.ExclusiveFullscreen:
                return 2;
            default:
                GD.PrintErr("invalid display mode value");
                return 0;
        }
    }

    private Settings.DisplayModeEnum DisplayIndexToEnum(int index)
    {
        switch (index)
        {
            case 0:
                return Settings.DisplayModeEnum.Windowed;
            case 1:
                return Settings.DisplayModeEnum.Fullscreen;
            case 2:
                return Settings.DisplayModeEnum.ExclusiveFullscreen;
            default:
                GD.PrintErr("invalid display mode index");
                return Settings.DisplayModeEnum.Windowed;
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

    private int AnisotropicFilterLevelToIndex(Viewport.AnisotropicFiltering level)
    {
        if (level == Viewport.AnisotropicFiltering.Disabled)
            return 0;

        if (level == Viewport.AnisotropicFiltering.Anisotropy2X)
            return 1;

        if (level == Viewport.AnisotropicFiltering.Anisotropy4X)
            return 2;

        if (level == Viewport.AnisotropicFiltering.Anisotropy8X)
            return 3;

        if (level == Viewport.AnisotropicFiltering.Anisotropy16X)
            return 4;

        GD.PrintErr("invalid anisotropic filtering level index");
        return 3;
    }

    private Viewport.AnisotropicFiltering AnisotropicFilteringIndexToLevel(int index)
    {
        switch (index)
        {
            case 0:
                return Viewport.AnisotropicFiltering.Disabled;
            case 1:
                return Viewport.AnisotropicFiltering.Anisotropy2X;
            case 2:
                return Viewport.AnisotropicFiltering.Anisotropy4X;
            case 3:
                return Viewport.AnisotropicFiltering.Anisotropy8X;
            case 4:
                return Viewport.AnisotropicFiltering.Anisotropy16X;
            default:
                GD.PrintErr("invalid anisotropic filtering level index");
                return Viewport.AnisotropicFiltering.Anisotropy8X;
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

    private int UpscalingMethodValueToIndex(Settings.UpscalingMode value)
    {
        switch (value)
        {
            case Settings.UpscalingMode.Bilinear:
                return 0;
            case Settings.UpscalingMode.Fsr1:
                return 1;
            case Settings.UpscalingMode.Fsr2:
                return 2;
            default:
                GD.PrintErr("invalid upscaling method value");
                return 0;
        }
    }

    private Settings.UpscalingMode UpscalingMethodIndexToValue(int index)
    {
        switch (index)
        {
            case 0:
                return Settings.UpscalingMode.Bilinear;
            case 1:
                return Settings.UpscalingMode.Fsr1;
            case 2:
                return Settings.UpscalingMode.Fsr2;
            default:
                GD.PrintErr("invalid upscaling index");
                return Settings.UpscalingMode.Bilinear;
        }
    }

    private int AntiAliasingModeToIndex(Settings.AntiAliasingMode value)
    {
        switch (value)
        {
            case Settings.AntiAliasingMode.MSAA:
                return 0;
            case Settings.AntiAliasingMode.TemporalAntiAliasing:
                return 1;
            case Settings.AntiAliasingMode.MSAAAndTemporal:
                return 2;
            case Settings.AntiAliasingMode.ScreenSpaceFx:
                return 3;
            case Settings.AntiAliasingMode.Disabled:
                return 4;
            default:
                GD.PrintErr("invalid anti-aliasing value");
                return 0;
        }
    }

    private Settings.AntiAliasingMode AntiAliasingIndexToValue(int index)
    {
        switch (index)
        {
            case 0:
                return Settings.AntiAliasingMode.MSAA;
            case 1:
                return Settings.AntiAliasingMode.TemporalAntiAliasing;
            case 2:
                return Settings.AntiAliasingMode.MSAAAndTemporal;
            case 3:
                return Settings.AntiAliasingMode.ScreenSpaceFx;
            case 4:
                return Settings.AntiAliasingMode.Disabled;
            default:
                GD.PrintErr("invalid anti-aliasing index");
                return Settings.AntiAliasingMode.MSAA;
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

    private void UpdateCurrentCacheSize()
    {
        var wantedDisplay = Math.Round((double)DiskCache.Instance.TotalCacheSize / Constants.MEBIBYTE, 1);

        if (Math.Abs(wantedDisplay - displayedCacheSize) > 0.01)
        {
            displayedCacheSize = wantedDisplay;
            currentCacheSize.Text = Localization.Translate("MIB_VALUE").FormatSafe(wantedDisplay);
        }
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
    private void OnDisplayModeSelected(int index)
    {
        Settings.Instance.DisplayMode.Value = DisplayIndexToEnum(index);
        Settings.Instance.ApplyWindowSettings();

        UpdateResetSaveButtonState();
    }

    private void OnVSyncToggled(bool pressed)
    {
        Settings.Instance.VSync.Value = pressed;
        Settings.Instance.ApplyWindowSettings();

        UpdateResetSaveButtonState();
    }

    private void OnAntiAliasingModeSelected(int index)
    {
        Settings.Instance.AntiAliasing.Value = AntiAliasingIndexToValue(index);
        Settings.Instance.ApplyGraphicsSettings();

        UpdateResetSaveButtonState();
        UpdateMSAAVisibility();
    }

    private void UpdateMSAAVisibility()
    {
        msaaSection.Visible =
            Settings.Instance.AntiAliasing.Value is Settings.AntiAliasingMode.MSAA
                or Settings.AntiAliasingMode.MSAAAndTemporal;
    }

    private void OnMSAAResolutionSelected(int index)
    {
        Settings.Instance.MSAAResolution.Value = MSAAIndexToResolution(index);
        Settings.Instance.ApplyGraphicsSettings();

        UpdateResetSaveButtonState();
    }

    private void OnAnisotropicFilteringSelected(int index)
    {
        Settings.Instance.AnisotropicFilterLevel.Value = AnisotropicFilteringIndexToLevel(index);
        Settings.Instance.ApplyGraphicsSettings();

        UpdateResetSaveButtonState();
    }

    private void OnMaxFramesPerSecondSelected(int index)
    {
        Settings.Instance.MaxFramesPerSecond.Value = MaxFPSIndexToValue(index);
        Settings.Instance.ApplyGraphicsSettings();

        UpdateResetSaveButtonState();
    }

    private void OnRenderScaleChanged(float value)
    {
        Settings.Instance.RenderScale.Value = value;
        Settings.Instance.ApplyGraphicsSettings();

        UpdateResetSaveButtonState();
        DisplayResolution();
        UpdateRenderScale();
    }

    private void UpdateRenderScale()
    {
        renderScaleLabel.Text =
            Localization.Translate("PERCENTAGE_VALUE").FormatSafe(Math.Round(renderScale.Value * 100));
    }

    private void OnUpscalingMethodSelected(int index)
    {
        Settings.Instance.UpscalingMethod.Value = UpscalingMethodIndexToValue(index);
        Settings.Instance.ApplyGraphicsSettings();

        UpdateResetSaveButtonState();

        upscalingSharpening.Editable = Settings.Instance.UpscalingMethod.Value != Settings.UpscalingMode.Bilinear;
    }

    private void OnUpscalingSharpeningChanged(float value)
    {
        Settings.Instance.UpscalingSharpening.Value = value;
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
        chromaticAberrationSlider.Editable = toggle || !DisableInactiveSliders;

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

    private void OnLowQualityBackgroundBlurToggled(bool toggle)
    {
        Settings.Instance.MicrobeBackgroundBlurLowQuality.Value = toggle;

        UpdateResetSaveButtonState();
    }

    private void OnMicrobeRippleToggled(bool toggle)
    {
        Settings.Instance.MicrobeRippleEffect.Value = toggle;

        UpdateResetSaveButtonState();
    }

    private void OnMicrobeCameraTiltToggled(bool toggle)
    {
        Settings.Instance.MicrobeCameraTilt.Value = toggle;

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
        bloomSlider.Editable = toggle || !DisableInactiveSliders;

        UpdateResetSaveButtonState();
    }

    private void OnBloomStrengthChanged(float value)
    {
        Settings.Instance.BloomStrength.Value = value;

        UpdateResetSaveButtonState();
    }

    private void OnBlurStrengthChanged(float value)
    {
        Settings.Instance.MicrobeBackgroundBlurStrength.Value = value;

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

    private void OnClearDiskCachePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        GD.Print("Clearing disk cache due to clear button press");
        DiskCache.Instance.Clear();

        UpdateCurrentCacheSize();
    }

    private void OnDiskCachingToggled(bool pressed)
    {
        Settings.Instance.UseDiskCache.Value = pressed;

        UpdateResetSaveButtonState();
        ApplyCacheSliderEnabledStates();
    }

    private void ApplyCacheSliderEnabledStates()
    {
        bool diskEnabled = Settings.Instance.UseDiskCache.Value;

        maxCacheSizeSlider.Editable = diskEnabled;
        maxMemoryCacheTimeSlider.Editable = diskEnabled;
        maxDiskCacheTimeSlider.Editable = diskEnabled;
        maxMemoryCacheTimeSlider.Editable = diskEnabled;
        maxMemoryOnlyCacheTimeSlider.Editable = !diskEnabled;
    }

    private void OnMaxCacheSizeChanged(float value)
    {
        Settings.Instance.DiskCacheSize.Value = (long)(Math.Round(value / 1024.0f) * 1024);

        UpdateResetSaveButtonState();
        UpdateMaxCacheSize();
    }

    private void UpdateMaxCacheSize()
    {
        maxCacheSizeLabel.Text = Localization.Translate("MIB_VALUE")
            .FormatSafe(Math.Round((double)Settings.Instance.DiskCacheSize.Value / Constants.MEBIBYTE));
    }

    private void OnDiskCacheDurationChanged(float value)
    {
        Settings.Instance.DiskCacheMaxTime.Value = (float)Math.Round(value);

        UpdateResetSaveButtonState();
        UpdateMaxDiskCacheItemTime();
    }

    private void UpdateMaxDiskCacheItemTime()
    {
        maxDiskCacheTimeLabel.Text = Math.Round(Settings.Instance.DiskCacheMaxTime.Value / 3600.0, 1) + "h";
    }

    private void OnMemoryMaxItemsChanged(float value)
    {
        Settings.Instance.MemoryCacheMaxSize.Value = (int)value;

        UpdateResetSaveButtonState();
        UpdateMaxMemoryItems();
    }

    private void UpdateMaxMemoryItems()
    {
        maxMemoryItemsLabel.Text = Settings.Instance.MemoryCacheMaxSize.Value.ToString(CultureInfo.CurrentCulture);
    }

    private void OnMemoryDiskCacheTimeChanged(float value)
    {
        Settings.Instance.DiskMemoryCachePortionTime.Value = (float)Math.Round(value);

        UpdateResetSaveButtonState();
        UpdateDiskMemoryPortionCacheTime();
    }

    private void UpdateDiskMemoryPortionCacheTime()
    {
        maxMemoryCacheTimeLabel.Text = Math.Round(Settings.Instance.DiskMemoryCachePortionTime.Value) + "s";
    }

    private void OnMemoryOnlyCacheTimeChanged(float value)
    {
        Settings.Instance.MemoryOnlyCacheTime.Value = (float)Math.Round(value);

        UpdateResetSaveButtonState();
        UpdateMemoryOnlyCacheTime();
    }

    private void UpdateMemoryOnlyCacheTime()
    {
        maxMemoryOnlyCacheTimeLabel.Text = Math.Round(Settings.Instance.MemoryOnlyCacheTime.Value) + "s";
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

    private void OnResetShownTutorials()
    {
        GUICommon.Instance.PlayButtonPressSound();

        GD.Print("Clearing all seen tutorials");
        AlreadySeenTutorials.ResetAllSeenTutorials();
    }

    private void OnOpenPatchNotesPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        patchNotesDisplayer.ShowLatest();
        patchNotesBox.PopupCenteredShrink();
    }
}
