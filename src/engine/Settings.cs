using System.Globalization;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Class that handles storing and applying player changeable game settings.
/// </summary>
public class Settings
{
    /// <summary>
    ///   Singleton used for holding the live copy of game settings.
    /// </summary>
    private static readonly Settings SingletonInstance = InitializeGlobalSettings();

    static Settings()
    {
    }

    private Settings()
    {
    }

    public static Settings Instance => SingletonInstance;

    // Graphics Properties

    /// <summary>
    ///   Sets whether the game window is in fullscreen mode
    /// </summary>
    public bool FullScreen { get; set; } = true;

    /// <summary>
    ///   Sets whether the game window will use vsync
    /// </summary>
    public bool VSync { get; set; } = true;

    /// <summary>
    ///   Sets amount of MSAA to apply to the viewport
    /// </summary>
    public Viewport.MSAA MSAAResolution { get; set; } = Viewport.MSAA.Disabled;

    /// <summary>
    ///   Optionally applies a colour filter to the screen to aid colourblind individuals
    ///   0 = None, 1 = Red/Green, 2 = Blue/Yellow
    /// </summary>
    public int ColourblindSetting { get; set; } = 0;

    /// <summary>
    ///   The amount of Chromatic Aberration to apply to the screen
    /// </summary>
    public float ChromaticAmount { get; set; } = 20.0f;

    /// <summary>
    ///   Enable or Disable Chromatic Aberration for screen
    /// </summary>
    public bool ChromaticEnabled { get; set; } = true;

    // Sound Properties

    /// <summary>
    ///   The Db value to be added to the master audio bus
    /// </summary>
    public float VolumeMaster { get; set; } = 0.0f;

    /// <summary>
    ///   If true all sounds are muted
    /// </summary>
    public bool VolumeMasterMuted { get; set; } = false;

    /// <summary>
    ///   The Db value to be added to the music audio bus
    /// </summary>
    public float VolumeMusic { get; set; } = 0.0f;

    /// <summary>
    ///   If true music is muted
    /// </summary>
    public bool VolumeMusicMuted { get; set; } = false;

    /// <summary>
    ///   The Db value to be added to the ambiance audio bus
    /// </summary>
    public float VolumeAmbiance { get; set; } = 0.0f;

    /// <summary>
    ///   If true ambiance is muted
    /// </summary>
    public bool VolumeAmbianceMuted { get; set; } = false;

    /// <summary>
    ///   The Db value to be added to the sfx audio bus
    /// </summary>
    public float VolumeSFX { get; set; } = 0.0f;

    /// <summary>
    ///   If true sfx is muted
    /// </summary>
    public bool VolumeSFXMuted { get; set; } = false;

    /// <summary>
    ///   The Db value to be added to the gui audio bus
    /// </summary>
    public float VolumeGUI { get; set; } = 0.0f;

    /// <summary>
    ///   If true gui audio bus is muted
    /// </summary>
    public bool VolumeGUIMuted { get; set; } = false;

    // Performance Properties

    /// <summary>
    ///   If this is over 0 then this limits how often compound clouds
    ///   are updated. The default value of 0.020 at 60 FPS makes
    ///   every other frame not update the clouds.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This should be made user configurable for different
    ///     computers. The choises should probably be:
    ///     0.0f, 0.020f, 0.040f, 0.1f, 0.25f
    ///   </para>
    /// </remarks>
    public float CloudUpdateInterval { get; set; } = 0.040f;

    /// <summary>
    ///   This can be freely adjusted to adjust the performance The
    ///   higher this value is the smaller the size of the simulated
    ///   cloud is and the performance is better.
    /// </summary>
    public int CloudResolution { get; set; } = 2;

    /// <summary>
    ///   If true an auto-evo run is started during gameplay,
    ///   taking up one of the background threads.
    /// </summary>
    public bool RunAutoEvoDuringGamePlay { get; set; } = true;

    // Misc Properties

    /// <summary>
    ///   When true the main intro is played
    /// </summary>
    public bool PlayIntroVideo { get; set; } = true;

    /// <summary>
    ///   When true the microbe intro is played on new game
    /// </summary>
    public bool PlayMicrobeIntroVideo { get; set; } = true;

    /// <summary>
    ///   If false auto saving will be disabled
    /// </summary>
    public bool AutoSaveEnabled { get; set; } = true;

    /// <summary>
    ///   Number of auto saves to keep
    /// </summary>
    public int MaxAutoSaves { get; set; } = 5;

    /// <summary>
    ///   Number of quick saves to keep
    /// </summary>
    public int MaxQuickSaves { get; set; } = 5;

    /// <summary>
    ///   When true cheats are enabled
    /// </summary>
    public bool CheatsEnabled { get; set; } = false;

    public int CloudSimulationWidth => Constants.CLOUD_X_EXTENT / CloudResolution;

    public int CloudSimulationHeight => Constants.CLOUD_Y_EXTENT / CloudResolution;

    public string SelectedLanguage { get; set; } = null;

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
    ///   Saves the current settings by writing them to the settings configuration file.
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
    public void ApplyAll()
    {
        ApplyGraphicsSettings();
        ApplySoundSettings();
        ApplyWindowSettings();
        ApplyLanguageSettings();
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
        TranslationServer.SetLocale(SelectedLanguage);

        // Set locale for the game.
        CultureInfo cultureInfo = new CultureInfo(SelectedLanguage);
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;
    }

    /// <summary>
    ///   Loads, initializes and returns the global settings object.
    /// </summary>
    private static Settings InitializeGlobalSettings()
    {
        Settings settings = LoadSettings();
        settings.ApplyAll();

        return settings;
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
                GD.Print("Settings configuration file is missing or unreadable, using default settings.");

                var settings = new Settings();
                settings.Save();

                return settings;
            }

            var text = file.GetAsText();

            file.Close();

            return JsonConvert.DeserializeObject<Settings>(text);
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

            property.SetValue(this, property.GetValue(settings));
        }
    }
}
