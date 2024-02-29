using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Godot;
using Newtonsoft.Json;
using Saving;
using Environment = System.Environment;

/// <summary>
///   Class that handles storing and applying player changeable game settings.
/// </summary>
public class Settings
{
    private static readonly List<string>
        AvailableLocales = TranslationServer.GetLoadedLocales().Cast<string>().ToList();

    private static readonly string DefaultLanguageValue = GetSupportedLocale(TranslationServer.GetLocale());
    private static readonly CultureInfo DefaultCultureValue = CultureInfo.CurrentCulture;
    private static readonly InputDataList DefaultControls = GetCurrentlyAppliedControls();

    /// <summary>
    ///   Singleton used for holding the live copy of game settings.
    /// </summary>
    private static readonly Settings SingletonInstance = InitializeGlobalSettings();

    static Settings()
    {
    }

    private Settings()
    {
        // This is mainly just to make sure the property is read here before anyone can change TranslationServer locale
        if (DefaultLanguage.Length < 1)
            GD.PrintErr("Default locale is empty");
    }

    public static Settings Instance => SingletonInstance;

    public static string DefaultLanguage => DefaultLanguageValue;

    public static CultureInfo DefaultCulture => DefaultCultureValue;

    /// <summary>
    ///   If environment is steam returns SteamHandler.DisplayName, else Environment.UserName
    /// </summary>
    public static string EnvironmentUserName => SteamHandler.Instance.IsLoaded ?
        SteamHandler.Instance.DisplayName :
        Environment.UserName;

    // Graphics Properties

    /// <summary>
    ///   Sets whether the game window is in fullscreen mode
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> FullScreen { get; private set; } = new(true);

    /// <summary>
    ///   Sets whether the game window will use vsync
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> VSync { get; private set; } = new(true);

    /// <summary>
    ///   Sets amount of MSAA to apply to the viewport
    /// </summary>
    [JsonProperty]
    public SettingValue<Viewport.MSAA> MSAAResolution { get; private set; } =
        new(Viewport.MSAA.Msaa2x);

    /// <summary>
    ///   Sets the maximum framerate of the game window
    /// </summary>
    [JsonProperty]
    public SettingValue<int> MaxFramesPerSecond { get; private set; } = new(360);

    /// <summary>
    ///   Optionally applies a colour filter to the screen to aid colourblind individuals
    ///   0 = None, 1 = Red/Green, 2 = Blue/Yellow
    /// </summary>
    [JsonProperty]
    public SettingValue<int> ColourblindSetting { get; private set; } = new(0);

    /// <summary>
    ///   The amount of Chromatic Aberration to apply to the screen
    /// </summary>
    [JsonProperty]
    public SettingValue<float> ChromaticAmount { get; private set; } = new(15.0f);

    /// <summary>
    ///   Enable or Disable Chromatic Aberration for screen
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> ChromaticEnabled { get; private set; } = new(true);

    /// <summary>
    ///   Type of controller button prompts to show
    /// </summary>
    [JsonProperty]
    public SettingValue<ControllerType> ControllerPromptType { get; private set; } = new(ControllerType.Automatic);

    /// <summary>
    ///   Display or hide the abilities hotbar in the microbe stage HUD.
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> DisplayAbilitiesHotBar { get; private set; } = new(true);

    /// <summary>
    ///   Display or hide the background particles in game background particles can also be in foreground
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> DisplayBackgroundParticles { get; private set; } = new(true);

    /// <summary>
    ///   Enable or disable lighting effects on the GUI. Mainly Used to workaround a bug where the HUD area
    ///   surrounding the editor button sometimes disappearing with the light effect turned on.
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> GUILightEffectsEnabled { get; private set; } = new(true);

    /// <summary>
    ///   Enable or disable 3D background scenes in the menu
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> Menu3DBackgroundEnabled { get; private set; } = new(true);

