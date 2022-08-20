using System;
using System.Collections;
using Godot;

public class DayNightCycle : Godot.Node
{
    /*
    * stuff we need
        config parameters; a JSON file
        methods to manipulate environment node for lighting and post-process
    */

    /*

    Gamedungeons Notes:

    * Almost all of these should be converted to json. I really don't know how.

    * Probably need to add save support

    */

    public float HoursPerDay = 24;
    private static DayNightCycle? instance;

    /// <summary>
    ///   This is how long it takes to complete a full day in realtime seconds
    /// </summary>
    private float realTimePerDay = 240;
    private float daytimeDaylightLen = 16;
    private DayNightCycle()
    {
        instance = this;
    }

    // TODO: Figure out how to avoid doing this without large reafactors to ProcessSystem.cs
    public static DayNightCycle Instance => instance ?? throw new InstanceNotLoadedYetException();

    /// <summary>
    ///   The current time in hours
    /// </summary>
    public float Time { get; set; }

    public float PercentOfDayElapsed
    {
        get { return Time / HoursPerDay; }
    }

    /// <summary>
    ///   The percentage of daylight you should get.
    ///   light = max(-(PercentOfDayElapsed - 0.5)^2 * daytimeDaylightLen + 1, 0)
    ///   desmos: https://www.desmos.com/calculator/vrrk1bkac2
    /// </summary>
    public float DayLightPercentage
    {
        get { return Math.Max(-(float)Math.Pow((PercentOfDayElapsed - 0.5), 2) * daytimeDaylightLen + 1, 0); }
    }

    public override void _Process(float delta)
    {
        Time = (Time + (1 / realTimePerDay) * HoursPerDay * delta) % HoursPerDay;
        GD.Print(DayLightPercentage);
    }
}
