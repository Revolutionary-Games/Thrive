using System;
using Godot;

public struct NetworkInputVars : INetworkSerializable, IEquatable<NetworkInputVars>
{
    public Vector3 WorldLookAtPoint { get; set; }

    /// <summary>
    ///   We know that movement direction will always be in the range of (1, 1, 1) so we can just encode each axis plus
    ///   sign bits all in the space of 6 bits.
    /// </summary>
    public byte MovementDirection { get; set; }

    /// <summary>
    ///   Bitmask of true/false value inputs. Maximum of 255 flags. TODO: Turn this into array?
    /// </summary>
    public byte Bools { get; set; }

    public static bool operator ==(NetworkInputVars left, NetworkInputVars right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NetworkInputVars left, NetworkInputVars right)
    {
        return !left.Equals(right);
    }

    public void EncodeMovementDirection(Vector3 direction)
    {
        MovementDirection = 0;

        MovementDirection |= (byte)((direction.x < 0 ? 1 : 0) << 0);
        MovementDirection |= (byte)((direction.y < 0 ? 1 : 0) << 1);
        MovementDirection |= (byte)((direction.z < 0 ? 1 : 0) << 2);

        MovementDirection |= (byte)(Mathf.CeilToInt(Mathf.Abs(direction.x)) << 3);
        MovementDirection |= (byte)(Mathf.CeilToInt(Mathf.Abs(direction.y)) << 4);
        MovementDirection |= (byte)(Mathf.CeilToInt(Mathf.Abs(direction.z)) << 5);
    }

    public Vector3 DecodeMovementDirection()
    {
        var x = MovementDirection.ToBoolean(3) ? 1 : 0;
        var y = MovementDirection.ToBoolean(4) ? 1 : 0;
        var z = MovementDirection.ToBoolean(5) ? 1 : 0;

        return new Vector3(
            MovementDirection.ToBoolean(0) ? -x : x,
            MovementDirection.ToBoolean(1) ? -y : y,
            MovementDirection.ToBoolean(2) ? -z : z).Normalized();
    }

    public void NetworkSerialize(BytesBuffer buffer)
    {
        // 14 bytes

        buffer.Write(WorldLookAtPoint);
        buffer.Write(MovementDirection);
        buffer.Write(Bools);
    }

    public void NetworkDeserialize(BytesBuffer buffer)
    {
        WorldLookAtPoint = buffer.ReadVector3();
        MovementDirection = buffer.ReadByte();
        Bools = buffer.ReadByte();
    }

    public bool Equals(NetworkInputVars other)
    {
        return WorldLookAtPoint.IsEqualApprox(other.WorldLookAtPoint) &&
            MovementDirection == other.MovementDirection &&
            Bools == other.Bools;
    }

    public override bool Equals(object obj)
    {
        return obj is NetworkInputVars input && Equals(input);
    }

    public override int GetHashCode()
    {
        int hashCode = -1311921306;
        hashCode = hashCode * -1521134295 + WorldLookAtPoint.GetHashCode();
        hashCode = hashCode * -1521134295 + MovementDirection.GetHashCode();
        hashCode = hashCode * -1521134295 + Bools.GetHashCode();
        return hashCode;
    }
}
