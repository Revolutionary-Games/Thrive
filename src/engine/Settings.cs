using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Singleton class that handles storing and applying player changeable game settings.
/// </summary>
public class Settings
{
    private static readonly Settings SingletonInstance = new Settings();

    private SettingsData settingsData;

    // Current active settings that should be applied when possible if they are changed.

    static Settings()
    {
        if (!SingletonInstance.Load())
        {
            // Loading failed so we'll load the new defaults and save to fix the configuration file.
            SingletonInstance.LoadDefaults();
            SingletonInstance.Save();
        }
    }

    private Settings()
    {
    }

    public static Settings Instance => SingletonInstance;

    /// <summary>
    ///   Accesses the internal SettingsData struct, automatically applies when set.
    /// </summary>
    public SettingsData Data
    {
        get => SingletonInstance.settingsData;
        set
        {
            SingletonInstance.settingsData = value;
            ApplyAll();
        }
    }

    // Graphics Properties

    /// <summary>
    ///   Sets whether the game window is in fullscreen mode
    /// </summary>
    public bool FullScreen
    {
        get => settingsData.FullScreen;
        set => settingsData.FullScreen = value;
    }

    /// <summary>
    ///   Sets whether the game window will use vsync
    /// </summary>
    public bool VSync
    {
        get => settingsData.Vsync;
        set => settingsData.Vsync = value;
    }

    /// <summary>
    ///   Sets amount of MSAA to apply to the viewport
    /// </summary>
    public Viewport.MSAA MSAAResolution
    {
        get => settingsData.MsaaResolution;
        set => settingsData.MsaaResolution = value;
    }

    /// <summary>
    ///   Optionally applies a colour filter to the screen to aid colourblind individuals
    ///   0 = None, 1 = Red/Green, 2 = Blue/Yellow
    /// </summary>
    public int ColourblindSetting
    {
        get => settingsData.ColourBlindSetting;
        set => settingsData.ColourBlindSetting = value;
    }

    /// <summary>
    ///   The amount of Chromatic Aberration to apply to the screen
    /// </summary>
    public float ChromaticAmount
    {
        get => settingsData.ChromaticAmount;
        set => settingsData.ChromaticAmount = value;
    }

    /// <summary>
    ///   Enable or Disable Chromatic Aberration for screen
    /// </summary>
    public bool ChromaticEnabled
    {
        get => settingsData.ChromaticEnabled;
        set => settingsData.ChromaticEnabled = value;
    }

    // Sound Properties

    /// <summary>
    ///   The Db value to be added to the master audio bus
    /// </summary>
    public float VolumeMaster
    {
        get => settingsData.VolumeMaster;
        set => settingsData.VolumeMaster = value;
    }

    /// <summary>
    ///   If true all sounds are muted
    /// </summary>
    public bool VolumeMasterMuted
    {
        get => settingsData.VolumeMasterMuted;
        set => settingsData.VolumeMasterMuted = value;
    }

    /// <summary>
    ///   The Db value to be added to the music audio bus
    /// </summary>
    public float VolumeMusic
    {
        get => settingsData.VolumeMusic;
        set => settingsData.VolumeMusic = value;
    }

    /// <summary>
    ///   If true music is muted
    /// </summary>
    public bool VolumeMusicMuted
    {
        get => settingsData.VolumeMusicMuted;
        set => settingsData.VolumeMusicMuted = value;
    }

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
    public float CloudUpdateInterval
    {
        get => settingsData.CloudUpdateInterval;
        set => settingsData.CloudUpdateInterval = value;
    }

    /// <summary>
    ///   This can be freely adjusted to adjust the performance The
    ///   higher this value is the smaller the size of the simulated
    ///   cloud is and the performance is better.
    /// </summary>
    public int CloudResolution
    {
        get => settingsData.CloudResolution;
        set => settingsData.CloudResolution = value;
    }

    // Misc Properties

