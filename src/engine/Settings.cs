﻿using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main object for containing player changeable game settings
/// </summary>
public class Settings
{
    private static readonly Settings SingletonInstance = LoadSettings();

    static Settings()
    {
    }

    private Settings()
    {
    }

    public static Settings Instance => SingletonInstance;

    /// <summary>
    ///   If true all sounds are muted
    /// </summary>
    public bool VolumeMasterMuted { get; set; } = false;

    /// <summary>
    ///   The Db value to be added to the master audio bus
    /// </summary>
    public float VolumeMaster { get; set; } = 0.0f;

    /// <summary>
    ///   If true music is muted
    /// </summary>
    public bool VolumeMusicMuted { get; set; } = false;

    /// <summary>
    ///   The Db value to be added to the music audio bus
    /// </summary>
    public float VolumeMusic { get; set; } = 0.0f;

    /// <summary>
    ///   If true tell godot to be in fullscreen mode
    /// </summary>
    public bool FullScreen { get; set; } = true;

    /// <summary>
    ///   If true tell godot to use vsync
    /// </summary>
    public bool VSync { get; set; } = true;

    /// <summary>
    ///   When true cheats are enabled
    /// </summary>
    public bool CheatsEnabled { get; set; } = false;

    /// <summary>
    ///   When true the main intro is played
    /// </summary>
    public bool PlayIntroVideo { get; set; } = true;

    /// <summary>
    ///   When true the microbe intro is played on new game
    /// </summary>
    public bool PlayMicrobeIntroVideo { get; set; } = true;

    /// <summary>
    ///   If true an auto-evo run is started during gameplay,
    ///   taking up one of the background threads.
    /// </summary>
    public bool RunAutoEvoDuringGamePlay { get; set; } = true;

    /// <summary>
    ///   If false auto saving will be disabled
    /// </summary>
    public bool AutoSaveEnabled { get; set; } = true;

    /// <summary>
    ///   Number of quick saves to keep
    /// </summary>
    public int MaxQuickSaves { get; set; } = 5;

    /// <summary>
    ///   Number of auto saves to keep
    /// </summary>
    public int MaxAutoSaves { get; set; } = 5;

    /// <summary>
    ///   This can be freely adjusted to adjust the performance The
    ///   higher this value is the smaller the size of the simulated
    ///   cloud is and the performance is better.
    /// </summary>
    public int CloudResolution { get; set; } = 2;

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
    ///   Sets amount of MSAA to apply to the viewport
    /// </summary>
    public Viewport.MSAA MSAAResolution { get; set; } = Viewport.MSAA.Disabled;

    /// <summary>
    ///   Choose what filter to apply to the screen
    ///   0 = None, 1 = Red/Green, 2 = Blue/Yellow
    /// </summary>
    public int ColourblindSetting { get; set; } = 0;

    public int CloudSimulationWidth => Constants.CLOUD_X_EXTENT / CloudResolution;

    public int CloudSimulationHeight => Constants.CLOUD_Y_EXTENT / CloudResolution;

    /// <summary>
    ///   The amount of Chromatic Aberration to apply to the screen
    /// </summary>
    public float ChromaticAmount { get; set; } = 20f;

    /// <summary>
    ///   Enable or Disable Chromatic Aberration for screen
    /// </summary>
    public bool ChromaticEnabled { get; set; } = true;

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
                GD.PrintErr("Couldn't open settings file for writing");
                return false;
            }

            file.StoreString(JsonConvert.SerializeObject(this));

            file.Close();
            return true;
        }
    }

    /// <summary>
    ///   Applies all the general settings
    /// </summary>
    public void ApplyAll()
    {
        ApplySoundLevels();
        ApplyWindowSettings();
        ApplyGraphicsSettings();
    }

    /// <summary>
    ///   Applies the sound volume and mute settings on the audio bus
    /// </summary>
    public void ApplySoundLevels()
    {
        var master = AudioServer.GetBusIndex("Master");

        AudioServer.SetBusVolumeDb(master, VolumeMaster);
        AudioServer.SetBusMute(master, VolumeMasterMuted);

        var music = AudioServer.GetBusIndex("Music");

        AudioServer.SetBusVolumeDb(music, VolumeMusic);
        AudioServer.SetBusMute(music, VolumeMusicMuted);
    }

    public void ApplyWindowSettings()
    {
        OS.WindowFullscreen = FullScreen;
        OS.VsyncEnabled = VSync;
    }

    public void ApplyGraphicsSettings()
    {
        GUICommon.Instance.GetTree().Root.GetViewport().Msaa = MSAAResolution;
        ColourblindScreenFilter.Instance.SetColourblindSetting(ColourblindSetting);
    }

    /// <summary>
    ///   Reset all options to default values
    /// </summary>
    public void ResetToDefaults()
    {
        // var defaults = new Settings();

        // TODO: apply the default values
        throw new NotImplementedException();
    }

    private static Settings LoadSettings()
    {
        var settings = LoadSettingsFileOrDefault();
        settings.ApplyAll();
        return settings;
    }

    private static Settings LoadSettingsFileOrDefault()
    {
        using (var file = new File())
        {
            var error = file.Open(Constants.CONFIGURATION_FILE, File.ModeFlags.Read);

            if (error != Error.Ok)
            {
                GD.Print("Settings file is missing or unreadable, using default settings");
                return new Settings();
            }

            var text = file.GetAsText();

            file.Close();

            return JsonConvert.DeserializeObject<Settings>(text);
        }
    }
}
