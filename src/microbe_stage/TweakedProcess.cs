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
    public float Rate;

    public TweakedProcess(BioProcess process, float rate = 1.0f)
    {
        Rate = rate;
        Process = process;
    }

    public BioProcess Process { get; }

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
            return (Rate.GetHashCode() * 397) ^ Process.GetHashCode();
        }
    }
}
