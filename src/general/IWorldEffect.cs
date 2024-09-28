/// <summary>
///   Time dependent effects running on a world
/// </summary>
public interface IWorldEffect
{
    /// <summary>
    ///   Called when added to a world. The best time to do dynamic casts
    /// </summary>
    public void OnRegisterToWorld();

    public void OnTimePassed(double elapsed, double totalTimePassed);
}