    /// <summary>
    ///   Display or hide part names in the editor, for accessibility reasons
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> DisplayPartNames { get; private set; } = new(false);

    // Sound Properties

    /// <summary>
    ///   The Db value to be added to the master audio bus
    /// </summary>
    [JsonProperty]
    public SettingValue<float> VolumeMaster { get; private set; } = new(0.0f);

    /// <summary>
    ///   If true all sounds are muted
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> VolumeMasterMuted { get; private set; } = new(false);

    /// <summary>
    ///   The Db value to be added to the music audio bus
    /// </summary>
    [JsonProperty]
    public SettingValue<float> VolumeMusic { get; private set; } = new(0.0f);

    /// <summary>
    ///   If true music is muted
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> VolumeMusicMuted { get; private set; } = new(false);

    /// <summary>
    ///   The Db value to be added to the ambiance audio bus
    /// </summary>
    [JsonProperty]
    public SettingValue<float> VolumeAmbiance { get; private set; } = new(0.0f);

    /// <summary>
    ///   If true ambiance is muted
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> VolumeAmbianceMuted { get; private set; } = new(false);

    /// <summary>
    ///   The Db value to be added to the sfx audio bus
    /// </summary>
    [JsonProperty]
    public SettingValue<float> VolumeSFX { get; private set; } = new(0.0f);

    /// <summary>
    ///   If true sfx is muted
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> VolumeSFXMuted { get; private set; } = new(false);

    /// <summary>
    ///   The Db value to be added to the gui audio bus
    /// </summary>
    [JsonProperty]
    public SettingValue<float> VolumeGUI { get; private set; } = new(0.0f);

    /// <summary>
    ///   If true gui audio bus is muted
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> VolumeGUIMuted { get; private set; } = new(false);

    [JsonProperty]
    public SettingValue<string?> SelectedAudioOutputDevice { get; private set; } =
        new(Constants.DEFAULT_AUDIO_OUTPUT_DEVICE_NAME);

    public SettingValue<string?> SelectedLanguage { get; private set; } = new(null);

    // Performance Properties

    /// <summary>
    ///   If this is over 0 then this limits how often compound clouds
    ///   are updated. The default value of 0.020 at 60 FPS makes
    ///   every other frame not update the clouds.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This should be made user configurable for different
    ///     computers. The choices should probably be:
    ///     0.0f, 0.020f, 0.040f, 0.1f, 0.25f
    ///   </para>
    /// </remarks>
    [JsonProperty]
    public SettingValue<float> CloudUpdateInterval { get; private set; } = new(0.040f);

    /// <summary>
    ///   This can be freely adjusted to adjust the performance The
    ///   higher this value is the smaller the size of the simulated
    ///   cloud is and the performance is better.
    /// </summary>
    [JsonProperty]
    public SettingValue<int> CloudResolution { get; private set; } = new(2);

    /// <summary>
    ///   If true an auto-evo run is started during gameplay, taking up one of the background threads.
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> RunAutoEvoDuringGamePlay { get; private set; } = new(true);

    /// <summary>
    ///   If true the game simulations can run in a multithreaded way (for example <see cref="MicrobeWorldSimulation"/>
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> RunGameSimulationMultithreaded { get; private set; } = new(true);

    /// <summary>
    ///   If true it is assumed that the CPU has hyperthreading, meaning that real cores is CPU count / 2
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> AssumeCPUHasHyperthreading { get; private set; } = new(true);

    /// <summary>
    ///   Only if this is true the ThreadCount will be followed
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> UseManualThreadCount { get; private set; } = new(false);

    /// <summary>
    ///   Manually set number of background threads to use. Needs to be at least 2 if RunAutoEvoDuringGamePlay is true
    /// </summary>
    [JsonProperty]
    public SettingValue<int> ThreadCount { get; private set; } = new(4);

    [JsonProperty]
    public SettingValue<bool> UseManualNativeThreadCount { get; private set; } = new(false);

