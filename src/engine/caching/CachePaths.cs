using System;
using System.Text;
using Godot;

/// <summary>
///   Helper methods for parsing and generating cache paths
/// </summary>
public static class CachePaths
{
    private static readonly byte[] PngExtensionRaw = ".png"u8.ToArray();
    private static readonly byte[] ImageCacheFolderRaw = Encoding.UTF8.GetBytes(Constants.CACHE_IMAGES_FOLDER + '/');

    /// <summary>
    ///   Generates a cache path based on the key and item type
    /// </summary>
    /// <returns>Generated path in the form of "user://cache/folder/a/bcdef.thing"</returns>
    public static string GenerateCachePath(ulong cacheKey, CacheItemType type, byte[] workMemory)
    {
        byte[] extension;
        byte[] pathPrefix;

        switch (type)
        {
            case CacheItemType.Png:
                extension = PngExtensionRaw;
                pathPrefix = ImageCacheFolderRaw;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        if (!BitConverter.TryWriteBytes(workMemory.AsSpan(), cacheKey))
            throw new Exception("Byte write failed for building cache path");

        HexEncoding.EncodeToHexInPlace(workMemory, sizeof(ulong), out var bytesWritten);

        // Add path prefix based on the type of the storage folder, this is done here so that only a single string
        // conversion is required
        int insertLength = pathPrefix.Length;

        for (int i = insertLength * 2; i >= insertLength; --i)
        {
            workMemory[i] = workMemory[i - insertLength];
        }

        pathPrefix.CopyTo(workMemory, 0);
        bytesWritten += insertLength;

        // Put the path separator into the raw data
        // It is assumed that the encoding here doesn't have multibyte codepoints in it, or at least not at the
        // start, otherwise this would not be safe to do
        // Make room for the separator
        for (int i = bytesWritten; i > 1 + insertLength; --i)
        {
            workMemory[i] = workMemory[i - 1];
        }

        // And then insert the separator as a single character leaving one character between
        workMemory[1 + insertLength] = (byte)'/';
        ++bytesWritten;

        // And to complete the path, add the extension
        extension.CopyTo(workMemory, bytesWritten);
        bytesWritten += extension.Length;

        // So that we can get by with just a single string conversion and allocation here
        return Encoding.UTF8.GetString(workMemory, 0, bytesWritten);
    }

    /// <summary>
    ///   Parses a cache path back into a hash (the reverse of <see cref="GenerateCachePath"/>)
    /// </summary>
    /// <param name="cachePath">Path to parse</param>
    /// <param name="prefixSkip">
    ///   Needs to be enough characters to not parse the general part of the path but only after the hash part starts,
    ///   otherwise an error will occur
    /// </param>
    /// <param name="workMemory">Temporary memory to hold path data while parsing, should be at least 128 bytes</param>
    /// <param name="throwOnError">If set to false then will return 0 on error instead of throwing</param>
    /// <returns>Parsed hash of the path</returns>
    /// <exception cref="Exception">If path format is invalid (only if <see cref="throwOnError"/>)</exception>
    public static ulong ParseCachePath(string cachePath, int prefixSkip, byte[] workMemory, bool throwOnError = true)
    {
        int writeIndex = 0;

        bool startFound = false;
        int start = 0;

        for (int i = prefixSkip; i < cachePath.Length; ++i)
        {
            var character = cachePath[i];

            if (character is '/' or '\\' or '.')
            {
                writeIndex += Encoding.UTF8.GetBytes(cachePath, start, i - start, workMemory, writeIndex);
                startFound = false;

                // We don't allow any extra dots in filenames
                if (character == '.')
                    break;

                continue;
            }

            if (!startFound)
            {
                startFound = true;
                start = i;
            }
        }

        if (startFound)
        {
            writeIndex += Encoding.UTF8.GetBytes(cachePath, start, cachePath.Length - start, workMemory, writeIndex);
        }

        if (writeIndex < 1)
        {
            if (throwOnError)
            {
                throw new Exception("Failed to find any cache path to decode");
            }

            GD.PrintErr($"Failed to find any parts in path to decode: {cachePath}");
            return 0;
        }

        if (!HexEncoding.DecodeFromHexInPlace(workMemory, writeIndex, out var bytesWritten))
        {
            if (throwOnError)
            {
                throw new Exception("Failed to decode cache path");
            }

            GD.PrintErr($"Failed to decode hash from path: {cachePath}");
            return 0;
        }

        if (bytesWritten != sizeof(ulong))
            GD.PrintErr("Hex decode didn't read expected byte count");

        // return BitConverter.ToUInt64(workMemory.AsSpan(0, bytesWritten));
        return BitConverter.ToUInt64(workMemory, 0);
    }
}
