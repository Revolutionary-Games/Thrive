using System;
using System.Text;
using Godot;

/// <summary>
///   Reads messages written by <see cref="NetworkWriter"/>
/// </summary>
/// <remarks>
///   <para>
///     All read methods throw <see cref="EndOfNetworkMessageException"/> when the message is shorter than
///     expected. Data coming from a client is never trusted, so a caller must be ready for that exception rather
///     than assuming a message is well-formed.
///   </para>
/// </remarks>
public class NetworkReader
{
    private byte[] buffer = Array.Empty<byte>();
    private int position;
    private int length;

    /// <summary>
    ///   Bytes left to read in the current message
    /// </summary>
    public int Remaining => length - position;

    public bool AtEnd => position >= length;

    /// <summary>
    ///   Points this reader at new data. The data must stay valid while it is being read.
    /// </summary>
    public void SetData(byte[] data, int dataLength)
    {
        if (dataLength > data.Length)
            throw new ArgumentException("Length is past the end of the data", nameof(dataLength));

        buffer = data;
        length = dataLength;
        position = 0;
    }

    public bool ReadBool()
    {
        RequireBytes(1);
        return buffer[position++] != 0;
    }

    public byte ReadByte()
    {
        RequireBytes(1);
        return buffer[position++];
    }

    public ushort ReadUInt16()
    {
        RequireBytes(sizeof(ushort));
        var value = BitConverter.ToUInt16(buffer, position);
        position += sizeof(ushort);
        return value;
    }

    public int ReadInt32()
    {
        RequireBytes(sizeof(int));
        var value = BitConverter.ToInt32(buffer, position);
        position += sizeof(int);
        return value;
    }

    public uint ReadUInt32()
    {
        RequireBytes(sizeof(uint));
        var value = BitConverter.ToUInt32(buffer, position);
        position += sizeof(uint);
        return value;
    }

    public float ReadFloat()
    {
        RequireBytes(sizeof(float));
        var value = BitConverter.ToSingle(buffer, position);
        position += sizeof(float);
        return value;
    }

    public Vector3 ReadVector3()
    {
        return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
    }

    public string ReadString()
    {
        int byteCount = ReadUInt16();
        RequireBytes(byteCount);

        var value = Encoding.UTF8.GetString(buffer, position, byteCount);
        position += byteCount;
        return value;
    }

    /// <summary>
    ///   Reads a byte blob as a span pointing into the message data, valid only until the next <see cref="SetData"/>
    ///   call
    /// </summary>
    public ReadOnlySpan<byte> ReadBytes()
    {
        int byteCount = ReadUInt16();
        RequireBytes(byteCount);

        var result = new ReadOnlySpan<byte>(buffer, position, byteCount);
        position += byteCount;
        return result;
    }

    public MessageType ReadMessageType()
    {
        return (MessageType)ReadByte();
    }

    public float ReadQuantized16(float min, float max)
    {
        return QuantizationHelpers.FromQuantized16(ReadUInt16(), min, max);
    }

    public float ReadQuantized8(float min, float max)
    {
        return QuantizationHelpers.FromQuantized8(ReadByte(), min, max);
    }

    public float ReadAngle()
    {
        return QuantizationHelpers.AngleFromQuantized16(ReadUInt16());
    }

    private void RequireBytes(int count)
    {
        if (position + count > length)
            throw new EndOfNetworkMessageException();
    }
}
