using System;
using System.Collections.Generic;
using System.Text;
using Godot;

/// <summary>
///   Holds an resizable array of bytes that can be read and wrote into. Provides low-level access for manually
///   constructing a binary format, encoded using Godot's byte encoding implementation as the base, see
///   <see cref="MarshalUtils"/>.
/// </summary>
public class BytesBuffer
{
    private List<byte> buffer;
    private int position;

    /// <summary>
    ///   Constructs an empty resizable buffer.
    /// </summary>
    public BytesBuffer()
    {
        buffer = new List<byte>();
    }

    /// <summary>
    ///   Constructs a new buffer with the given initial capacity.
    /// </summary>
    public BytesBuffer(int size)
    {
        buffer = new List<byte>(size);
    }

    /// <summary>
    ///   Constructs a new buffer from the given byte array.
    /// </summary>
    public BytesBuffer(byte[] buffer)
    {
        this.buffer = new List<byte>(buffer);
    }

    /// <summary>
    ///   Returns the internal buffer.
    /// </summary>
    public byte[] Data => buffer.ToArray();

    /// <summary>
    ///   Gets and sets the read/write cursor. Always reset this to 0 when switching between read/write operation.
    /// </summary>
    public int Position
    {
        get => position;
        set
        {
            if (value < 0 || value > buffer.Count)
                return;

            position = value;
        }
    }

    /// <summary>
    ///   Returns the internal buffer's byte count.
    /// </summary>
    public int Length => buffer.Count;

    /// <summary>
    ///   Writes a byte array to the buffer. Advances the position by that array's length.
    /// </summary>
    public void Write(byte[] buffer)
    {
        this.buffer.AddRange(buffer);
        position += buffer.Length;
    }

    /// <summary>
    ///   Writes a <see cref="BytesBuffer"/> to the buffer. Advances the position by that buffer's
    ///   length + 4 byte header.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Use this when you don't know the exact number of bytes in the buffer.
    ///   </para>
    /// </remarks>
    public void Write(BytesBuffer buffer)
    {
        Write(buffer.Length);
        Write(buffer.Data);
    }

    /// <summary>
    ///   Writes a byte to the buffer and advances the position by 1 byte.
    /// </summary>
    public void Write(byte value)
    {
        buffer.Add(value);
        position += 1;
    }

    /// <summary>
    ///   Writes a 1-byte boolean to the buffer with 1 representing true and 0 representing false and
    ///   advances the current position by 1 byte.
    /// </summary>
    public void Write(bool value)
    {
        Write(value.ToByte());
    }

    /// <summary>
    ///   Writes an unsigned 2-byte integer to the buffer and advances the position by 2 byte.
    /// </summary>
    public void Write(ushort value)
    {
        var data = new byte[2];
        data.EncodeUInt16(value);
        Write(data);
    }

    /// <summary>
    ///   Writes a signed 2-byte integer to the buffer and advances the position by 2 byte.
    /// </summary>
    public void Write(short value)
    {
        Write((ushort)value);
    }

    /// <summary>
    ///   Writes an unsigned 4-byte integer to the buffer and advances the position by 4 byte.
    /// </summary>
    public void Write(uint value)
    {
        var data = new byte[4];
        data.EncodeUInt32(value);
        Write(data);
    }

    /// <summary>
    ///   Writes a signed 4-byte integer to the buffer and advances the position by 4 byte.
    /// </summary>
    public void Write(int value)
    {
        Write((uint)value);
    }

    /// <summary>
    ///   Writes an unsigned 8-byte integer to the buffer and advances the position by 8 byte.
    /// </summary>
    public void Write(ulong value)
    {
        var data = new byte[8];
        data.EncodeUInt64(value);
        Write(data);
    }

    /// <summary>
    ///   Writes a signed 8-byte integer to the buffer and advances the position by 8 byte.
    /// </summary>
    public void Write(long value)
    {
        Write((ulong)value);
    }

    /// <summary>
    ///   Writes a 4-byte floating-point value to the buffer and advances the position by 4 byte.
    /// </summary>
    public void Write(float value)
    {
        var data = new byte[4];
        data.EncodeSingle(value);
        Write(data);
    }

    /// <summary>
    ///   Writes an ASCII encoded string to the buffer and advances the position by that encoded string's length
    ///    + 4-byte header.
    /// </summary>
    public void Write(string value)
    {
        Write(Encoding.ASCII.GetByteCount(value));
        Write(Encoding.ASCII.GetBytes(value));
    }

    public void Write(Vector2 value)
    {
        Write(value.x);
        Write(value.y);
    }

    public void Write(Vector3 value)
    {
        Write(value.x);
        Write(value.y);
        Write(value.z);
    }

    /// <summary>
    ///   Writes an UTF-8 encoded string to the buffer and advances the position by that encoded string's length
    ///    + 4-byte header.
    /// </summary>
    public void WriteUtf8(string value)
    {
        Write(Encoding.UTF8.GetByteCount(value));
        Write(Encoding.UTF8.GetBytes(value));
    }

