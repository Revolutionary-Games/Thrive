using System;

/// <summary>
///   Time dependent effects running on a world
/// </summary>
public interface IWorldEffect
{
    /// <summary>
    /// Called when added to a world. The best time to do dynamic casts
    /// </summary>
    void OnRegisterToWorld();

    void OnTimePassed(double elapsed, double totalTimePassed);
}

/// <summary>
///   A helper providing a way to run lambda when time passes
/// </summary>
public class WorldEffectLambda : IWorldEffect
{
    private readonly Action<double, double> onElapsed;

    public WorldEffectLambda(Action<double, double> onElapsed)
    {
        this.onElapsed = onElapsed;
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        onElapsed.Invoke(elapsed, totalTimePassed);
    }
}
