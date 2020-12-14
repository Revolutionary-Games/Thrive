using System;
using System.Globalization;
using Godot;
using Newtonsoft.Json;
using Environment = System.Environment;

/// <summary>
///   Class that handles storing and applying player changeable game settings.
/// </summary>
public class Settings
{
    private static readonly string DefaultLanguageValue = TranslationServer.GetLocale();
    private static readonly CultureInfo DefaultCultureValue = CultureInfo.CurrentCulture;

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
    ///   The current controls of the game.
    ///   It stores the godot actions like g_move_left and
    ///   their associated <see cref="SpecifiedInputKey">SpecifiedInputKey</see>
    /// </summary>
    public SettingValue<InputDataList> CurrentControls { get; set; } =
        new SettingValue<InputDataList>(InputGroupList.GetDefaultControls());

    /// <summary>
    ///   If false username will be set to System username
    /// </summary>
    public SettingValue<bool> CustomUsernameEnabled { get; set; } = new SettingValue<bool>(false);

    /// <summary>
    ///   Username that the user can choose
    /// </summary>
    public SettingValue<string> CustomUsername { get; set; } = new SettingValue<string>(null);

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

            if (thisValue != objValue && (thisValue == null || !thisValue.Equals(objValue)))
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
        using (var file = new File())
        {
            var error = file.Open(Constants.CONFIGURATION_FILE, File.ModeFlags.Write);

            if (error != Error.Ok)
            {
                GD.PrintErr("Couldn't open settings file for writing.");
                return false;
            }

            file.StoreString(JsonConvert.SerializeObject(this));

            file.Close();
        }

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
        if (delayedApply)
        {
            GD.Print("Doing delayed apply for some settings");
            Invoke.Instance.Perform(ApplyGraphicsSettings);

            // These need to be also delay applied, otherwise when debugging these overwrite the default settings
            Invoke.Instance.Queue(ApplySoundSettings);
        }
        else
        {
            ApplyGraphicsSettings();
            ApplySoundSettings();
        }

        ApplyLanguageSettings();
        ApplyWindowSettings();
        ApplyInputSettings();
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
            cultureInfo = GetCultureInfo(language);
        }

        // Set locale for the game.
        TranslationServer.SetLocale(language);

        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        SimulationParameters.Instance.ApplyTranslations();
    }

    /// <summary>
    ///   Tries to return a C# culture info from Godot language name
    /// </summary>
    /// <param name="language">The language name to try to understand</param>
    /// <returns>The culture info</returns>
    private static CultureInfo GetCultureInfo(string language)
    {
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
        using (var file = new File())
        {
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
