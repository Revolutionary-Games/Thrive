using System;
using Newtonsoft.Json;

/// <summary>
///   A concrete process that organelle does. Applies a modifier to the process
/// </summary>
public class TweakedProcess : IEquatable<TweakedProcess>
{
    [JsonProperty]
    public readonly BioProcess Process;

    public float Rate;

    public float SpeedMultiplier = 1;

    [JsonConstructor]
    public TweakedProcess(BioProcess process, float rate = 1.0f)
    {
        Rate = rate;
        Process = process;
    }

    public static bool operator ==(TweakedProcess left, TweakedProcess right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TweakedProcess left, TweakedProcess right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        if (obj is TweakedProcess casted)
            return Equals(casted);

        return false;
    }

    public bool Equals(TweakedProcess? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        if (Process != other.Process)
            return false;

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        return Rate == other.Rate && SpeedMultiplier == other.SpeedMultiplier
            && ReferenceEquals(Process, other.Process);
    }

    public TweakedProcess Clone()
    {
        return new TweakedProcess(Process, Rate);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return Rate.GetHashCode() * 397 ^ Process.GetHashCode();
        }
    }

    public override string ToString()
    {
        if (SpeedMultiplier != 1)
            return $"{Process} at {Rate}x (mult: #{SpeedMultiplier})";

        return $"{Process} at {Rate}x";
    }
}