    /// <summary>
    ///   Manually set number of native threads to use. Applies similarly to the C# side of things (i.e. only when
    ///   manual count is enabled)
    /// </summary>
    [JsonProperty]
    public SettingValue<int> NativeThreadCount { get; private set; } = new(3);

    /// <summary>
    ///   Sets the maximum number of entities that can exist at one time.
    /// </summary>
    [JsonProperty(PropertyName = "MaxSpawnedEntitiesV2")]
    public SettingValue<int> MaxSpawnedEntities { get; private set; } = new(Constants.NORMAL_MAX_SPAWNED_ENTITIES);

    // Misc Properties

    /// <summary>
    ///   When true the main intro is played. Note <see cref="LaunchOptions.VideosEnabled"/> must also be true to play
    ///   any videos as they need to be able to be skipped due to a rare Godot engine crash when playing them.
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> PlayIntroVideo { get; private set; } = new(true);

    /// <summary>
    ///   When true the microbe intro is played on new game
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> PlayMicrobeIntroVideo { get; private set; } = new(true);

    /// <summary>
    ///   If false auto saving will be disabled
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> AutoSaveEnabled { get; private set; } = new(true);

    /// <summary>
    ///   Number of auto saves to keep
    /// </summary>
    [JsonProperty]
    public SettingValue<int> MaxAutoSaves { get; private set; } = new(5);

    /// <summary>
    ///   Number of quick saves to keep
    /// </summary>
    [JsonProperty]
    public SettingValue<int> MaxQuickSaves { get; private set; } = new(5);

    /// <summary>
    ///   Saves the current settings by writing them to the settings configuration file.
    ///   Show tutorial messages
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> TutorialsEnabled { get; private set; } = new(true);

    /// <summary>
    ///   When true cheats are enabled
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> CheatsEnabled { get; private set; } = new(false);

    /// <summary>
    ///   Enables online news feed
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> ThriveNewsFeedEnabled { get; private set; } = new(true);

    /// <summary>
    ///   Enables showing new patch notes when the game is updated
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> ShowNewPatchNotes { get; private set; } = new(true);

    /// <summary>
    ///   If false username will be set to System username
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> CustomUsernameEnabled { get; private set; } = new(false);

    /// <summary>
    ///   Username that the user can choose
    /// </summary>
    [JsonProperty]
    public SettingValue<string?> CustomUsername { get; private set; } = new(null);

    /// <summary>
    ///   List of notices that have been permanently dismissed (well at least until manually reset)
    /// </summary>
    [JsonProperty]
    public SettingValue<IReadOnlyCollection<DismissibleNotice>> PermanentlyDismissedNotices { get; private set; } =
        new(new HashSet<DismissibleNotice>());

    /// <summary>
    ///   The Db value to be added to the master audio bus
    /// </summary>
    [JsonProperty]
    public SettingValue<JSONDebug.DebugMode> JSONDebugMode { get; private set; } =
        new(JSONDebug.DebugMode.Automatic);

    /// <summary>
    ///   The screen effect currently being used
    /// </summary>
    [JsonProperty]
    public SettingValue<ScreenEffect?> CurrentScreenEffect { get; private set; } = new(null);

    /// <summary>
    ///   Enables/disables the unsaved progress warning popup for when the player tries to quit the game.
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> ShowUnsavedProgressWarning { get; private set; } = new(true);

    [JsonProperty]
    public SettingValue<bool> MoveOrganellesWithSymmetry { get; private set; } = new(false);

    // Input properties

    /// <summary>
    ///   The current controls of the game.
    ///   It stores the godot actions like g_move_left and
    ///   their associated <see cref="SpecifiedInputKey">SpecifiedInputKey</see>
    /// </summary>
    [JsonProperty]
    public SettingValue<InputDataList> CurrentControls { get; private set; } =
        new(GetDefaultControls());

