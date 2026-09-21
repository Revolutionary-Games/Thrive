using System;
using System.Text;
using Godot;

/// <summary>
///   Writes network messages into a growable byte buffer
/// </summary>
/// <remarks>
///   <para>
///     A writer is meant to be kept around and reused with <see cref="Reset"/> between messages, as allocating one
///     per message would create garbage every tick.
///   </para>
///   <para>
///     Everything is written little endian and byte aligned.
///   </para>
/// </remarks>
public class NetworkWriter
{
    private byte[] buffer;
    private int position;

    public NetworkWriter(int initialCapacity = 1024)
    {
        buffer = new byte[initialCapacity];
    }

    /// <summary>
    ///   Number of bytes written so far
    /// </summary>
    public int Length => position;

    public ReadOnlySpan<byte> WrittenData => new(buffer, 0, position);

    public void Reset()
    {
        position = 0;
    }

    public void Write(bool value)
    {
        EnsureSpace(1);
        buffer[position++] = value ? (byte)1 : (byte)0;
    }

    public void Write(byte value)
    {
        EnsureSpace(1);
        buffer[position++] = value;
    }

    public void Write(ushort value)
    {
        EnsureSpace(sizeof(ushort));
        BitConverter.TryWriteBytes(new Span<byte>(buffer, position, sizeof(ushort)), value);
        position += sizeof(ushort);
    }

    public void Write(int value)
    {
        EnsureSpace(sizeof(int));
        BitConverter.TryWriteBytes(new Span<byte>(buffer, position, sizeof(int)), value);
        position += sizeof(int);
    }

    public void Write(uint value)
    {
        EnsureSpace(sizeof(uint));
        BitConverter.TryWriteBytes(new Span<byte>(buffer, position, sizeof(uint)), value);
        position += sizeof(uint);
    }

    public void Write(float value)
    {
        EnsureSpace(sizeof(float));
        BitConverter.TryWriteBytes(new Span<byte>(buffer, position, sizeof(float)), value);
        position += sizeof(float);
    }

    public void Write(Vector3 value)
    {
        Write(value.X);
        Write(value.Y);
        Write(value.Z);
    }

    public void Write(string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);

        if (byteCount > ushort.MaxValue)
            throw new ArgumentException("String is too long to write", nameof(value));

        Write((ushort)byteCount);
        EnsureSpace(byteCount);

        Encoding.UTF8.GetBytes(value, new Span<byte>(buffer, position, byteCount));
        position += byteCount;
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        Write((ushort)data.Length);
        EnsureSpace(data.Length);

        data.CopyTo(new Span<byte>(buffer, position, data.Length));
        position += data.Length;
    }

    public void Write(MessageType value)
    {
        Write((byte)value);
    }

    /// <summary>
    ///   Writes a float that is known to stay within a range, using two bytes instead of four
    /// </summary>
    /// <param name="value">Value to write, which is clamped to the given range</param>
    /// <param name="min">Lowest value that can be represented</param>
    /// <param name="max">Highest value that can be represented</param>
    public void WriteQuantized16(float value, float min, float max)
    {
        Write(QuantizationHelpers.ToQuantized16(value, min, max));
    }

    /// <summary>
    ///   Writes a float within a range using a single byte. Only suitable for values where visible stepping does
    ///   not matter, for example a health fraction shown on another player's cell.
    /// </summary>
    public void WriteQuantized8(float value, float min, float max)
    {
        Write(QuantizationHelpers.ToQuantized8(value, min, max));
    }

    /// <summary>
    ///   Writes an angle in radians using two bytes
    /// </summary>
    public void WriteAngle(float radians)
    {
        Write(QuantizationHelpers.AngleToQuantized16(radians));
    }

    private void EnsureSpace(int extraBytes)
    {
        int required = position + extraBytes;

        if (required <= buffer.Length)
            return;

        if (required > NetworkConstants.MAX_MESSAGE_SIZE)
            throw new InvalidOperationException("Network message grew past the maximum message size");

        int newSize = buffer.Length * 2;

        while (newSize < required)
        {
            newSize *= 2;
        }

        Array.Resize(ref buffer, newSize);
    }
}