    /// <summary>
    ///   When true the main intro is played
    /// </summary>
    public bool PlayIntroVideo
    {
        get => settingsData.PlayIntroVideo;
        set => settingsData.PlayIntroVideo = value;
    }

    /// <summary>
    ///   When true the microbe intro is played on new game
    /// </summary>
    public bool PlayMicrobeIntroVideo
    {
        get => settingsData.PlayMicrobeIntroVideo;
        set => settingsData.PlayMicrobeIntroVideo = value;
    }

    /// <summary>
    ///   If false auto saving will be disabled
    /// </summary>
    public bool AutoSaveEnabled
    {
        get => settingsData.AutoSaveEnabled;
        set => settingsData.AutoSaveEnabled = value;
    }

    /// <summary>
    ///   Number of auto saves to keep
    /// </summary>
    public int MaxAutoSaves
    {
        get => settingsData.MaxAutoSaves;
        set => settingsData.MaxAutoSaves = value;
    }

    /// <summary>
    ///   Number of quick saves to keep
    /// </summary>
    public int MaxQuickSaves
    {
        get => settingsData.MaxQuickSaves;
        set => settingsData.MaxQuickSaves = value;
    }

    /// <summary>
    ///   When true cheats are enabled
    /// </summary>
    public bool CheatsEnabled
    {
        get => settingsData.CheatsEnabled;
        set => settingsData.CheatsEnabled = value;
    }

    /// <summary>
    ///   If true an auto-evo run is started during gameplay,
    ///   taking up one of the background threads.
    /// </summary>
    public bool RunAutoEvoDuringGamePlay
    {
        get => settingsData.RunAutoEvoDuringGamePlay;
        set => settingsData.RunAutoEvoDuringGamePlay = value;
    }

    public int CloudSimulationWidth => Constants.CLOUD_X_EXTENT / CloudResolution;

    public int CloudSimulationHeight => Constants.CLOUD_Y_EXTENT / CloudResolution;

    /// <summary>
    ///   Loads and applies settings from the saved setting configuration file.
    /// </summary>
    /// <returns>True on success, false if the file is invalid or couldn't be opened for reading.</returns>
    public bool Load()
    {
        SettingsData loadedData;

        var file = new File();

        var error = file.Open(Constants.CONFIGURATION_FILE, File.ModeFlags.Read);

        if (error != Error.Ok)
        {
            GD.Print("Settings configuration file is missing or unreadable.");
            return false;
        }

        var text = file.GetAsText();

        file.Close();

        loadedData = JsonConvert.DeserializeObject<SettingsData>(text);

        // Sets local data to the loaded data and automatically applies it.
        Data = loadedData;
        GD.Print("Loaded and applied settings from configuration file.");
        return true;
    }

    /// <summary>
    ///   Loads and applies default configuration settings.
    /// </summary>
    public void LoadDefaults()
    {
        settingsData.ResetToDefaults();
        ApplyAll();
        GD.Print("Loaded and applied default settings.");
    }

