using System;
using System.IO;
using System.Text;
using Godot;
using ICSharpCode.SharpZipLib.Tar;

public static class TarHelper
{
    public static void OutputEntry(TarOutputStream archive, string name, byte[] data)
    {
        var entry = TarEntry.CreateTarEntry(name);

        entry.TarHeader.Mode = Convert.ToInt32("0664", 8);

        // TODO: could fill in more of the properties

        entry.Size = data.Length;

        archive.PutNextEntry(entry);

        archive.Write(data, 0, data.Length);

        archive.CloseEntry();
    }

    public static string ReadStringEntry(TarInputStream tar, int length)
    {
        // Pre-allocate storage
        var buffer = new byte[length];
        {
            using var stream = new MemoryStream(buffer);
            tar.CopyEntryContents(stream);
        }

        return Encoding.UTF8.GetString(buffer);
    }

    public static byte[] ReadBytesEntry(TarInputStream tar, int length)
    {
        // Pre-allocate storage
        var buffer = new byte[length];
        {
            using var stream = new MemoryStream(buffer);
            tar.CopyEntryContents(stream);
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
