using Newtonsoft.Json;

/// <summary>
///   A concrete process that organelle does. Applies a modifier to the process
/// </summary>
/// <remarks>
///   <para>
///     This is a struct as this just packs one float and a single object reference in here. This allows much tighter
///     data packing when this is used in lists.
///   </para>
/// </remarks>
public struct TweakedProcess
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

    [JsonProperty]
    public readonly BioProcess Process;

    [JsonConstructor]
    public TweakedProcess(BioProcess process, float rate = 1.0f)
    {
        Count = count;
        Process = process;
        Rate = rate;
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

    public bool Equals(TweakedProcess other)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        return Rate == other.Rate && ReferenceEquals(Process, other.Process);
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

    private bool Equals(TweakedProcess other)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        return Rate == other.Rate && Count == other.Count &&
            ReferenceEquals(Process, other.Process);
    }
}
