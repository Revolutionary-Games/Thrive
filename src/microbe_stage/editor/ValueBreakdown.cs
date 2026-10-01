/// <summary>
///   Stores a summary of a value, including how much comes from the specialization bonus.
/// </summary>
public struct ValueBreakdown
{
    public float Total;

    public float Base;

    public float Specialization;

    public static ValueBreakdown Add(ValueBreakdown a, ValueBreakdown b)
    {
        var result = default(ValueBreakdown);
        result.Total = a.Total + b.Total;
        result.Base = a.Base + b.Base;
        result.Specialization = a.Specialization + b.Specialization;

        return result;
    }
}
