using Newtonsoft.Json;

public abstract class GenericStatistic<T> : IStatistic
{
    public GenericStatistic(T value)
    {
        Value = value;
    }

    public T Value { get; protected set; }

    public abstract void Increment(T value);
}

public class SimpleStatistic : GenericStatistic<int>
{
    public SimpleStatistic() : this(0)
    {
    }

    [JsonConstructor]
    public SimpleStatistic(int value) : base(value)
    {
    }

    public override void Increment(int value)
    {
        Value += value;
    }
}
