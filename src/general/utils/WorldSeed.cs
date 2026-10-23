using System;
using System.Buffers.Binary;
using System.IO.Hashing;

/// <summary>
///   Derives private random sources from the selected world seed, without shared state or external entropy.
/// </summary>
public static class WorldSeed
{
    /// <summary>
    ///   Stable purpose identifiers. Never renumber or reuse existing values when adding another source.
    /// </summary>
    public enum Domain : uint
    {
        WorldEvents = 1,
        NitrogenControl = 2,
    }

    /// <summary>
    ///   Hashes exactly 12 bytes: the signed 64-bit world seed in two's complement, then the unsigned 32-bit
    ///   domain, both little-endian. Uses xxHash64 with hash seed 0 and its modulo-2^64 arithmetic; the result
    ///   is reinterpreted as a signed 64-bit RNG seed. Zero and negative seeds are ordinary inputs.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This encoding and the existing domain values are a stable contract, covered by fixed vectors. Future
    ///     domains may define additional fixed-width identity fields appended in a documented order, without
    ///     changing this encoding. Finite 64-bit results do not guarantee collision-free input combinations.
    ///   </para>
    /// </remarks>
    public static long Derive(long worldSeed, Domain domain)
    {
        Span<byte> input = stackalloc byte[12];
        BinaryPrimitives.WriteInt64LittleEndian(input, worldSeed);
        BinaryPrimitives.WriteUInt32LittleEndian(input[8..], (uint)domain);
        return unchecked((long)XxHash64.HashToUInt64(input));
    }
}
