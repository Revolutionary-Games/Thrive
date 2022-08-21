using System;
using System.Collections;
using Godot;

public class DayNightCycle : Godot.Node
{
    public float HoursPerDay = 24;

    /// <summary>
    ///   This is how long it takes to complete a full day in realtime seconds
    /// </summary>
    private float realTimePerDay = 240;
    private float daytimeDaylightLen = 16;

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
        get { return Math.Max(-(float)Math.Pow(PercentOfDayElapsed - 0.5, 2) * daytimeDaylightLen + 1, 0); }
    }

    public override void _Process(float delta)
    {
        Time = (Time + (1 / realTimePerDay) * HoursPerDay * delta) % HoursPerDay;
        GD.Print(DayLightPercentage);
    }
}
