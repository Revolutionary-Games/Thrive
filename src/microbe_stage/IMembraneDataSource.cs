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

    public Vector2[]? MulticellularPositions { get; }

    public Vector2? CellPositionInMulticellular { get; }

    // Optional per-cell orientations for multicellular arrangements. Values are 0..5 (hex rotations).
    public int[]? MulticellularOrientations { get; }

    // Orientation of this cell within the multicellular arrangement (0..5). Null for single-cell generation.
    public int? CellOrientation { get; }
    public MembraneType Type { get; }
}

/// <summary>
///   Struct that holds parameters about a membrane generation request
/// </summary>
public struct MembraneGenerationParameters : IMembraneDataSource
{
    public MembraneGenerationParameters(Vector2[] hexPositions, int hexPositionCount, MembraneType type,
        Vector2[] multicellularPositions, Vector2 thisCellPosition, int[]? multicellularOrientations,
        int? thisCellOrientation)
        : this(hexPositions, hexPositionCount, type)
    {
        MulticellularPositions = multicellularPositions;
        CellPositionInMulticellular = thisCellPosition;
        MulticellularOrientations = multicellularOrientations;
        CellOrientation = thisCellOrientation;
    }

    public MembraneGenerationParameters(Vector2[] hexPositions, int hexPositionCount, MembraneType type)
    {
        HexPositions = hexPositions;
        HexPositionCount = hexPositionCount;
        Type = type;
    }

    public int[]? MulticellularOrientations { get; }

    public int? CellOrientation { get; }

    public Vector2[] HexPositions { get; }

    public Vector2[]? MulticellularPositions { get; }

    public Vector2? CellPositionInMulticellular { get; }

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
            // Return the no longer necessary hex positions to the cache (when we need to generate new data, the hexes
            // will get owned by the cache entry)
            ArrayPool<Vector2>.Shared.Return(hexes);

