using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Godot;

/// <summary>
///   Coordinator that implements two-pass membrane generation.
///   - First pass: generate base membranes per cell
///   - Second pass: generate stetched multicellular membranes if needed.
/// </summary>
public static class MembraneGenerationCoordinator
{
    private static readonly ConcurrentDictionary<long, ColonyTracker> Trackers = new();

    /// <summary>
    ///   Handles membrane generation requests. For single-cell requests the list contains one hash.
    /// </summary>
    public static List<long> HandleGenerationRequest(ref MembraneGenerationParameters generationParameters)
    {
        var hashedMembranes = new List<long>();
        var generator = MembraneShapeGenerator.GetThreadSpecificGenerator();
        var isSingleCell = generationParameters.GrownCellsData.Length == 0 ||
            !generationParameters.IsMulticellularMembraneDataValid;

        if (isSingleCell)
        {
            var membranePointData = generator.GenerateMicrobeShape(ref generationParameters);
            var hash = ProceduralDataCache.Instance.WriteMembraneData(ref membranePointData);
            hashedMembranes.Add(hash);
            return hashedMembranes;
        }

        var registeredHash = generationParameters.ComputeMembraneDataHash();

        // If the final multicellular membrane is already cached, just return it
        var existing = ProceduralDataCache.Instance.ReadMembraneData(registeredHash);
        if (existing != null)
        {
            hashedMembranes.Add(registeredHash);
            return hashedMembranes;
        }

        generationParameters.IsPreMulticellularStretch = true;

        var stretchedHash = generationParameters.ComputeMembraneDataHash();
        var singleCellMembranePointData = ProceduralDataCache.Instance.ReadMembraneData(stretchedHash);
        if (singleCellMembranePointData == null)
        {
            singleCellMembranePointData = generator.GenerateMicrobeShape(ref generationParameters, true);
            ProceduralDataCache.Instance.WriteMembraneData(ref singleCellMembranePointData);
        }

        var grownCellsData = generationParameters.GrownCellsData;
        var cellPosition = generationParameters.CurrentCellMulticellularMembraneData.Position;
        var cellOrientation = generationParameters.CurrentCellMulticellularMembraneData.Orientation;

        // Prefer the ColonyKey provided in generationParameters if available. Otherwise compute or fetch a cached
        // colony key for this colony. generationParameters may not carry a reference to the species, so fall back
        // to computing directly if necessary.
        long colonyKey;
        if (generationParameters.IsColonyKeyValid)
        {
            colonyKey = generationParameters.ColonyKey;
        }
        else
        {
            colonyKey = ComputeColonyKey(grownCellsData);
        }

        var tracker = Trackers.GetOrAdd(colonyKey,
            _ => new ColonyTracker { ExpectedCount = grownCellsData.Length });

        var multicellularMembraneData = new MulticellularMembraneData(cellPosition, cellOrientation);

        var singleCellData = new NeighbourData(registeredHash, multicellularMembraneData, singleCellMembranePointData);

        tracker.NeighboursData[CellKey(cellPosition)] = singleCellData;

        // TODO: Maybe implement a system that clears trackers every now and then that are inactive
        // for too long to avoid potential memory leaks

        // Colony not yet complete — return empty
        if (tracker.NeighboursData.Count < tracker.ExpectedCount)
            return hashedMembranes;

        // Pass 2: all base membranes are ready. Use a flag to ensure exactly one thread executes the second pass.
        if (!tracker.TryBeginSecondPass())
            return hashedMembranes;

        // Return ALL resolved hashes so every cell's pending entry gets cleared
        foreach (var (key, data) in tracker.NeighboursData)
        {
            var multicellularMembrane =
                generator.GenerateMulticellularMembrane(key, tracker.NeighboursData, grownCellsData);

            ProceduralDataCache.Instance.WriteMembraneData(ref multicellularMembrane);
            hashedMembranes.Add(data.SingleCellHash);
        }

        Trackers.TryRemove(colonyKey, out _);

        return hashedMembranes;
    }

    public static long ComputeColonyKey(MulticellularMembraneData[] cellsData)
    {
        unchecked
        {
            const long offset = -3750763034362895579L;
            const long prime = 1099511628211L;

            long hash = offset;
            hash ^= cellsData.Length;
            hash *= prime;

            for (int i = 0; i < cellsData.Length; ++i)
            {
                var cell = cellsData[i];
                hash ^= BitConverter.SingleToInt32Bits(cell.Position.X) * prime;
                hash *= prime;
                hash ^= BitConverter.SingleToInt32Bits(cell.Position.Y) * prime;
                hash *= prime;
                hash ^= cell.Orientation * prime;
                hash *= prime;
            }

            return hash;
        }
    }

    private static long CellKey(Vector2 position)
    {
        const long prime = 1099511628211L;

        long hash = prime;
        hash ^= BitConverter.SingleToInt32Bits(position.X);
        hash *= prime;
        hash ^= BitConverter.SingleToInt32Bits(position.Y);
        hash *= prime;

        return hash;
    }

    private class ColonyTracker
    {
        public int ExpectedCount;
        public ConcurrentDictionary<long, NeighbourData> NeighboursData = new();
        private int secondPassStarted;

        /// <summary>
        ///   Returns true if the current thread should run the second pass
        ///   and false if another thread already runs the second pass
        /// </summary>
        public bool TryBeginSecondPass()
        {
            return Interlocked.CompareExchange(ref secondPassStarted, 1, 0) == 0;
        }
    }
}
