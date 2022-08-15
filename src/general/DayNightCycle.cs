using System;
using System.Collections;
using Godot;

public class DayNightCycle : Godot.Node
{
    /*

    * stuff we need at a minimum / TL;DR
        exact time
        states for dawn, day, dusk, and night
        config parameters; a JSON file
        methods to feed timeOfDay to relevant shaders
        methods to manipulate environment node for lighting and post-process

    * for simplicity's sake, the time can just be a float property (minute)
        public float Time{ get; set; };


    * we need various parameters that can be changed on a per planet basis like:
        how long a day is
        light levels for day and night if not const
        graphical particularities like color during sunrise/sunset, when to apply post fx

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
    private float minLightPercentage = 0.1f;
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
    ///   light = max(-abs(PercentOfDayElapsed*4-2)+1, minLightPercentage)
    ///   desmos: https://www.desmos.com/calculator/qfq0fcs4om
    /// </summary>
    public float DayLightPercentage
    {
        get { return Math.Max(-Math.Abs(PercentOfDayElapsed * 4 - 2) + 1, minLightPercentage); }
    }

    public override void _Process(float delta)
    {
        Time = (Time + (1 / realTimePerDay) * HoursPerDay * delta) % HoursPerDay;
    }
}