    /// <summary>
    ///   How sensitive mouse looking is in the vertical direction. As mouse movement is the number of pixels moved
    ///   this multiplier needs to be very low compared to the controller multiplier.
    /// </summary>
    [JsonProperty]
    public SettingValue<float> VerticalMouseLookSensitivity { get; private set; } = new(0.003f);

    /// <summary>
    ///   How sensitive mouse looking is in the horizontal direction
    /// </summary>
    [JsonProperty]
    public SettingValue<float> HorizontalMouseLookSensitivity { get; private set; } = new(0.003f);

    /// <summary>
    ///   If true inverts the vertical axis inputs for mouse
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> InvertVerticalMouseLook { get; private set; } = new(false);

    /// <summary>
    ///   If true inverts the horizontal axis inputs for mouse
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> InvertHorizontalMouseLook { get; private set; } = new(false);

    /// <summary>
    ///   When true, mouse inputs going through <see cref="InputManager"/> are scaled by the current window size
    /// </summary>
    [JsonProperty]
    public SettingValue<MouseInputScaling> ScaleMouseInputByWindowSize { get; private set; } =
        new(MouseInputScaling.ScaleReverse);

    /// <summary>
    ///   Modifies behaviour of <see cref="ScaleMouseInputByWindowSize"/> to either use the logical Godot window size
    ///   (<see cref="LoadingScreen.LogicalDrawingAreaSize"/>) or the actual operating system window size
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> InputWindowSizeIsLogicalSize { get; private set; } = new(false);

    /// <summary>
    ///   How sensitive controller looking is in the vertical direction
    /// </summary>
    [JsonProperty]
    public SettingValue<float> VerticalControllerLookSensitivity { get; private set; } = new(1);

    /// <summary>
    ///   How sensitive controller looking is in the horizontal direction
    /// </summary>
    [JsonProperty]
    public SettingValue<float> HorizontalControllerLookSensitivity { get; private set; } = new(1.4f);

    /// <summary>
    ///   If true inverts the vertical axis inputs for controller
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> InvertVerticalControllerLook { get; private set; } = new(false);

    /// <summary>
    ///   If true inverts the horizontal axis inputs for controller
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> InvertHorizontalControllerLook { get; private set; } = new(false);

    /// <summary>
    ///   Sets how left/right inputs are interpreted in 2D (for example the microbe stage)
    /// </summary>
    [JsonProperty]
    public SettingValue<TwoDimensionalMovementMode> TwoDimensionalMovement { get; private set; } =
        new(TwoDimensionalMovementMode.Automatic);

    /// <summary>
    ///   Sets how movement direction inputs are interpreted for 3D movement
    /// </summary>
    [JsonProperty]
    public SettingValue<ThreeDimensionalMovementMode> ThreeDimensionalMovement { get; private set; } =
        new(ThreeDimensionalMovementMode.ScreenRelative);

    // TODO: control in options
    /// <summary>
    ///   If true putting the mouse to a screen edge pans the strategy view
    /// </summary>
    [JsonProperty]
    public SettingValue<bool> PanStrategyViewWithMouse { get; private set; } = new(true);

    /// <summary>
    ///   Speed of the mouse edge panning
    /// </summary>
    [JsonProperty]
    public SettingValue<float> PanStrategyViewMouseSpeed { get; private set; } = new(30);

    /// <summary>
    ///   How big the deadzones are for controller axes
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This should have <see cref="JoystickList.AxisMax"/> values in here to have one for each supported axis.
    ///   </para>
    /// </remarks>
    [JsonProperty]
    public SettingValue<IReadOnlyList<float>> ControllerAxisDeadzoneAxes { get; private set; } = new(new[]
    {
        Constants.CONTROLLER_DEFAULT_DEADZONE,
        Constants.CONTROLLER_DEFAULT_DEADZONE,
        Constants.CONTROLLER_DEFAULT_DEADZONE,
        Constants.CONTROLLER_DEFAULT_DEADZONE,
        Constants.CONTROLLER_DEFAULT_DEADZONE,
        Constants.CONTROLLER_DEFAULT_DEADZONE,
        Constants.CONTROLLER_DEFAULT_DEADZONE,
        Constants.CONTROLLER_DEFAULT_DEADZONE,
        Constants.CONTROLLER_DEFAULT_DEADZONE,
        Constants.CONTROLLER_DEFAULT_DEADZONE,
    });

