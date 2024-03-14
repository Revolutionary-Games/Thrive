using System;
using System.IO;
using FileAccess = Godot.FileAccess;

/// <summary>
///   Wraps a Godot.File as a Stream
/// </summary>
public class GodotFileStream : Stream
{
    private readonly FileAccess file;

    public GodotFileStream(FileAccess file)
    {
        this.file = file;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => true;
    public override long Length => checked((long)file.GetLength());

    public override long Position
    {
        get => checked((long)file.GetPosition());
        set => file.Seek(checked((ulong)value));
    }

    /// <summary>
    ///   AFAIK the Godot file has no buffering so we can't and don't need to support this
    /// </summary>
    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var data = file.GetBuffer(count);
        Array.Copy(data, 0, buffer, offset, data.Length);
        return data.Length;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                file.Seek((ulong)offset);
                break;
            case SeekOrigin.Current:
                file.Seek(checked((ulong)((long)file.GetPosition() + offset)));
                break;
            case SeekOrigin.End:
                file.SeekEnd(offset);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
        }

        return checked((long)file.GetPosition());
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (offset == 0 && count == buffer.Length)
        {
            file.StoreBuffer(buffer);
        }
        else
        {
            // We need a temporary copy
            byte[] copy = new byte[count];
            Array.Copy(buffer, offset, copy, 0, count);
            file.StoreBuffer(copy);
        }
    }
}
