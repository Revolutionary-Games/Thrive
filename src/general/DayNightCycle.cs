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
    private float realTimePerDay = 120;
    private float minLightPercentage = 0.1f; // For realism this should be removed.
    private int daytimeLeangthFactor = 3;
    private DayNightCycle()
    {
        instance = this;
    }

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
    ///   light = min(-abs(PercentOfDayElapsed*12-6)+daytimeLeangthFactor, 1)
    ///   desmos: https://www.desmos.com/calculator/iidhrbrpe2
    /// </summary>
    public float DayLightPercentage
    {
        get { return Math.Min(Math.Max(-Math.Abs(PercentOfDayElapsed * 12 - 6) + daytimeLeangthFactor, minLightPercentage), 1); }
    }

    public override void _Process(float delta)
    {
        Time = (Time + (1 / realTimePerDay) * HoursPerDay * delta) % HoursPerDay;
    }
}
