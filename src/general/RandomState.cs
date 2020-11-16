using System;

public struct RandomState : IEquatable<RandomState>
{
    public readonly byte[] State;

    public RandomState(byte[] state)
    {
        State = state;
    }

    public override int GetHashCode()
    {
        return State.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (!(obj is RandomState))
        {
            return false;
        }

        return Equals((RandomState)obj);
    }

    public bool Equals(RandomState other)
    {
        if (State != other.State)
        {
            return false;
        }

        return true;
    }

#pragma warning disable SA1201
    public static bool operator !=(RandomState state1, RandomState state2)
    {
        return !state1.Equals(state2);
    }
#pragma warning restore SA1201

    public static bool operator ==(RandomState state1, RandomState state2)
    {
        return state1.Equals(state2);
    }
}