    // Settings that are edited from elsewhere than the main options menu
    [JsonProperty]
    public SettingValue<IReadOnlyList<string>> EnabledMods { get; private set; } = new(new List<string>());

    // Computed properties from other settings

    [JsonIgnore]
    public string ActiveUsername =>
        CustomUsernameEnabled &&
        CustomUsername.Value != null ?
            CustomUsername.Value :
            EnvironmentUserName;

    public int CloudSimulationWidth => Constants.CLOUD_X_EXTENT / CloudResolution;

    public int CloudSimulationHeight => Constants.CLOUD_Y_EXTENT / CloudResolution;

    public static bool operator ==(Settings? lhs, Settings? rhs)
    {
        return Equals(lhs, rhs);
    }

    public static bool operator !=(Settings lhs, Settings rhs)
    {
        return !(lhs == rhs);
    }

    /// <summary>
    ///   Returns the default controls which never change, unless there is a new release.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This relies on the static member holding the default controls to be initialized before the code has a chance
    ///     to modify the controls.
    ///   </para>
    /// </remarks>
    /// <returns>The default controls</returns>
    public static InputDataList GetDefaultControls()
    {
        return (InputDataList)DefaultControls.Clone();
    }

    /// <summary>
    ///   Returns the currently applied controls. Gathers the data from the godot InputMap.
    ///   Required to get the default controls.
    /// </summary>
    /// <returns>The current inputs</returns>
    public static InputDataList GetCurrentlyAppliedControls()
    {
        return new InputDataList(InputMap.GetActions().OfType<string>()
            .ToDictionary(p => p,
                p => InputMap.GetActionList(p).OfType<InputEvent>().Select(x => new SpecifiedInputKey(x)).ToList())!);
    }

    /// <summary>
    ///   Tries to return a C# culture info from Godot language name
    /// </summary>
    /// <param name="language">The language name to try to understand</param>
    /// <returns>The culture info</returns>
    public static CultureInfo GetCultureInfo(string language)
    {
        // Perform hard coded translations first
        var translated = TranslateLocaleToCSharp(language);
        if (translated != null)
            language = translated;

        try
        {
            return new CultureInfo(language);
        }
        catch (CultureNotFoundException)
        {
            // Some locales might have "_extra" at the end that C# doesn't understand, because it uses a dash

            if (!language.Contains("_"))
                throw;

            // So we first try converting "_" to "-" and go with that
            language = language.Replace('_', '-');

            try
            {
                return new CultureInfo(language);
            }
            catch (CultureNotFoundException)
            {
                language = language.Split("-")[0];

                GD.Print("Failed to get CultureInfo with whole language name, tried stripping extra, new: ",
                    language);
                return new CultureInfo(language);
            }
        }
    }

    /// <summary>
    ///   Translates a Godot locale to C# locale name
    /// </summary>
    /// <param name="godotLocale">Godot locale</param>
    /// <returns>C# locale name, or null if there is not a premade mapping</returns>
    public static string? TranslateLocaleToCSharp(string godotLocale)
    {
        // ReSharper disable StringLiteralTypo
        switch (godotLocale)
        {
            case "eo":
                return "en";
            case "sr_Latn":
                return "sr-Latn-RS";
            case "sr_Cyrl":
                return "sr-Cyrl-RS";
        }

        // ReSharper restore StringLiteralTypo
        return null;
    }

