using System;

/// <summary>
///   A concrete process that organelle does. Applies a modifier to the process
/// </summary>
public class TweakedProcess : ICloneable
{
    /// <summary>
    ///   Holds the number of times the Process should be ran each cycle
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Most of the time this equals the number of organelles that run this process in the microbe,
    ///     but this is not always the case
    ///   </para>
    /// </remarks>
    public float Count;

    /// <summary>
    ///   Acts as a speed multiplier for the process
    /// </summary>
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
