using System;
using System.Formats.Tar;
using System.IO;
using System.Text;
using Godot;

public static class TarHelper
{
    public static void OutputEntry(TarWriter archive, string name, Stream data)
    {
#if DEBUG
        if (data.Position != 0)
            throw new ArgumentException("Data stream should be rewound");
#endif

        var entry = new PaxTarEntry(TarEntryType.RegularFile, name)
        {
            Mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.GroupWrite |
                UnixFileMode.OtherRead,

            DataStream = data,

            // TODO: could fill in more of the properties
        };

        archive.WriteEntry(entry);
    }

    public static void OutputEntry(TarWriter archive, string name, byte[] data)
    {
        OutputEntry(archive, name, new MemoryStream(data));
    }

    public static void OutputEntry(TarWriter archive, string name, string text, MemoryStream storageStreamToUse,
        StreamWriter textFormatterToStorage)
    {
        // Reuse the buffer object. If there was a way to reuse the buffer without overwriting it with zeros first that
        // would be better
        storageStreamToUse.SetLength(0);

        textFormatterToStorage.Write(text);
        textFormatterToStorage.Flush();

        // Rewind stream to write from the right starting position
        storageStreamToUse.Position = 0;

        OutputEntry(archive, name, storageStreamToUse);
    }

    public static string ReadStringEntry(TarEntry entry)
    {
        if (entry.DataStream == null)
            throw new ArgumentException("TarEntry has no data stream");

        // Leave open must be true, otherwise the tar reader cannot proceed after reading this one entry
        using var utf8Reader = new StreamReader(entry.DataStream, Encoding.UTF8, true, -1, true);

        return utf8Reader.ReadToEnd();
    }

    public static byte[] ReadBytesEntry(TarEntry entry)
    {
        if (entry.DataStream == null)
            throw new ArgumentException("TarEntry has no data stream");

        // Storage for the entry data
        var buffer = new byte[entry.Length];
        {
            // Read the data into the pre-allocated buffer
            using var stream = new MemoryStream(buffer);
            entry.DataStream.CopyTo(stream);
        }

        return buffer;
    }

    public static Image ImageFromBuffer(byte[] buffer)
    {
        var result = new Image();

        if (buffer.Length > 0)
        {
            result.LoadPngFromBuffer(buffer);
        }

        return result;
    }
}
