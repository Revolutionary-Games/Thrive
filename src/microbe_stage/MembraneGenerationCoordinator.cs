using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Godot;

/// <summary>
///   Coordinator that implements two-pass membrane generation for multicellular bodies.
///   - First pass: generate base membranes per cell ignoring multicellular positions
///   - Second pass: when all bases for a colony exist, generate modified membranes that include the multicellular
///     adjustments and write those to the procedural cache.
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

        // Single-cell path — unchanged
        if (generationParameters.MulticellularPositions == null ||
            generationParameters.CellPositionInMulticellular == null)
        {
            var membranePointData = generator.GenerateMicrobeShape(ref generationParameters);
            var hash = ProceduralDataCache.Instance.WriteMembraneData(ref membranePointData);
            return new List<long> { hash };
        }

        var multicellularPositions = generationParameters.MulticellularPositions!;
        var cellPosition = generationParameters.CellPositionInMulticellular!.Value;

        // This is the hash the CALLER registered in pendingGenerationsOfMembraneHashes
        var registeredHash = MembraneComputationHelpers.ComputeMembraneDataHash(generationParameters.HexPositions,
            generationParameters.HexPositionCount,
            generationParameters.Type,
            multicellularPositions,
            cellPosition);

        // If the final multicellular membrane is already cached, just return the registered hash to unblock it
        var existing = ProceduralDataCache.Instance.ReadMembraneData(registeredHash);
        if (existing != null)
            return new List<long> { registeredHash };

        // Pass 1: generate the base (single-cell) shape — do NOT write this to the cache under its
        // single-cell hash, because that would pollute lookups; keep it only in NeighbourData.
        var singleCellMembranePointData = generator.GenerateMicrobeShape(ref generationParameters, true);

        var colonyKey = ComputeColonyKey(multicellularPositions);
        var tracker = Trackers.GetOrAdd(colonyKey,
            _ => new ColonyTracker { ExpectedCount = multicellularPositions.Length });

        var singleCellData = new NeighbourData
        {
            SingleCellHash = registeredHash, // store the multicell hash so we can resolve it later
            CellPosition = cellPosition,
            HexPositions = generationParameters.HexPositions,
            HexCount = generationParameters.HexPositionCount,
            Type = generationParameters.Type,
            PointData = singleCellMembranePointData,
        };

        tracker.NeighboursData[CellKey(cellPosition)] = singleCellData;

        // Colony not yet complete — tell the caller their hash is still pending (return empty)
        if (tracker.NeighboursData.Count < tracker.ExpectedCount)
            return new List<long>();

        // Pass 2: all base membranes are ready; generate multicellular-adjusted versions.
        // Use a flag to ensure exactly one thread executes the second pass.
        if (!tracker.TryBeginSecondPass())
            return new List<long>();

        var neighboursData = tracker.NeighboursData.Values.ToArray();
        var resolvedHashes = new List<long>();

        foreach (var entry in tracker.NeighboursData.Values)
        {
            var multicellularMembrane = generator.GenerateMulticellularMembrane(entry.PointData, neighboursData,
                multicellularPositions, entry.CellPosition);

            // Compute the exact hash the caller registered for this cell
            var entryRegisteredHash = MembraneComputationHelpers.ComputeMembraneDataHash(entry.HexPositions,
                entry.HexCount, entry.Type,
                multicellularPositions, entry.CellPosition);

            ProceduralDataCache.Instance.WriteMembraneData(ref multicellularMembrane);
            resolvedHashes.Add(entryRegisteredHash);
        }

        Trackers.TryRemove(colonyKey, out _);

        // Return ALL resolved hashes so every cell's pending entry gets cleared
        return resolvedHashes;
    }

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
        private int _secondPassStarted;

        /// <returns>True if THIS caller should run the second pass; false if another thread beat it</returns>
        public bool TryBeginSecondPass() =>
            Interlocked.CompareExchange(ref _secondPassStarted, 1, 0) == 0;
    }
}