    /// <summary>
    ///   Overrides Native name if an override is set
    /// </summary>
    /// <param name="godotLocale">Godot locale</param>
    /// <returns>Native name, or null if there is not a premade mapping</returns>
    public static string? GetLanguageNativeNameOverride(string godotLocale)
    {
        switch (godotLocale)
        {
            case "eo":
                return "Esperanto";
        }

        return null;
    }

    public bool IsNoticePermanentlyDismissed(DismissibleNotice notice)
    {
        return PermanentlyDismissedNotices.Value.Contains(notice);
    }

    public bool PermanentlyDismissNotice(DismissibleNotice notice)
    {
        if (PermanentlyDismissedNotices.Value.Contains(notice))
            return false;

        PermanentlyDismissedNotices.Value = PermanentlyDismissedNotices.Value.Append(notice).ToHashSet();

        // We need to save the fact that a new notice is now permanently dismissed in case the player doesn't touch
        // any settings (which would warrant saving the settings elsewhere)
        if (!Save())
            GD.PrintErr("Failed to save settings to mark notice as permanently dismissed: ", notice);

        return true;
    }

    /// <summary>
    ///   Loads values from an existing settings object.
    /// </summary>
    public void LoadFromObject(Settings settings)
    {
        CopySettings(settings);
    }

    /// <summary>
    ///   Loads default values.
    /// </summary>
    public void LoadDefaults()
    {
        Settings settings = new Settings();
        CopySettings(settings);
    }

    /// <summary>
    ///   Saves the current settings by writing them to the settings file
    /// </summary>
    /// <returns>True on success, false if the file can't be written.</returns>
    public bool Save()
    {
        using var file = new File();
        var error = file.Open(Constants.CONFIGURATION_FILE, File.ModeFlags.Write);

        if (error != Error.Ok)
        {
            GD.PrintErr("Couldn't open settings file for writing.");
            return false;
        }

        file.StoreString(JsonConvert.SerializeObject(this));

        file.Close();

        return true;
    }

    /// <summary>
    ///   Applies all current settings to any applicable engine systems.
    /// </summary>
    /// <param name="delayedApply">
    ///   If true things that can't be immediately on game startup be applied, are applied later
    /// </param>
    public void ApplyAll(bool delayedApply = false)
    {
        if (Engine.EditorHint)
        {
            // Do not apply settings within the Godot editor.
            return;
        }

        // Delayed apply was implemented to fix problems within the Godot editor.
        // So this might no longer be necessary, as this is now skipped within editor.
        if (delayedApply)
        {
            GD.Print("Doing delayed apply for some settings");
            Invoke.Instance.Perform(ApplyGraphicsSettings);

            // These need to be also delay applied, otherwise when debugging these overwrite the default settings
            Invoke.Instance.Queue(ApplySoundSettings);

            // If this is not delay applied, this also causes some errors in godot editor output when running
            Invoke.Instance.Queue(ApplyInputSettings);
        }
        else
        {
            ApplyGraphicsSettings();
            ApplySoundSettings();
            ApplyInputSettings();
        }

        ApplyAudioOutputDeviceSettings();
        ApplyLanguageSettings();
        ApplyWindowSettings();
    }

    /// <summary>
    ///   Applies current graphics related settings.
    /// </summary>
    public void ApplyGraphicsSettings()
    {
        GUICommon.Instance.GetTree().Root.GetViewport().Msaa = MSAAResolution;

        // Values less than 0 are undefined behaviour
        Engine.TargetFps = MaxFramesPerSecond >= 0 ? MaxFramesPerSecond : 0;
        ColourblindScreenFilter.Instance.SetColourblindSetting(ColourblindSetting);
    }

