using System;
using System.Buffers;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Access to membrane properties that are needed for generating and caching generated membrane shapes
/// </summary>
public interface IMembraneDataSource
{
    public Vector2[] HexPositions { get; }
    public int HexPositionCount { get; }
    public MembraneType Type { get; }
}

/// <summary>
///   Struct that holds parameters about a membrane generation request
/// </summary>
public struct MembraneGenerationParameters : IMembraneDataSource
{
    public MembraneGenerationParameters(Vector2[] hexPositions, int hexPositionCount, MembraneType type)
    {
        HexPositions = hexPositions;
        HexPositionCount = hexPositionCount;
        Type = type;
    }

    public Vector2[] HexPositions { get; }
    public int HexPositionCount { get; }

    public MembraneType Type { get; }
}

/// <summary>
///   Helpers related to the source data needed to be fed into the membrane generation algorithm
/// </summary>
public static class MembraneComputationHelpers
{
    private static readonly HexPositionComparer HexComparer = new();

    public static Vector2[] PrepareHexPositionsForMembraneCalculations(IReadOnlyList<IPositionedOrganelle> organelles,
        out int length)
    {
        // First calculate needed size and then allocate the memory and fill it
        length = 0;

        var collectionCount = organelles.Count;

        for (int i = 0; i < collectionCount; ++i)
        {
            length += organelles[i].Definition.HexCount;
        }

        var result = ArrayPool<Vector2>.Shared.Rent(length);
        int resultWriteIndex = 0;

        for (int i = 0; i < collectionCount; ++i)
        {
            // The membrane needs hex positions (rather than organelle positions) to handle cells with multihex
            // organelles
            var entry = organelles[i];

            var rotatedHexes = entry.Definition.GetRotatedHexes(entry.Orientation);
            int hexCount = rotatedHexes.Count;

            // Manual loop to reduce memory allocations in this often called method
            for (int j = 0; j < hexCount; ++j)
            {
                var hexCartesian = Hex.AxialToCartesian(entry.Position + rotatedHexes[j]);
                result[resultWriteIndex++] = new Vector2(hexCartesian.X, hexCartesian.Z);
            }
        }

        if (resultWriteIndex != length)
            throw new Exception("Logic error in membrane hex position copy");

        // Points are sorted to ensure same shape but different order of organelles results in reusable data
        // TODO: check if this is actually a good idea or it is better to not sort and let duplicate membrane data
        // just be generated. Also this seems to allocate memory a bit.
        Array.Sort(result, 0, length, HexComparer);

        return result;
    }

    public static MembranePointData GetOrComputeMembraneShape(IReadOnlyList<IPositionedOrganelle> organelles,
        MembraneType membraneType)
    {
        var hexes = PrepareHexPositionsForMembraneCalculations(organelles, out var length);

        var cache = ProceduralDataCache.Instance;

        var hash = ComputeMembraneDataHash(hexes, length, membraneType);

        var result = cache.ReadMembraneData(hash);

        if (result != null)
        {
            // Return the no longer needed hex positions to the cache (when we need to generate new data, the hexes
            // will get owned by the cache entry)
            ArrayPool<Vector2>.Shared.Return(hexes);

            return result;
        }

        // Need to compute the data now, it doesn't exist in the cache
        // TODO: https://github.com/Revolutionary-Games/Thrive/issues/4989
        var generator = MembraneShapeGenerator.GetThreadSpecificGenerator();

        lock (generator)
        {
            result = generator.GenerateShape(hexes, length, membraneType);
        }

        cache.WriteMembraneData(result);
        return result;
    }

    public static long ComputeMembraneDataHash(Vector2[] positions, int count, MembraneType type)
    {
        var nameHash = type.InternalName.GetHashCode();

        unchecked
        {
            long hash = 1409 + nameHash + ((long)nameHash << 28);

            hash ^= (count + 1) * 7793;
            int hashMultiply = 1;

            for (int i = 0; i < count; ++i)
            {
                var posHash = positions[i].GetHashCode();

                // TODO: switch to using rotate left here once we can (after Godot 4)
                hash ^= (hashMultiply * posHash) ^ ((5081L * hashMultiply * hashMultiply + posHash) << 32);
                ++hashMultiply;
            }

            return hash;
        }
    }

    public static long ComputeMembraneDataHash(this IMembraneDataSource dataSource)
    {
        return ComputeMembraneDataHash(dataSource.HexPositions, dataSource.HexPositionCount, dataSource.Type);
    }

    public static bool MembraneDataFieldsEqual(this IMembraneDataSource dataSource, IMembraneDataSource other)
    {
        return dataSource.MembraneDataFieldsEqual(other.HexPositions, other.HexPositionCount, other.Type);
    }

    public static bool MembraneDataFieldsEqual(this IMembraneDataSource dataSource, Vector2[] otherPoints,
        int otherPointCount, MembraneType otherType)
    {
        if (!dataSource.Type.Equals(otherType))
            return false;

        if (dataSource.HexPositionCount != otherPointCount)
            return false;

        var count = dataSource.HexPositionCount;

        var sourcePoints = dataSource.HexPositions;

        for (int i = 0; i < count; ++i)
        {
            if (sourcePoints[i] != otherPoints[i])
                return false;
        }

        return true;
    }

    private class HexPositionComparer : IComparer<Vector2>
    {
        public int Compare(Vector2 first, Vector2 second)
        {
            var xComparison = first.X.CompareTo(second.X);
            if (xComparison != 0)
                return xComparison;

            return first.Y.CompareTo(second.Y);
        }
    }
}
