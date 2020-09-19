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
        // Initialize properties to default settings.

        // Graphics Properties
        FullScreen = new SettingValue<bool>(true);
        VSync = new SettingValue<bool>(true);
        MSAAResolution = new SettingValue<Viewport.MSAA>(Viewport.MSAA.Msaa2x);
        ColourblindSetting = new SettingValue<int>(0);
        ChromaticAmount = new SettingValue<float>(15.0f);
        ChromaticEnabled = new SettingValue<bool>(true);

        // Sound Properties
        VolumeMaster = new SettingValue<float>(0.0f);
        VolumeMasterMuted = new SettingValue<bool>(false);
        VolumeMusic = new SettingValue<float>(0.0f);
        VolumeMusicMuted = new SettingValue<bool>(false);
        VolumeAmbiance = new SettingValue<float>(0.0f);
        VolumeAmbianceMuted = new SettingValue<bool>(false);
        VolumeSFX = new SettingValue<float>(0.0f);
        VolumeSFXMuted = new SettingValue<bool>(false);
        VolumeGUI = new SettingValue<float>(0.0f);
        VolumeGUIMuted = new SettingValue<bool>(false);

        // Performance Properties
        CloudUpdateInterval = new SettingValue<float>(0.040f);
        CloudResolution = new SettingValue<int>(2);
        RunAutoEvoDuringGamePlay = new SettingValue<bool>(true);

        // Misc Properties
        PlayIntroVideo = new SettingValue<bool>(true);
        PlayMicrobeIntroVideo = new SettingValue<bool>(true);
        AutoSaveEnabled = new SettingValue<bool>(true);
        MaxAutoSaves = new SettingValue<int>(5);
        MaxQuickSaves = new SettingValue<int>(5);
        TutorialsEnabled = new SettingValue<bool>(true);
        CheatsEnabled = new SettingValue<bool>(false);
    }

    /// <summary>
    ///   Generic delegate used for alerting when a setting value was changed.
    /// </summary>
    public delegate void SettingValueChangedDelegate<TValueType>(TValueType value);

    /// <summary>
    ///   Interface used for the SettingValue class to allow casting of an object to the correct concrete class.
    /// </summary>
    public interface IAssignableSetting
    {
        void AssignFrom(object obj);
    }

    public static Settings Instance => SingletonInstance;

    // Graphics Properties

    /// <summary>
    ///   Sets whether the game window is in fullscreen mode
    /// </summary>
    public SettingValue<bool> FullScreen { get; set; }

    /// <summary>
    ///   Sets whether the game window will use vsync
    /// </summary>
    public SettingValue<bool> VSync { get; set; }

    /// <summary>
    ///   Sets amount of MSAA to apply to the viewport
    /// </summary>
    public SettingValue<Viewport.MSAA> MSAAResolution { get; set; }

    /// <summary>
    ///   Optionally applies a colour filter to the screen to aid colourblind individuals
    ///   0 = None, 1 = Red/Green, 2 = Blue/Yellow
    /// </summary>
    public SettingValue<int> ColourblindSetting { get; set; }

    /// <summary>
    ///   The amount of Chromatic Aberration to apply to the screen
    /// </summary>
    public SettingValue<float> ChromaticAmount { get; set; }

    /// <summary>
    ///   Enable or Disable Chromatic Aberration for screen
    /// </summary>
    public SettingValue<bool> ChromaticEnabled { get; set; }

    // Sound Properties

    /// <summary>
    ///   The Db value to be added to the master audio bus
    /// </summary>
    public SettingValue<float> VolumeMaster { get; set; }

    /// <summary>
    ///   If true all sounds are muted
    /// </summary>
    public SettingValue<bool> VolumeMasterMuted { get; set; }

    /// <summary>
    ///   The Db value to be added to the music audio bus
    /// </summary>
    public SettingValue<float> VolumeMusic { get; set; }

    /// <summary>
    ///   If true music is muted
    /// </summary>
    public SettingValue<bool> VolumeMusicMuted { get; set; }

    /// <summary>
    ///   The Db value to be added to the ambiance audio bus
    /// </summary>
    public SettingValue<float> VolumeAmbiance { get; set; }

    /// <summary>
    ///   If true ambiance is muted
    /// </summary>
    public SettingValue<bool> VolumeAmbianceMuted { get; set; }

    /// <summary>
    ///   The Db value to be added to the sfx audio bus
    /// </summary>
    public SettingValue<float> VolumeSFX { get; set; }

    /// <summary>
    ///   If true sfx is muted
    /// </summary>
    public SettingValue<bool> VolumeSFXMuted { get; set; }

    /// <summary>
    ///   The Db value to be added to the gui audio bus
    /// </summary>
    public SettingValue<float> VolumeGUI { get; set; }

    /// <summary>
    ///   If true gui audio bus is muted
    /// </summary>
    public SettingValue<bool> VolumeGUIMuted { get; set; }

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
    public SettingValue<float> CloudUpdateInterval { get; set; }

    /// <summary>
    ///   This can be freely adjusted to adjust the performance The
    ///   higher this value is the smaller the size of the simulated
    ///   cloud is and the performance is better.
    /// </summary>
    public SettingValue<int> CloudResolution { get; set; }

    /// <summary>
    ///   If true an auto-evo run is started during gameplay,
    ///   taking up one of the background threads.
    /// </summary>
    public SettingValue<bool> RunAutoEvoDuringGamePlay { get; set; }

    // Misc Properties

    /// <summary>
    ///   When true the main intro is played
    /// </summary>
    public SettingValue<bool> PlayIntroVideo { get; set; }

    /// <summary>
    ///   When true the microbe intro is played on new game
    /// </summary>
    public SettingValue<bool> PlayMicrobeIntroVideo { get; set; }

    /// <summary>
    ///   If false auto saving will be disabled
    /// </summary>
    public SettingValue<bool> AutoSaveEnabled { get; set; }

    /// <summary>
    ///   Number of auto saves to keep
    /// </summary>
    public SettingValue<int> MaxAutoSaves { get; set; }

    /// <summary>
    ///   Number of quick saves to keep
    /// </summary>
    public SettingValue<int> MaxQuickSaves { get; set; }

    /// <summary>
    ///   Saves the current settings by writing them to the settings configuration file.
    ///   Show tutorial messages
    /// </summary>
    public SettingValue<bool> TutorialsEnabled { get; set; }

    /// <summary>
    ///   When true cheats are enabled
    /// </summary>
    public SettingValue<bool> CheatsEnabled { get; set; }

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
    public void ApplyAll()
    {
        ApplyGraphicsSettings();
        ApplySoundSettings();
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
    ///   Applies current window settings to any applicable engine systems.
    /// </summary>
    public void ApplyWindowSettings()
    {
        OS.WindowFullscreen = FullScreen;
        OS.VsyncEnabled = VSync;
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
                GD.Print("Failed to load settings configuration file, using default settings.");

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
                GD.Print("Settings configuration file is unreadable, using default settings.");

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
            var setting = property.GetValue(this) as IAssignableSetting;

            setting?.AssignFrom(property.GetValue(settings));
        }
    }

    /// <summary>
    ///   Wrapper class for settings options containing the value and a delegate that provides a callback for
    ///   when the value is changed.
    /// </summary>
    public class SettingValue<TValueType> : IAssignableSetting
    {
        private TValueType value;

        public SettingValue(TValueType value)
        {
            this.value = value;
        }

        public event SettingValueChangedDelegate<TValueType> OnChanged;

        public TValueType Value
        {
            get => value;
            set
            {
                if (!Equals(this.value, value))
                {
                    this.value = value;

                    OnChanged?.Invoke(value);
                }
            }
        }

        public static implicit operator TValueType(SettingValue<TValueType> value)
        {
            return value.value;
        }

        public static bool operator ==(SettingValue<TValueType> lhs, SettingValue<TValueType> rhs)
        {
            return Equals(lhs, rhs);
        }

        public static bool operator !=(SettingValue<TValueType> lhs, SettingValue<TValueType> rhs)
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

            return Equals((SettingValue<TValueType>)obj);
        }

        public bool Equals(SettingValue<TValueType> obj)
        {
            if (!value.Equals(obj.value))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return 17 ^ value.GetHashCode();
        }

        /// <summary>
        ///   Casts a parameter object into a SettingValue generic of a matching type (if possible) and then
        ///   copies the value from it.
        /// </summary>
        public void AssignFrom(object obj)
        {
            // Convert the object to the correct concrete type if possible.
            SettingValue<TValueType> settingObject = obj as SettingValue<TValueType>;

            if (settingObject == null)
                return;

            // Matching types, so we copy the value from the other SettingValue.
            if (!value.Equals(settingObject.value))
            {
                value = settingObject.value;

                // Call any registered listeners through the delegate to inform them of the value change.
                OnChanged?.Invoke(value);
            }
        }
    }
}