    /// <summary>
    ///   Applies current sound settings to any applicable engine systems.
    /// </summary>
    public void ApplySoundSettings()
    {
        var master = AudioServer.GetBusIndex("Master");

        AudioServer.SetBusVolumeDb(master, VolumeMaster);
        AudioServer.SetBusMute(master, VolumeMasterMuted);

        var music = AudioServer.GetBusIndex("Music");

        AudioServer.SetBusVolumeDb(music, VolumeMusic);
        AudioServer.SetBusMute(music, VolumeMusicMuted);

        var ambiance = AudioServer.GetBusIndex("Ambiance");

        AudioServer.SetBusVolumeDb(ambiance, VolumeAmbiance);
        AudioServer.SetBusMute(ambiance, VolumeAmbianceMuted);

        var sfx = AudioServer.GetBusIndex("SFX");

        AudioServer.SetBusVolumeDb(sfx, VolumeSFX);
        AudioServer.SetBusMute(sfx, VolumeSFXMuted);

        var gui = AudioServer.GetBusIndex("GUI");

        AudioServer.SetBusVolumeDb(gui, VolumeGUI);
        AudioServer.SetBusMute(gui, VolumeGUIMuted);
    }

    /// <summary>
    ///   Applies the current controls to the InputMap.
    /// </summary>
    public void ApplyInputSettings()
    {
        CurrentControls.Value.ApplyToGodotInputMap();
    }

    /// <summary>
    ///   Applies current window settings to any applicable engine systems.
    /// </summary>
    public void ApplyWindowSettings()
    {
        OS.WindowFullscreen = FullScreen;
        OS.VsyncEnabled = VSync;
    }

    /// <summary>
    ///   Applies current output device settings to the audio system
    /// </summary>
    public void ApplyAudioOutputDeviceSettings()
    {
        var audioOutputDevice = SelectedAudioOutputDevice.Value;
        if (string.IsNullOrEmpty(audioOutputDevice))
        {
            audioOutputDevice = Constants.DEFAULT_AUDIO_OUTPUT_DEVICE_NAME;
        }

        // If the selected output device is invalid Godot resets AudioServer.Device to Default.
        // It seems like there is some kind of threading going on. The getter of AudioServer.Device
        // only returns the new value after some time, therefore we can't check if the output device
        // got applied successfully.
        AudioServer.Device = audioOutputDevice;

        GD.Print("Set audio output device to: ", audioOutputDevice);
    }

    /// <summary>
    ///   Applies thread count settings, not necessary to call on startup as TaskExecutor reads the values itself from
    ///   us when starting
    /// </summary>
    public void ApplyThreadSettings()
    {
        TaskExecutor.Instance.ReApplyThreadCount();
    }

    /// <summary>
    ///   Applies current language settings to any applicable engine systems.
    /// </summary>
    public void ApplyLanguageSettings()
    {
        string? language = SelectedLanguage.Value;
        CultureInfo cultureInfo;

        // Process locale info in case it isn't exactly right
        if (string.IsNullOrEmpty(language))
        {
            language = DefaultLanguage;
            cultureInfo = DefaultCulture;
        }
        else
        {
            language = GetSupportedLocale(language!);
            cultureInfo = GetCultureInfo(language);
        }

        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        // Set locale for the game. Called after C# locale change so that string
        // formatting uses could also get updated properly.
        TranslationServer.SetLocale(language);

        GD.Print("Set C# locale to: ", cultureInfo, " Godot locale is: ", TranslationServer.GetLocale());
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (GetType() != obj.GetType())
        {
            return false;
        }

        return Equals((Settings)obj);
    }

