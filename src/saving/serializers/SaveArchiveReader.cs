namespace Saving.Serializers;

using System;
using System.IO;
using SharedBase.Archive;

/// <summary>
///   Main class for handling Thrive save reading
/// </summary>
public class SaveArchiveReader : ISArchiveReader
{
    private const int BUFFER_SIZE = 1024;

    private readonly Stream stream;

    private byte[]? tempBuffer;

    public SaveArchiveReader(Stream stream)
    {
        this.stream = stream;
    }

    public byte ReadInt8()
    {
        return (byte)stream.ReadByte();
    }

    public uint ReadVariableLengthField32()
    {
        uint result = 0;
        int shift = 0;

        for (int i = 0; i < 5; i++, shift += 7)
        {
            var currentByte = ReadInt8();
            result |= (uint)(currentByte & 0x7F) << shift;

            if ((currentByte & 0x80) == 0)
                return result;

            if (i == 4)
                throw new FormatException("Too many bytes in variable length field");
        }

        throw new FormatException("Variable-length field was not terminated");
    }

    public string ReadString()
    {
        var lengthRaw = ReadVariableLengthField32();

        if (lengthRaw == 0)
            return string.Empty;

        if (lengthRaw > int.MaxValue)
            throw new FormatException("Too long string");

        int length = (int)lengthRaw;

        if (length <= BUFFER_SIZE)
        {
            tempBuffer ??= new byte[BUFFER_SIZE];
            ReadBytes(tempBuffer.AsSpan(0, length));
            return ISArchiveWriter.Utf8NoSignature.GetString(tempBuffer, 0, length);
        }

        var pool = System.Buffers.ArrayPool<byte>.Shared;
        var rented = pool.Rent(length);
        try
        {
            ReadBytes(rented.AsSpan(0, length));
            return ISArchiveWriter.Utf8NoSignature.GetString(rented, 0, length);
        }
        finally
        {
            pool.Return(rented);
        }
    }

    public void ReadBytes(Span<byte> buffer)
    {
        if (stream.Read(buffer) != buffer.Length)
            throw new EndOfStreamException();
    }
}
