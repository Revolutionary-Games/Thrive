using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

    // Graphics Properties

    /// <summary>
    ///   Sets whether the game window is in fullscreen mode
    /// </summary>
    public SettingValue<bool> FullScreen { get; set; } = new SettingValue<bool>(true);

    /// <summary>
    ///   Sets whether the game window will use vsync
    /// </summary>
    public SettingValue<bool> VSync { get; set; } = new SettingValue<bool>(true);

    /// <summary>
    ///   Sets amount of MSAA to apply to the viewport
    /// </summary>
    public SettingValue<Viewport.MSAA> MSAAResolution { get; set; } =
        new SettingValue<Viewport.MSAA>(Viewport.MSAA.Msaa2x);

    /// <summary>
    ///   Optionally applies a colour filter to the screen to aid colourblind individuals
    ///   0 = None, 1 = Red/Green, 2 = Blue/Yellow
    /// </summary>
    public SettingValue<int> ColourblindSetting { get; set; } = new SettingValue<int>(0);

    /// <summary>
    ///   The amount of Chromatic Aberration to apply to the screen
    /// </summary>
    public SettingValue<float> ChromaticAmount { get; set; } = new SettingValue<float>(15.0f);

    /// <summary>
    ///   Enable or Disable Chromatic Aberration for screen
    /// </summary>
    public SettingValue<bool> ChromaticEnabled { get; set; } = new SettingValue<bool>(true);

    /// <summary>
    ///   Display or hide the abilities hotbar in the microbe stage HUD.
    /// </summary>
    public SettingValue<bool> DisplayAbilitiesHotBar { get; set; } = new SettingValue<bool>(true);

    /// <summary>
    ///   Enable or disable lighting effects on the GUI. Mainly Used to workaround a bug where the HUD area
    ///   surrounding the editor button sometimes disappearing with the light effect turned on.
    /// </summary>
    public SettingValue<bool> GUILightEffectsEnabled { get; set; } = new SettingValue<bool>(true);

    // Sound Properties

    /// <summary>
    ///   The Db value to be added to the master audio bus
    /// </summary>
    public SettingValue<float> VolumeMaster { get; set; } = new SettingValue<float>(0.0f);

    /// <summary>
    ///   If true all sounds are muted
    /// </summary>
    public SettingValue<bool> VolumeMasterMuted { get; set; } = new SettingValue<bool>(false);

    /// <summary>
    ///   The Db value to be added to the music audio bus
    /// </summary>
    public SettingValue<float> VolumeMusic { get; set; } = new SettingValue<float>(0.0f);

    /// <summary>
    ///   If true music is muted
    /// </summary>
    public SettingValue<bool> VolumeMusicMuted { get; set; } = new SettingValue<bool>(false);

    /// <summary>
    ///   The Db value to be added to the ambiance audio bus
    /// </summary>
    public SettingValue<float> VolumeAmbiance { get; set; } = new SettingValue<float>(0.0f);

    /// <summary>
    ///   If true ambiance is muted
    /// </summary>
    public SettingValue<bool> VolumeAmbianceMuted { get; set; } = new SettingValue<bool>(false);

    /// <summary>
    ///   The Db value to be added to the sfx audio bus
    /// </summary>
    public SettingValue<float> VolumeSFX { get; set; } = new SettingValue<float>(0.0f);

    /// <summary>
    ///   If true sfx is muted
    /// </summary>
    public SettingValue<bool> VolumeSFXMuted { get; set; } = new SettingValue<bool>(false);

    /// <summary>
    ///   The Db value to be added to the gui audio bus
    /// </summary>
    public SettingValue<float> VolumeGUI { get; set; } = new SettingValue<float>(0.0f);

    /// <summary>
    ///   If true gui audio bus is muted
    /// </summary>
    public SettingValue<bool> VolumeGUIMuted { get; set; } = new SettingValue<bool>(false);

    public SettingValue<string> SelectedAudioOutputDevice { get; set; } =
        new SettingValue<string>(Constants.DEFAULT_AUDIO_OUTPUT_DEVICE_NAME);

    public SettingValue<string> SelectedLanguage { get; set; } = new SettingValue<string>(null);

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
    public SettingValue<float> CloudUpdateInterval { get; set; } = new SettingValue<float>(0.040f);

    /// <summary>
    ///   This can be freely adjusted to adjust the performance The
    ///   higher this value is the smaller the size of the simulated
    ///   cloud is and the performance is better.
    /// </summary>
    public SettingValue<int> CloudResolution { get; set; } = new SettingValue<int>(2);

    /// <summary>
    ///   If true an auto-evo run is started during gameplay,
    ///   taking up one of the background threads.
    /// </summary>
    public SettingValue<bool> RunAutoEvoDuringGamePlay { get; set; } = new SettingValue<bool>(true);

    /// <summary>
    ///   If true it is assumed that the CPU has hyperthreading, meaning that real cores is CPU count / 2
    /// </summary>
    public SettingValue<bool> AssumeCPUHasHyperthreading { get; set; } = new SettingValue<bool>(true);

    /// <summary>
    ///   Only if this is true the ThreadCount will be followed
    /// </summary>
    public SettingValue<bool> UseManualThreadCount { get; set; } = new SettingValue<bool>(false);

    /// <summary>
    ///   Manually set number of background threads to use. Needs to be at least 2 if RunAutoEvoDuringGamePlay is true
    /// </summary>
    public SettingValue<int> ThreadCount { get; set; } = new SettingValue<int>(4);

    // Misc Properties

    /// <summary>
    ///   When true the main intro is played
    /// </summary>
    public SettingValue<bool> PlayIntroVideo { get; set; } = new SettingValue<bool>(true);

    /// <summary>
    ///   When true the microbe intro is played on new game
    /// </summary>
    public SettingValue<bool> PlayMicrobeIntroVideo { get; set; } = new SettingValue<bool>(true);

    /// <summary>
    ///   If false auto saving will be disabled
    /// </summary>
    public SettingValue<bool> AutoSaveEnabled { get; set; } = new SettingValue<bool>(true);

    /// <summary>
    ///   Number of auto saves to keep
    /// </summary>
    public SettingValue<int> MaxAutoSaves { get; set; } = new SettingValue<int>(5);

    /// <summary>
    ///   Number of quick saves to keep
    /// </summary>
    public SettingValue<int> MaxQuickSaves { get; set; } = new SettingValue<int>(5);

    /// <summary>
    ///   Saves the current settings by writing them to the settings configuration file.
    ///   Show tutorial messages
    /// </summary>
    public SettingValue<bool> TutorialsEnabled { get; set; } = new SettingValue<bool>(true);

    /// <summary>
    ///   When true cheats are enabled
    /// </summary>
    public SettingValue<bool> CheatsEnabled { get; set; } = new SettingValue<bool>(false);

    /// <summary>
    ///   If false username will be set to System username
    /// </summary>
    public SettingValue<bool> CustomUsernameEnabled { get; set; } = new SettingValue<bool>(false);

    /// <summary>
    ///   Username that the user can choose
    /// </summary>
    public SettingValue<string> CustomUsername { get; set; } = new SettingValue<string>(null);

    /// <summary>
    ///   The Db value to be added to the master audio bus
    /// </summary>
    public SettingValue<JSONDebug.DebugMode> JSONDebugMode { get; set; } =
        new SettingValue<JSONDebug.DebugMode>(JSONDebug.DebugMode.Automatic);

    // Input properties

    /// <summary>
    ///   The current controls of the game.
    ///   It stores the godot actions like g_move_left and
    ///   their associated <see cref="SpecifiedInputKey">SpecifiedInputKey</see>
    /// </summary>
    public SettingValue<InputDataList> CurrentControls { get; set; } =
        new SettingValue<InputDataList>(GetDefaultControls());

    // Computed properties from other settings

    [JsonIgnore]
    public string ActiveUsername =>
        CustomUsernameEnabled &&
        CustomUsername.Value != null ?
            CustomUsername.Value :
            Environment.UserName;

    public int CloudSimulationWidth => Constants.CLOUD_X_EXTENT / CloudResolution;

    public int CloudSimulationHeight => Constants.CLOUD_Y_EXTENT / CloudResolution;

    public static bool operator ==(Settings lhs, Settings rhs)
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
                p => InputMap.GetActionList(p).OfType<InputEventWithModifiers>().Select(
                    x => new SpecifiedInputKey(x)).ToList()));
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
    public static string TranslateLocaleToCSharp(string godotLocale)
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
    public static string GetLanguageNativeNameOverride(string godotLocale)
    {
        switch (godotLocale)
        {
            case "eo":
                return "Esperanto";
        }

        return null;
    }

    public override bool Equals(object obj)
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

        foreach (var property in type.GetProperties())
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

    public override int GetHashCode()
    {
        int hashCode = 17;

        var type = GetType();

        foreach (var property in type.GetProperties())
            hashCode ^= property.GetHashCode();

        return hashCode;
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
        string language = SelectedLanguage.Value;
        CultureInfo cultureInfo;

        // Process locale info in case it isn't exactly right
        if (string.IsNullOrEmpty(language))
        {
            language = DefaultLanguage;
            cultureInfo = DefaultCulture;
        }
        else
        {
            language = GetSupportedLocale(language);
            cultureInfo = GetCultureInfo(language);
        }

        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        // Set locale for the game. Called after C# locale change so that string
        // formatting uses could also get updated properly.
        TranslationServer.SetLocale(language);

        GD.Print("Set C# locale to: ", cultureInfo, " Godot locale is: ", TranslationServer.GetLocale());
    }

    /// <summary>
    ///   Loads, initializes and returns the global settings object.
    /// </summary>
    private static Settings InitializeGlobalSettings()
    {
        try
        {
            Settings settings = LoadSettings();

            if (settings == null)
            {
                GD.PrintErr("Loading settings from file failed, using default settings");
                settings = new Settings();
            }

            settings.ApplyAll(true);

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
    private static Settings LoadSettings()
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
            return JsonConvert.DeserializeObject<Settings>(text);
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

    /// <summary>
    ///   Tries to return the best supported Godot locale match.
    ///   Godot locale is different from C# culture.
    ///   e.g. fi_FI (Godot) to fi-FI (C#).
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

        foreach (var property in type.GetProperties())
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