    public bool Equals(Settings obj)
    {
        // Compare all properties in the two objects for equality.
        var type = GetType();

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            // Returns if any of the properties don't match.
            object thisValue = property.GetValue(this);
            object objValue = property.GetValue(obj);

            if (thisValue != objValue && thisValue?.Equals(objValue) != true)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///   Returns a cloned deep copy of the settings object.
    /// </summary>
    public Settings Clone()
    {
        Settings settings = new Settings();
        settings.CopySettings(this);

        return settings;
    }

    public override int GetHashCode()
    {
        int hashCode = 17;

        var type = GetType();

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            hashCode ^= property.GetHashCode();

        return hashCode;
    }

    /// <summary>
    ///   Loads, initializes and returns the global settings object.
    /// </summary>
    private static Settings InitializeGlobalSettings()
    {
        try
        {
            Settings? settings = LoadSettings();

            if (settings == null)
            {
                GD.PrintErr("Loading settings from file failed, using default settings");
                settings = new Settings();
            }

            if (!SceneManager.QuitOrQuitting)
            {
                settings.ApplyAll(true);
            }
            else
            {
                GD.PrintErr("Skipping settings apply as the game should close soon");
            }

            return settings;
        }
        catch (Exception e)
        {
            // Godot doesn't seem to catch this nicely so we print the errors ourselves
            GD.PrintErr("Error initializing global settings: ", e);
            throw;
        }
    }

    /// <summary>
    ///   Creates and returns a settings object loaded from the configuration settings file, or defaults if that fails.
    /// </summary>
    private static Settings? LoadSettings()
    {
        using var file = new File();
        var error = file.Open(Constants.CONFIGURATION_FILE, File.ModeFlags.Read);

        if (error != Error.Ok)
        {
            GD.Print("Failed to open settings configuration file, file is missing or unreadable. "
                + "Using default settings instead.");

            var settings = new Settings();
            settings.Save();

            return settings;
        }

        var text = file.GetAsText();

        file.Close();

        try
        {
            var settings = JsonConvert.DeserializeObject<Settings>(text);

            if (settings == null)
                return settings;

            EnsureLoadedSettingsAreValid(settings);
            return settings;
        }
        catch
        {
            GD.Print("Failed to deserialize settings file data, data may be improperly formatted. "
                + "Using default settings instead.");

            var settings = new Settings();
            settings.Save();

            return settings;
        }
    }

    private static void EnsureLoadedSettingsAreValid(Settings settings)
    {
        if (settings.MaxSpawnedEntities.Value < Constants.TINY_MAX_SPAWNED_ENTITIES)
        {
            GD.PrintErr($"{nameof(MaxSpawnedEntities)} is below the minimum value, resetting to normal");
            settings.MaxSpawnedEntities.Value = Constants.NORMAL_MAX_SPAWNED_ENTITIES;
        }
    }

    /// <summary>
    ///   Tries to return the best supported Godot locale match.
    ///   Godot locale is different from C# culture.
    ///   Compare for example fi_FI (Godot) to fi-FI (C#).
    /// </summary>
    /// <param name="locale">locale to check</param>
    /// <returns>supported locale</returns>
    private static string GetSupportedLocale(string locale)
    {
        if (AvailableLocales.Contains(locale))
        {
            return locale;
        }

        if (locale.Contains('_'))
        {
            locale = locale.Split("_")[0];
            if (AvailableLocales.Contains(locale))
            {
                return locale;
            }
        }

        return "en";
    }

    /// <summary>
    ///   Debug helper for dumping what C# considers valid locales
    /// </summary>
    private static void DumpValidCSharpLocales()
    {
        GD.Print("Locales (C#):");

        foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures))
        {
            GD.Print(culture.DisplayName + " - " + culture.Name);
        }

        GD.Print(string.Empty);
    }

    /// <summary>
    ///   Copies all properties from another settings object to the current one.
    /// </summary>
    private void CopySettings(Settings settings)
    {
        var type = GetType();

        foreach (var property in type.GetProperties(
                     BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (!property.CanWrite)
                continue;

            // Since the properties we want to copy are SettingValue generics we use the IAssignableSetting
            // interface and AssignFrom method to convert the property to the correct concrete class.
            var setting = (IAssignableSetting)property.GetValue(this);

            setting.AssignFrom(property.GetValue(settings));
        }
    }
}
