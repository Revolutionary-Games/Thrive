﻿using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main object for containing player changeable game settings
/// </summary>
public class Settings
{
    private static readonly Settings INSTANCE = LoadSettings();

    static Settings()
    {
    }

    private Settings()
    {
    }

    public static Settings Instance
    {
        get
        {
            return INSTANCE;
        }
    }

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
    ///   This can be freely adjusted to adjust the performance The
    ///   higher this value is the smaller the size of the simulated
    ///   cloud is and the performance is better. Don't change this to
    ///   be higher than 1.
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

    public int CloudSimulationWidth
    {
        get
        {
            return (int)(Constants.CLOUD_X_EXTENT / CloudResolution);
        }
    }

    public int CloudSimulationHeight
    {
        get
        {
            return (int)(Constants.CLOUD_Y_EXTENT / CloudResolution);
        }
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
                GD.PrintErr("Couldn't open settings file for writing");
                return false;
            }

            file.StoreString(JsonConvert.SerializeObject(this));

            file.Close();
            return true;
        }
    }

    private static Settings LoadSettings()
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
