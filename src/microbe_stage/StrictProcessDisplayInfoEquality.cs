using System;
using System.Linq;

/// <summary>
///   A strict equality comparison for <see cref="IProcessDisplayInfo"/>
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

        if (ReferenceEquals(our.Inputs, null) != ReferenceEquals(theirs.Inputs, null))
            return false;
        if (our.Inputs != null && !our.Inputs.SequenceEqual(theirs.Inputs!))
            return false;

        if (ReferenceEquals(our.EnvironmentalInputs, null) != ReferenceEquals(theirs.EnvironmentalInputs, null))
            return false;
        if (our.EnvironmentalInputs != null && !our.EnvironmentalInputs.SequenceEqual(theirs.EnvironmentalInputs!))
            return false;

        if (ReferenceEquals(our.FullSpeedRequiredEnvironmentalInputs, null) !=
            ReferenceEquals(theirs.FullSpeedRequiredEnvironmentalInputs, null))
        {
            return false;
        }

        if (our.FullSpeedRequiredEnvironmentalInputs != null &&
            !our.FullSpeedRequiredEnvironmentalInputs.SequenceEqual(theirs.FullSpeedRequiredEnvironmentalInputs!))
        {
            return false;
        }

        if (ReferenceEquals(our.Outputs, null) != ReferenceEquals(theirs.Outputs, null))
            return false;
        if (our.Outputs != null && !our.Outputs.SequenceEqual(theirs.Outputs!))
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
}
