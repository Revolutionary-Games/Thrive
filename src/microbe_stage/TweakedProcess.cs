using System;

/// <summary>
///   A concrete process that organelle does. Applies a modifier to the process
/// </summary>
public class TweakedProcess : ICloneable
{
    public float Count;
    public float Rate;

    public TweakedProcess(BioProcess process, float count = 1.0f, float rate = 1.0f)
    {
        Count = count;
        Process = process;
        Rate = rate;
    }

    public BioProcess Process { get; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj is TweakedProcess casted)
            return Equals(casted);

        return false;
    }

    public object Clone()
    {
        return new TweakedProcess(Process, Rate);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Rate.GetHashCode() * 397) ^ Process.GetHashCode();
        }
    }

    private bool Equals(TweakedProcess other)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        return Rate == other.Rate && Count == other.Count &&
            ReferenceEquals(Process, other.Process);
    }
}