    /// <summary>
    ///   Writes a Godot Variant encoded as a byte array to the buffer and advances the current position by that
    ///   array's length + 8-byte header.
    /// </summary>
    /// <remarks>
    ///   Use this method sparingly, reserve this for when you don't know the exact length of bytes and or the type
    ///   of the object to write.
    /// </remarks>
    public void WriteVariant(object variant)
    {
        var data = GD.Var2Bytes(variant);
        Write(data.Length);
        Write(data);
    }

    /// <summary>
    ///   Reads the specified number of bytes from the buffer into a byte array and
    ///   advances the current position by that number of bytes.
    /// </summary>
    public byte[] ReadBytes(int count)
    {
        if (buffer.Count <= 0)
            throw new InvalidOperationException("Can't read bytes on an empty buffer");

        int received;

        if (position + count > buffer.Count)
        {
            received = buffer.Count - position;

            if (received <= 0)
                return Array.Empty<byte>();
        }
        else
        {
            received = count;
        }

        var result = buffer.GetRange(position, received).ToArray();
        position += received;

        return result;
    }

    /// <summary>
    ///   Reads a byte array wrapped as <see cref="BytesBuffer"/> from the buffer and advances the current
    ///   position by that array's length + 4 byte header.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Use this when you don't know the exact number of bytes in the buffer.
    ///   </para>
    /// </remarks>
    public BytesBuffer ReadBuffer()
    {
        var size = ReadInt32();
        return new BytesBuffer(ReadBytes(size));
    }

    /// <summary>
    ///   Reads one byte from the buffer and advances the current position by one byte.
    /// </summary>
    public byte ReadByte()
    {
        return ReadBytes(1)[0];
    }

    /// <summary>
    ///   Reads a 1-byte boolean from the buffer with 1 representing true and 0 representing false and
    ///   advances the current position by 1 byte.
    /// </summary>
    public bool ReadBoolean()
    {
        return ReadByte().ToBoolean();
    }

    /// <summary>
    ///   Reads a 2-byte unsigned integer from the buffer and advances the position by 2 byte.
    /// </summary>
    public ushort ReadUInt16()
    {
        var data = ReadBytes(2);
        return data.DecodeUInt16();
    }

    /// <summary>
    ///   Reads a 2-byte signed integer from the buffer and advances the position by 2 byte.
    /// </summary>
    public short ReadInt16()
    {
        return (short)ReadUInt16();
    }

    /// <summary>
    ///   Reads a 4-byte unsigned integer from the buffer and advances the position by 4 byte.
    /// </summary>
    public uint ReadUInt32()
    {
        var data = ReadBytes(4);
        return data.DecodeUInt32();
    }

    /// <summary>
    ///   Reads a 4-byte signed integer from the buffer and advances the position by 4 byte.
    /// </summary>
    public int ReadInt32()
    {
        return (int)ReadUInt32();
    }

    /// <summary>
    ///   Reads an 8-byte unsigned integer from the buffer and advances the position by 8 byte.
    /// </summary>
    public ulong ReadUInt64()
    {
        var data = ReadBytes(8);
        return data.DecodeUInt64();
    }

    /// <summary>
    ///   Reads an 8-byte signed integer from the buffer and advances the position by 8 byte.
    /// </summary>
    public long ReadInt64()
    {
        return (long)ReadUInt64();
    }

    /// <summary>
    ///   Reads a 4-byte floating-point value from the buffer and advances the position by 4 byte.
    /// </summary>
    public float ReadSingle()
    {
        var data = ReadBytes(4);
        return data.DecodeSingle();
    }

    /// <summary>
    ///   Reads an ASCII encoded string from the buffer and advances the current position by the encoded string's
    ///   length + 4-byte header.
    /// </summary>
    public string ReadString()
    {
        var count = ReadInt32();
        var data = ReadBytes(count);
        return Encoding.ASCII.GetString(data);
    }

    /// <summary>
    ///   Reads an UTF-8 encoded string from the buffer and advances the current position by the encoded string's
    ///   length + 4-byte header.
    /// </summary>
    public string ReadUtf8String()
    {
        var count = ReadInt32();
        var data = ReadBytes(count);
        return Encoding.UTF8.GetString(data);
    }

    public Vector2 ReadVector2()
    {
        return new Vector2(ReadSingle(), ReadSingle());
    }

    public Vector3 ReadVector3()
    {
        return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
    }

    /// <summary>
    ///   Reads a Variant encoded as a byte array from the buffer and advances the current position by that
    ///   array's length + 8-byte header.
    /// </summary>
    public object ReadVariant()
    {
        var count = ReadInt32();
        var data = ReadBytes(count);
        return GD.Bytes2Var(data);
    }
}
