namespace Saving.Serializers;

using System;
using System.IO;
using SharedBase.Archive;

/// <summary>
///   Main class for handling Thrive save writing
/// </summary>
public class SaveArchiveWriter : ISArchiveWriter
{
    private readonly Stream stream;

    public SaveArchiveWriter(Stream stream)
    {
        this.stream = stream;

        if (!stream.CanSeek)
            throw new ArgumentException("Stream must be seekable");
    }

    public void Write(byte value)
    {
        stream.WriteByte(value);
    }

    public void Write(ReadOnlySpan<byte> value)
    {
        stream.Write(value);
    }

    public long GetPosition()
    {
        return stream.Position;
    }

    public void Seek(long position)
    {
        stream.Seek(position, SeekOrigin.Begin);
    }
}
