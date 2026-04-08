using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///   A strict equality comparison for <see cref="IProcessDisplayInfo"/>, done as some displays must be much more
///   strict than normal related to the equality of the data.
/// </summary>
public class StrictProcessDisplayInfoEquality : IEquatable<StrictProcessDisplayInfoEquality>
{
    public StrictProcessDisplayInfoEquality(IProcessDisplayInfo displayInfo)
    {
        DisplayInfo = displayInfo;
    }

    public IProcessDisplayInfo DisplayInfo { get; }

    public static bool operator ==(StrictProcessDisplayInfoEquality? left, StrictProcessDisplayInfoEquality? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(StrictProcessDisplayInfoEquality? left, StrictProcessDisplayInfoEquality? right)
    {
        return !Equals(left, right);
    }

    public bool Equals(StrictProcessDisplayInfoEquality? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        var our = DisplayInfo;
        var theirs = other.DisplayInfo;

        if (our.Name != theirs.Name)
            return false;

        if (Math.Abs(our.CurrentSpeed - theirs.CurrentSpeed) > MathUtils.EPSILON)
            return false;

        // If process toggle state doesn't match, cannot be equal (needed to properly update process panel state when
        // enable / disable button is pressed)
        if (our.Enabled != theirs.Enabled)
            return false;

        if (!IsSequenceEqual(our.Inputs, theirs.Inputs))
            return false;

        if (!IsSequenceEqual(our.EnvironmentalInputs, theirs.EnvironmentalInputs))
            return false;

        if (ReferenceEquals(our.FullSpeedRequiredEnvironmentalInputs, null) !=
            ReferenceEquals(theirs.FullSpeedRequiredEnvironmentalInputs, null))
        {
            return false;
        }

        if (our.FullSpeedRequiredEnvironmentalInputs != null &&
            !our.FullSpeedRequiredEnvironmentalInputs.DictionaryEquals(theirs.FullSpeedRequiredEnvironmentalInputs!))
        {
            return false;
        }

        if (!IsSequenceEqual(our.Outputs, theirs.Outputs))
            return false;

        if (ReferenceEquals(our.LimitingCompounds, null) != ReferenceEquals(theirs.LimitingCompounds, null))
            return false;
        if (our.LimitingCompounds != null && !our.LimitingCompounds.SequenceEqual(theirs.LimitingCompounds!))
            return false;

        return true;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;

        return Equals((StrictProcessDisplayInfoEquality)obj);
    }

    public override int GetHashCode()
    {
        return DisplayInfo.GetHashCode();
    }

    private bool IsSequenceEqual(IEnumerable<KeyValuePair<Compound, float>> items1,
        IEnumerable<KeyValuePair<Compound, float>> items2)
    {
        using var enumerator1 = items1.GetEnumerator();
        using var enumerator2 = items2.GetEnumerator();

        while (enumerator1.MoveNext())
        {
            // Fail if different count
            if (!enumerator2.MoveNext())
                return false;

            var value1 = enumerator1.Current;
            var value2 = enumerator2.Current;

            // We want exact float values only
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (value1.Value != value2.Value)
                return false;

            if (value1.Key != value2.Key)
                return false;
        }

        // Fail if different number of items
        if (enumerator2.MoveNext())
            return false;

        return true;
    }
}