    /// <summary>
    ///   Saves the current settings by writing them to the settings configuration file.
    /// </summary>
    /// <returns>True on success, false if the file can't be written.</returns>
    public bool Save()
    {
        var file = new File();
        var error = file.Open(Constants.CONFIGURATION_FILE, File.ModeFlags.Write);

        if (error != Error.Ok)
        {
            GD.PrintErr("Couldn't open settings file for writing.");
            return false;
        }

        file.StoreString(JsonConvert.SerializeObject(Data));

        file.Close();
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
    ///   Struct containing all raw settings fields for comparison and serialization.
    /// </summary>
    public struct SettingsData : IEquatable<SettingsData>
    {
        // Graphics Fields
        public bool FullScreen;
        public bool Vsync;
        public Viewport.MSAA MsaaResolution;
        public int ColourBlindSetting;
        public float ChromaticAmount;
        public bool ChromaticEnabled;

        // Sound Fields
        public float VolumeMaster;
        public bool VolumeMasterMuted;
        public float VolumeMusic;
        public bool VolumeMusicMuted;

        // Performance Properties
        public float CloudUpdateInterval;
        public int CloudResolution;

        // Misc Properties
        public bool PlayIntroVideo;
        public bool PlayMicrobeIntroVideo;
        public bool AutoSaveEnabled;
        public int MaxAutoSaves;
        public int MaxQuickSaves;
        public bool CheatsEnabled;
        public bool RunAutoEvoDuringGamePlay;

        public static bool operator ==(SettingsData lhs, SettingsData rhs)
        {
            return Equals(lhs, rhs);
        }

        public static bool operator !=(SettingsData lhs, SettingsData rhs)
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

            return Equals((SettingsData)obj);
        }

        public bool Equals(SettingsData obj)
        {
            return (FullScreen == obj.FullScreen) &&
                (Vsync == obj.Vsync) &&
                (MsaaResolution == obj.MsaaResolution) &&
                (ColourBlindSetting == obj.ColourBlindSetting) &&
                (ChromaticAmount == obj.ChromaticAmount) &&
                (ChromaticEnabled == obj.ChromaticEnabled) &&
                (VolumeMaster == obj.VolumeMaster) &&
                (VolumeMasterMuted == obj.VolumeMasterMuted) &&
                (VolumeMusic == obj.VolumeMusic) &&
                (VolumeMusicMuted == obj.VolumeMusicMuted) &&
                (CloudUpdateInterval == obj.CloudUpdateInterval) &&
                (CloudResolution == obj.CloudResolution) &&
                (PlayIntroVideo == obj.PlayIntroVideo) &&
                (PlayMicrobeIntroVideo == obj.PlayMicrobeIntroVideo) &&
                (AutoSaveEnabled == obj.AutoSaveEnabled) &&
                (MaxAutoSaves == obj.MaxAutoSaves) &&
                (MaxQuickSaves == obj.MaxQuickSaves) &&
                (CheatsEnabled == obj.CheatsEnabled) &&
                (RunAutoEvoDuringGamePlay == obj.RunAutoEvoDuringGamePlay);
        }

        public override int GetHashCode()
        {
            return FullScreen.GetHashCode() ^ Vsync.GetHashCode() ^ MsaaResolution.GetHashCode() ^
                ColourBlindSetting.GetHashCode() ^ ChromaticAmount.GetHashCode() ^ ChromaticEnabled.GetHashCode() ^
                VolumeMaster.GetHashCode() ^ VolumeMasterMuted.GetHashCode() ^ VolumeMusic.GetHashCode() ^
                VolumeMusicMuted.GetHashCode() ^ CloudUpdateInterval.GetHashCode() ^ CloudResolution.GetHashCode() ^
                PlayIntroVideo.GetHashCode() ^ PlayMicrobeIntroVideo.GetHashCode() ^ AutoSaveEnabled.GetHashCode() ^
                MaxAutoSaves.GetHashCode() ^ CheatsEnabled.GetHashCode() ^ RunAutoEvoDuringGamePlay.GetHashCode();
        }

        // Resets all settings fields to their default values.
        public void ResetToDefaults()
        {
            // Graphics Defaults
            FullScreen = true;
            Vsync = true;
            MsaaResolution = Viewport.MSAA.Disabled;
            ColourBlindSetting = 0;
            ChromaticAmount = 20.0f;
            ChromaticEnabled = true;

            // Sound Defaults
            VolumeMaster = 0.0f;
            VolumeMasterMuted = false;
            VolumeMusic = 0.0f;
            VolumeMusicMuted = false;

            // Performance Defaults
            CloudUpdateInterval = 0.040f;
            CloudResolution = 2;

            // Misc Properties
            PlayIntroVideo = true;
            PlayMicrobeIntroVideo = true;
            AutoSaveEnabled = true;
            MaxAutoSaves = 5;
            MaxQuickSaves = 5;
            CheatsEnabled = false;
            RunAutoEvoDuringGamePlay = true;
        }
    }
}
