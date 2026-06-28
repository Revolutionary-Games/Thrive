using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Godot;

/// <summary>
///   Coordinator that implements two-pass membrane generation for multicellular bodies.
///   - First pass: generate base membranes per cell
///   - Second pass: generate modified membranes that include the multicellular adjustments.
/// </summary>
public static class MembraneGenerationCoordinator
{
    private static readonly ConcurrentDictionary<long, ColonyTracker> Trackers = new();

    /// <summary>
    ///   Handles membrane generation requests. For single-cell
    ///   requests the list contains one hash.
    /// </summary>
    public static List<long> HandleGenerationRequest(ref MembraneGenerationParameters generationParameters)
    {
        var generator = MembraneShapeGenerator.GetThreadSpecificGenerator();
        var isSingleCell = generationParameters.MulticellularPositions == null ||
            generationParameters.CellPositionInMulticellular == null;

        if (isSingleCell)
        {
            var membranePointData = generator.GenerateMicrobeShape(ref generationParameters);
            var hash = ProceduralDataCache.Instance.WriteMembraneData(ref membranePointData);
            return new List<long> { hash };
        }

        var multicellularPositions = generationParameters.MulticellularPositions!;
        var multicellularOrientations = generationParameters.MulticellularOrientations;
        var cellPosition = generationParameters.CellPositionInMulticellular!.Value;
        var cellOrientation = generationParameters.CellOrientation;

        var registeredHash = MembraneComputationHelpers.ComputeMembraneDataHash(
            positions: generationParameters.HexPositions,
            count: generationParameters.HexPositionCount,
            type: generationParameters.Type,
            multicellularPositions: multicellularPositions,
            cellPositionInMulticellular: cellPosition,
            multicellularOrientations: multicellularOrientations,
            cellOrientation: cellOrientation);

        // If the final multicellular membrane is already cached, just return it
        var existing = ProceduralDataCache.Instance.ReadMembraneData(registeredHash);
        if (existing != null)
            return new List<long> { registeredHash };

        // TODO: this should be written into cache!
        // Pass 1: generate the base (single-cell) shape — do NOT write this to the cache under its
        // single-cell hash, because that would pollute lookups; keep it only in NeighbourData.
        var singleCellMembranePointData = generator.GenerateMicrobeShape(ref generationParameters, true);

        var colonyKey = ComputeColonyKey(multicellularPositions);
        var tracker = Trackers.GetOrAdd(colonyKey,
            _ => new ColonyTracker { ExpectedCount = multicellularPositions.Length });

        var singleCellData = new NeighbourData
        {
            SingleCellHash = registeredHash,
            CellPosition = cellPosition,
            OriginalPointData = singleCellMembranePointData,
            Orientation = cellOrientation ?? 0,
        };

        tracker.NeighboursData[CellKey(cellPosition)] = singleCellData;

        // Colony not yet complete — return empty
        if (tracker.NeighboursData.Count < tracker.ExpectedCount)
            return new List<long>();

        // TODO: maybe allow it to be multithreaded!
        // Pass 2: all base membranes are ready; generate multicellular-adjusted versions.
        // Use a flag to ensure exactly one thread executes the second pass.
        if (!tracker.TryBeginSecondPass())
            return new List<long>();

        var resolvedHashes = new List<long>();

        foreach (var (key, data) in tracker.NeighboursData)
        {
            var multicellularMembrane = generator.GenerateMulticellularMembrane(key, tracker.NeighboursData,
                multicellularPositions, multicellularOrientations);

            ProceduralDataCache.Instance.WriteMembraneData(ref multicellularMembrane);
            resolvedHashes.Add(data.SingleCellHash);
        }

        Trackers.TryRemove(colonyKey, out _);

        // Return ALL resolved hashes so every cell's pending entry gets cleared
        return resolvedHashes;
    }

    // TODO: is there a better way to calculate the key? maybe just some hash instead of string key...
    private static string CellKey(Vector2 v)
    {
        return BitConverter.SingleToInt32Bits(v.X) + ":" + BitConverter.SingleToInt32Bits(v.Y);
    }

    private static long ComputeColonyKey(Vector2[] positions)
    {
        unchecked
        {
            const long offset = -3750763034362895579L;
            const long prime = 1099511628211L;

            long hash = offset;
            hash ^= positions.Length;
            hash *= prime;

            for (int i = 0; i < positions.Length; ++i)
            {
                hash ^= BitConverter.SingleToInt32Bits(positions[i].X) * prime;
                hash *= prime;
                hash ^= BitConverter.SingleToInt32Bits(positions[i].Y) * prime;
                hash *= prime;
            }

            return hash;
        }
    }

    private class ColonyTracker
    {
        public int ExpectedCount;
        public ConcurrentDictionary<string, NeighbourData> NeighboursData = new();
        private int secondPassStarted;

        /// <returns>True if THIS caller should run the second pass; false if another thread beat it</returns>
        public bool TryBeginSecondPass()
        {
            return Interlocked.CompareExchange(ref secondPassStarted, 1, 0) == 0;
        }
    }
}
