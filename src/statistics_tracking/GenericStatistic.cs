using Newtonsoft.Json;

public abstract class GenericStatistic<T> : IStatistic
{
    public GenericStatistic(T value, StatsTrackerEvent linkedEvent)
    {
        Value = value;
        LinkedEvent = linkedEvent;
    }

    public StatsTrackerEvent LinkedEvent { get; set; }

    public T Value { get; protected set; }

    public abstract void Increment(T value);
}

public class SimpleStatistic : GenericStatistic<int>
{
    public SimpleStatistic(StatsTrackerEvent @event)
        : base(0, @event)
    {
    }

    [JsonConstructor]
    public SimpleStatistic(int value, StatsTrackerEvent linkedEvent)
        : base(value, linkedEvent)
    {
    }

    public override void Increment(int value)
    {
        Value += value;
    }
}
