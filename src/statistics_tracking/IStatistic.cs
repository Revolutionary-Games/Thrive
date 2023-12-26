/// <summary>
///   A statitsic about the player or world tracked by <see cref="WorldStatsTracker"/>.
/// </summary>
public interface IStatistic
{
    public StatsTrackerEvent LinkedEvent { get; set; }
}
