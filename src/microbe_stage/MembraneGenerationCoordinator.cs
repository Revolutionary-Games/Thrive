using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Godot;

/// <summary>
///   Coordinator that implements two-pass membrane generation.
///   - First pass: generate base membranes per cell
///   - Second pass: generate stretched multicellular membranes if needed. They are not persistently cached. Instead,
///   a finished stretched result is handed off exactly once to the main-thread request that is waiting for it
/// </summary>
public static class MembraneGenerationCoordinator
{
    private static readonly ConcurrentDictionary<long, ColonyTracker> ColonyTrackers = new();

    /// <summary>
    ///   Stores finished stretched multicellular membranes. Unlike a cache, an entry here is
    ///   removed the moment it is consumed.
    /// </summary>
    private static readonly ConcurrentDictionary<int, MembranePointData> FinishedMulticellularMembranes = new();

    /// <summary>
    ///   Handles membrane generation requests. For single-cell requests the list contains one hash.
    /// </summary>
    public static List<long> HandleGenerationRequest(ref MembraneGenerationParameters generationParameters)
    {
        var generatedMembranes = new List<long>();
        var generator = MembraneShapeGenerator.GetThreadSpecificGenerator();
        var isSingleCell = generationParameters.GrownCellsData.Length == 0 ||
            !generationParameters.IsMulticellularMembraneDataValid;

        if (isSingleCell)
        {
            var membranePointData = generator.GenerateMicrobeShape(ref generationParameters);
            var hash = ProceduralDataCache.Instance.WriteMembraneData(ref membranePointData);
            generatedMembranes.Add(hash);
            return generatedMembranes;
        }

        generationParameters.IsPreMulticellularStretch = true;

        var stretchedHash = generationParameters.ComputeMembraneDataHash();
        var singleCellMembranePointData = ProceduralDataCache.Instance.ReadMembraneData(stretchedHash);
        if (singleCellMembranePointData == null)
        {
            singleCellMembranePointData = generator.GenerateMicrobeShape(ref generationParameters, true);
            ProceduralDataCache.Instance.WriteMembraneData(ref singleCellMembranePointData);
        }
        else
        {
            // Cache hit — hexes are no longer needed, return to pool
            ArrayPool<Vector2>.Shared.Return(generationParameters.HexPositions);
        }

        var grownCellsData = generationParameters.GrownCellsData;
        var cellPosition = generationParameters.CurrentCellMulticellularMembraneGenerationCellData.Position;
        var cellOrientation = generationParameters.CurrentCellMulticellularMembraneGenerationCellData.Orientation;

        var tracker = ColonyTrackers.GetOrAdd(generationParameters.LeaderCellId,
            _ => new ColonyTracker { ExpectedCount = grownCellsData.Length });

        var multicellularMembraneData = new MulticellularMembraneGenerationCellData(cellPosition, cellOrientation);

        var singleCellData = new NeighbourData(generationParameters.CellId, multicellularMembraneData,
            singleCellMembranePointData);

        tracker.NeighboursData[CellKey(cellPosition)] = singleCellData;

        // TODO: Maybe implement a system that clears trackers every now and then that are inactive
        // for too long to avoid potential memory leaks

        GD.Print($"{generationParameters.LeaderCellId}: {tracker.NeighboursData.Count}/{tracker.ExpectedCount}");

        // Colony not yet complete — return empty
        if (tracker.NeighboursData.Count < tracker.ExpectedCount)
            return generatedMembranes;

        // Pass 2: all base membranes are ready. Use a flag to ensure exactly one thread executes the second pass.
        if (!tracker.TryBeginSecondPass())
            return generatedMembranes;

        // Return ALL resolved hashes so every cell's pending entry gets cleared
        foreach (var (key, data) in tracker.NeighboursData)
        {
            var multicellularMembrane =
                generator.GenerateMulticellularMembrane(key, tracker.NeighboursData,
                    generationParameters.LeaderCellId, generationParameters.CellId);

            AddMulticellularMembrane(data.CellId, multicellularMembrane);
            generatedMembranes.Add(data.CellId);
        }

        ColonyTrackers.TryRemove(generationParameters.LeaderCellId, out _);

        return generatedMembranes;
    }

    /// <summary>
    ///   Attempts to retrieve and remove a finished stretched multicellular membrane.
    /// </summary>
    public static bool TryTakeFinishedMulticellularMembrane(int hash, out MembranePointData? data)
    {
        return FinishedMulticellularMembranes.TryRemove(hash, out data);
    }

    /// <summary>
    ///   Disposes and removes any finished multicellular membranes that were computed but never collected,
    ///   and clears all colony trackers.
    /// </summary>
    public static void ClearCoordinator()
    {
#if DEBUG
        if (!FinishedMulticellularMembranes.IsEmpty)
        {
            GD.PrintErr("FinishedMulticellularMembranes is not empty");
        }

        if (!ColonyTrackers.IsEmpty)
        {
            GD.PrintErr("ColonyTrackers is not empty");
        }
#endif

        foreach (var key in FinishedMulticellularMembranes.Keys)
        {
            if (FinishedMulticellularMembranes.TryRemove(key, out var orphaned))
                orphaned.Dispose();
        }

        ColonyTrackers.Clear();
    }

    private static void AddMulticellularMembrane(int cellId, MembranePointData data)
    {
        if (FinishedMulticellularMembranes.TryRemove(cellId, out var previous))
        {
            GD.PrintErr("FinishedMulticellularMembranes was overwritten");
            previous.Dispose();
        }

        FinishedMulticellularMembranes[cellId] = data;
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
