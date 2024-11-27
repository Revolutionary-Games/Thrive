using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Text;

/// <summary>
///   Handles calculating hashes for strings that are persistent as the default ones are (so they can't be used for
///   visual hashes)
/// </summary>
public static class PersistentStringHash
{
    private static readonly Dictionary<string, ulong> HashCache = new();
    private static readonly XxHash64 Hasher = new();

    private static byte[] encodingBuffer = new byte[256];

    /// <summary>
    ///   Gets a persistent hash for a string. Note that this uses a cache so any strings passed to this method will
    ///   *permanently* stay in memory! So don't pass in dynamically generated text, for those use
    ///   <see cref="CalculateHashWithoutCache(string)"/>.
    /// </summary>
    /// <param name="str">String to hash</param>
    /// <returns>A persistent hash for the string contents</returns>
    public static ulong GetHash(string str)
    {
        lock (HashCache)
        {
            if (HashCache.TryGetValue(str, out var result))
                return result;

            result = CalculateHashWithoutCache(str);
            HashCache.Add(str, result);
            return result;
        }
    }

    public static ulong CalculateHashWithoutCache(string str)
    {
        lock (Hasher)
        {
            int bytesWritten;

            while (true)
            {
                if (Encoding.Unicode.TryGetBytes(str, encodingBuffer, out bytesWritten))
                    break;

                // Need more space in the buffer
                encodingBuffer = new byte[encodingBuffer.Length * 2];
            }

            Hasher.Reset();

            Hasher.Append(encodingBuffer.AsSpan(0, bytesWritten));
            return Hasher.GetCurrentHashAsUInt64();
        }
    }

    public static ulong CalculateHashWithoutCache(string str1, string str2)
    {
        lock (Hasher)
        {
            var encoding = Encoding.Unicode;

            int bytesWritten1;
            int bytesWritten2;

            while (true)
            {
                // As the data is not copied (as it is assumed the buffer will usually quickly become big enough) the
                // first part needs to be encoded again into the buffer
                if (encoding.TryGetBytes(str1, encodingBuffer, out bytesWritten1))
                {
                    if (encoding.TryGetBytes(str2, encodingBuffer.AsSpan(bytesWritten1), out bytesWritten2))
                        break;
                }

                // Need more space in the buffer
                encodingBuffer = new byte[encodingBuffer.Length * 2];
            }

            Hasher.Reset();

            Hasher.Append(encodingBuffer.AsSpan(0, bytesWritten1 + bytesWritten2));
            return Hasher.GetCurrentHashAsUInt64();
        }
    }
}