            return result;
        }

        // Need to compute the data now, it doesn't exist in the cache
        var generator = MembraneShapeGenerator.GetThreadSpecificGenerator();

        result = generator.GenerateMicrobeShape(hexes, length, membraneType, false);

        cache.WriteMembraneData(ref result);
        return result;
    }

    public static long ComputeMembraneDataHash(Vector2[] positions, int count, MembraneType type,
        Vector2[]? multicellularPositions = null, Vector2? cellPositionInMulticellular = null,
        int[]? multicellularOrientations = null, int? cellOrientation = null)
    {
        const long prime1 = 1099511628211L;
        const long prime2 = 1409;
        const long prime3 = 7793;

        var nameHash = type.InternalName.GetHashCode();

        unchecked
        {
            long hash = prime2 + nameHash + ((long)nameHash << 28);

            hash ^= (count + 1) * prime3;

            for (int i = 0; i < count; ++i)
            {
                hash = (hash * prime1) ^ BitConverter.SingleToInt32Bits(positions[i].X);
                hash = (hash * prime1) ^ BitConverter.SingleToInt32Bits(positions[i].Y);
            }

            if (cellPositionInMulticellular != null)
            {
                hash = (hash * prime1) ^ BitConverter.SingleToInt32Bits(cellPositionInMulticellular.Value.X);
                hash = (hash * prime1) ^ BitConverter.SingleToInt32Bits(cellPositionInMulticellular.Value.Y);
            }

            if (multicellularPositions != null)
            {
                for (int i = 0; i < multicellularPositions.Length; ++i)
                {
                    hash = (hash * prime1) ^ BitConverter.SingleToInt32Bits(multicellularPositions[i].X);
                    hash = (hash * prime1) ^ BitConverter.SingleToInt32Bits(multicellularPositions[i].Y);
                }
            }

            if (cellOrientation != null)
            {
                hash = (hash * prime1) ^ cellOrientation.Value;
            }

            if (multicellularOrientations != null)
            {
                for (int i = 0; i < multicellularOrientations.Length; ++i)
                {
                    hash = (hash * prime1) ^ multicellularOrientations[i];
                }
            }

            return hash;
        }
    }

    public static bool MembraneDataFieldsEqual(this IMembraneDataSource dataSource, IMembraneDataSource other)
    {
        return dataSource.MembraneDataFieldsEqual(other.HexPositions, other.HexPositionCount, other.Type,
            other.MulticellularPositions, other.CellPositionInMulticellular, other.MulticellularOrientations,
            other.CellOrientation);
    }

    public static bool MembraneDataFieldsEqual(this IMembraneDataSource dataSource, Vector2[] otherPoints,
        int otherPointCount, MembraneType otherType, Vector2[]? multicellularPositions,
        Vector2? cellPositionInMulticellular, int[]? multicellularOrientations = null,
        int? cellOrientationInMulticellular = null)
    {
        if (!dataSource.Type.Equals(otherType))
        {
            GD.Print($"Type: {dataSource.Type} != {otherType}");
            return false;
        }

        if (dataSource.HexPositionCount != otherPointCount)
        {
            GD.Print($"HexPositionCount: {dataSource.HexPositionCount} != {otherPointCount}");
            return false;
        }

        var count = dataSource.HexPositionCount;

        var sourcePoints = dataSource.HexPositions;

        // Compare multicellular positions array if either side has it
        if (dataSource.MulticellularPositions != null || multicellularPositions != null)
        {
            if (dataSource.MulticellularPositions == null || multicellularPositions == null)
            {
                GD.Print($"MulticellularPositions null mismatch: source={dataSource.MulticellularPositions == null} " +
                    $"other={multicellularPositions == null}");
                return false;
            }

            if (dataSource.MulticellularPositions.Length != multicellularPositions.Length)
            {
                GD.Print($"MulticellularPositions.Length: {dataSource.MulticellularPositions.Length} != " +
                    $"{multicellularPositions.Length}");
                return false;
            }

            for (int i = 0; i < dataSource.MulticellularPositions.Length; ++i)
            {
                if (dataSource.MulticellularPositions[i] != multicellularPositions[i])
                {
                    GD.Print($"MulticellularPositions[{i}]: {dataSource.MulticellularPositions[i]} != " +
                        $"{multicellularPositions[i]}");
                    return false;
                }
            }
        }

        // Compare multicellular orientations if either side has them
        if (dataSource.MulticellularOrientations != null || multicellularOrientations != null)
        {
            if (dataSource.MulticellularOrientations == null || multicellularOrientations == null)
            {
                GD.Print(
                    $"MulticellularOrientations null mismatch: source={dataSource.MulticellularOrientations == null} " +
                    $"other={multicellularOrientations == null}");
                return false;
            }

            if (dataSource.MulticellularOrientations.Length != multicellularOrientations.Length)
            {
                GD.Print($"MulticellularOrientations.Length: {dataSource.MulticellularOrientations.Length} != " +
                    $"{multicellularOrientations.Length}");
                return false;
            }

            for (int i = 0; i < dataSource.MulticellularOrientations.Length; ++i)
            {
                if (dataSource.MulticellularOrientations[i] != multicellularOrientations[i])
                {
                    GD.Print($"MulticellularOrientations[{i}]: {dataSource.MulticellularOrientations[i]} != " +
                        $"{multicellularOrientations[i]}");
                    return false;
                }
            }
        }

        if (dataSource.CellPositionInMulticellular != null)
        {
            if (cellPositionInMulticellular == null)
            {
                GD.Print(
                    $"CellPositionInMulticellular null mismatch: source={dataSource.CellPositionInMulticellular} " +
                    $"other=null");
                return false;
            }

            if (!dataSource.CellPositionInMulticellular.Equals(cellPositionInMulticellular))
            {
                GD.Print($"CellPositionInMulticellular: {dataSource.CellPositionInMulticellular} != " +
                    $"{cellPositionInMulticellular}");
                return false;
            }
        }
        else
        {
            if (cellPositionInMulticellular != null)
            {
                GD.Print($"CellPositionInMulticellular null mismatch: source=null other={cellPositionInMulticellular}");
                return false;
            }
        }

        // Compare cell orientation
        if (dataSource.CellOrientation != null)
        {
            if (cellOrientationInMulticellular == null)
            {
                GD.Print($"CellOrientation null mismatch: source={dataSource.CellOrientation} other=null");
                return false;
            }

            if (!dataSource.CellOrientation.Equals(cellOrientationInMulticellular))
            {
                GD.Print($"CellOrientation: {dataSource.CellOrientation} != {cellOrientationInMulticellular}");
                return false;
            }
        }
        else
        {
            if (cellOrientationInMulticellular != null)
            {
                GD.Print($"CellOrientation null mismatch: source=null other={cellOrientationInMulticellular}");
                return false;
            }
        }

        for (int i = 0; i < count; ++i)
        {
            if (sourcePoints[i] != otherPoints[i])
            {
                GD.Print($"HexPositions[{i}]: {sourcePoints[i]} != {otherPoints[i]}");
                return false;
            }
        }

        return true;
    }

    public static long ComputeMembraneDataHash(this IMembraneDataSource dataSource)
    {
        return ComputeMembraneDataHash(dataSource.HexPositions, dataSource.HexPositionCount, dataSource.Type,
            dataSource.MulticellularPositions, dataSource.CellPositionInMulticellular,
            dataSource.MulticellularOrientations,
            dataSource.CellOrientation);
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
