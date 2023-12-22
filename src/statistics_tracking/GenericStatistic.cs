public abstract class GenericStatistic<T> : IStatistic
{
    public StatsTrackerEvent Event { get; set; }

    public abstract T Value { get; protected set; }

    public abstract void Increment(T value);
}

public class SimpleStatistic : GenericStatistic<int>
{
    public SimpleStatistic(StatsTrackerEvent @event)
    {
        Value = 0;
        Event = @event;
    }

    public override int Value { get; protected set; }

    public override void Increment(int value = 1)
    {
        Value += value;
    }
}
