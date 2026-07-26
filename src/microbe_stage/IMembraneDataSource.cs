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
    public int[]? MulticellularOrientations { get; }
    public int? CellOrientation { get; }
    public long? ColonyKey { get; }
    public MembraneType Type { get; }
    public bool IsPreMulticellularStretch { get; }
}

/// <summary>
///   Struct that holds parameters about a membrane generation request
/// </summary>
public struct MembraneGenerationParameters : IMembraneDataSource
{
    public MembraneGenerationParameters(Vector2[] hexPositions, int hexPositionCount, MembraneType type,
        Vector2[] multicellularPositions, Vector2 thisCellPosition, int[]? multicellularOrientations,
        int? thisCellOrientation, long? colonyKey = null, bool isPreMulticellularStretch = false)
        : this(hexPositions, hexPositionCount, type)
    {
        MulticellularPositions = multicellularPositions;
        CellPositionInMulticellular = thisCellPosition;
        MulticellularOrientations = multicellularOrientations;
        CellOrientation = thisCellOrientation;
        ColonyKey = colonyKey;
        IsPreMulticellularStretch = isPreMulticellularStretch;
    }

    public MembraneGenerationParameters(Vector2[] hexPositions, int hexPositionCount, MembraneType type)
    {
        HexPositions = hexPositions;
        HexPositionCount = hexPositionCount;
        Type = type;
    }

    public int[]? MulticellularOrientations { get; }

    public int? CellOrientation { get; }

    public long? ColonyKey { get; }

    public Vector2[] HexPositions { get; }

    public Vector2[]? MulticellularPositions { get; }

    public Vector2? CellPositionInMulticellular { get; }

    public int HexPositionCount { get; }

    public MembraneType Type { get; }

    public bool IsPreMulticellularStretch { get; set; }
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

        var hash = new MembraneGenerationParameters(hexes, length, membraneType).ComputeMembraneDataHash();

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

        result = generator.GenerateMicrobeShape(hexes, length, membraneType);

        cache.WriteMembraneData(ref result);
        return result;
    }

    public static long ComputeMembraneDataHash(this IMembraneDataSource dataSource)
    {
        const long prime1 = 1099511628211L;
        const long prime2 = 1409;
        const long prime3 = 7793;

        var nameHash = dataSource.Type.InternalName.GetHashCode();

        unchecked
        {
            long hash = prime2 + nameHash + ((long)nameHash << 28);

            hash ^= (dataSource.HexPositionCount + 1) * prime3;

            for (int i = 0; i < dataSource.HexPositionCount; ++i)
            {
                hash = (hash * prime1) ^ BitConverter.SingleToInt32Bits(dataSource.HexPositions[i].X);
                hash = (hash * prime1) ^ BitConverter.SingleToInt32Bits(dataSource.HexPositions[i].Y);
            }

            // NOTE: ColonyKey alone is the same for every cell in a colony, so it cannot be the only thing
            // distinguishing membrane data between cells that share a hex layout. The per-cell position/orientation
            // must also be part of the hash.
            if (dataSource.CellPositionInMulticellular != null)
            {
                hash = (hash * prime1) ^
                    BitConverter.SingleToInt32Bits(dataSource.CellPositionInMulticellular.Value.X);
                hash = (hash * prime1) ^
                    BitConverter.SingleToInt32Bits(dataSource.CellPositionInMulticellular.Value.Y);
            }

            if (dataSource.CellOrientation != null)
            {
                hash = (hash * prime1) ^ dataSource.CellOrientation.Value;
            }

            if (dataSource.ColonyKey != null)
            {
                hash = (hash * prime1) ^ dataSource.ColonyKey.Value;
            }

            if (dataSource.IsPreMulticellularStretch)
            {
                hash = (hash * prime1) ^ 1;
            }

            return hash;
        }
    }

    public static bool MembraneDataFieldsEqual(this IMembraneDataSource dataSource, IMembraneDataSource other)
{
    return dataSource.MembraneDataFieldsEqual(other.HexPositions, other.HexPositionCount, other.Type,
        other.CellPositionInMulticellular, other.CellOrientation, other.ColonyKey);
}

public static bool MembraneDataFieldsEqual(this IMembraneDataSource dataSource, Vector2[] otherPoints,
    int otherPointCount, MembraneType otherType, Vector2? cellPositionInMulticellular = null,
    int? cellOrientationInMulticellular = null, long? otherColonyKey = null)
{
    if (!dataSource.Type.Equals(otherType))
    {
        GD.PrintErr($"Membrane cache Type mismatch: {dataSource.Type} != {otherType}");
        return false;
    }

    if (dataSource.HexPositionCount != otherPointCount)
    {
        GD.PrintErr("Membrane cache HexPositionCount mismatch: " +
            $"{dataSource.HexPositionCount} != {otherPointCount}");
        return false;
    }

    var count = dataSource.HexPositionCount;

    var sourcePoints = dataSource.HexPositions;

    // Per-cell position must match — this is what actually distinguishes cells within the same colony
    if (dataSource.CellPositionInMulticellular != null || cellPositionInMulticellular != null)
    {
        if (dataSource.CellPositionInMulticellular == null || cellPositionInMulticellular == null)
        {
            GD.PrintErr("Membrane cache CellPositionInMulticellular null mismatch: " +
                $"source={dataSource.CellPositionInMulticellular == null} " +
                $"other={cellPositionInMulticellular == null}");
            return false;
        }

        if (!dataSource.CellPositionInMulticellular.Value.Equals(cellPositionInMulticellular.Value))
        {
            GD.PrintErr("Membrane cache CellPositionInMulticellular mismatch: " +
                $"{dataSource.CellPositionInMulticellular} != {cellPositionInMulticellular}");
            return false;
        }
    }

    if (dataSource.CellOrientation != null || cellOrientationInMulticellular != null)
    {
        if (dataSource.CellOrientation == null || cellOrientationInMulticellular == null)
        {
            GD.PrintErr("Membrane cache CellOrientation null mismatch: " +
                $"source={dataSource.CellOrientation == null} " +
                $"other={cellOrientationInMulticellular == null}");
            return false;
        }

        if (dataSource.CellOrientation.Value != cellOrientationInMulticellular.Value)
        {
            GD.PrintErr("Membrane cache CellOrientation mismatch: " +
                $"{dataSource.CellOrientation} != {cellOrientationInMulticellular}");
            return false;
        }
    }

    if (dataSource.ColonyKey != null || otherColonyKey != null)
    {
        if (dataSource.ColonyKey == null || otherColonyKey == null)
        {
            GD.PrintErr("Membrane cache ColonyKey null mismatch: " +
                $"source={dataSource.ColonyKey == null} other={otherColonyKey == null}");
            return false;
        }

        if (dataSource.ColonyKey.Value != otherColonyKey.Value)
        {
            GD.PrintErr(
                $"Membrane cache ColonyKey mismatch: {dataSource.ColonyKey.Value} != {otherColonyKey.Value}");
            return false;
        }
    }

    for (int i = 0; i < count; ++i)
    {
        if (sourcePoints[i] != otherPoints[i])
        {
            GD.PrintErr($"Membrane cache HexPositions[{i}] mismatch: " +
                $"{sourcePoints[i]} != {otherPoints[i]}");
            return false;
        }
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
