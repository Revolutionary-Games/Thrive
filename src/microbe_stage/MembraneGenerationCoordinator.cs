using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    private static readonly ConcurrentDictionary<long, MembranePointData> FinishedMulticellularMembranes = new();

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
        var cellData = generationParameters.CurrentCellMulticellularMembraneGenerationCellData;

        return CoordinateMulticellularGeneration(generator, singleCellMembranePointData,
            generationParameters.LeaderCellKey, generationParameters.CellKey, cellData, grownCellsData.Length);
    }

    /// <summary>
    ///   Coordinates the second pass of multicellular membrane generation for a colony batch.
    ///   If the current tracker has already started its second pass, the request is routed to a new tracker
    ///   so late cells are never stranded or mutate an in-flight batch.
    /// </summary>
    public static List<long> CoordinateMulticellularGeneration(MembraneShapeGenerator generator,
        MembranePointData singleCellMembranePointData, long leaderCellKey, long cellKey,
        MulticellularMembraneGenerationCellData cellData, int expectedCellCount)
    {
        var generatedMembranes = new List<long>();
        var generationData = new MembraneShapeStretcher.CellGenerationData(singleCellMembranePointData, cellData);

        ColonyTracker? completedTracker = null;

        while (true)
        {
            var tracker = ColonyTrackers.GetOrAdd(leaderCellKey,
                _ => new ColonyTracker { ExpectedCount = expectedCellCount });

            if (!tracker.TryAddCell(leaderCellKey, cellKey, generationData, expectedCellCount,
                    out var readyForSecondPass))
            {
                // This tracker already started its second pass; retry with a new tracker
                continue;
            }

            if (!readyForSecondPass)
                return generatedMembranes;

            completedTracker = tracker;
            break;
        }

        // TODO: Maybe implement a system that clears trackers every now and then that are inactive
        // for too long to avoid potential memory leaks

        var stretchedMembranes = generator.GenerateMulticellularMembrane(completedTracker.CellsData);

        foreach (var (key, membraneData) in stretchedMembranes)
        {
            AddMulticellularMembrane(key, membraneData);
            generatedMembranes.Add(key);
        }

        stretchedMembranes.Clear();

        return generatedMembranes;
    }

    /// <summary>
    ///   Attempts to retrieve and remove a finished stretched multicellular membrane.
    /// </summary>
    public static MembranePointData? TryTakeFinishedMulticellularMembrane(long key)
    {
        FinishedMulticellularMembranes.TryRemove(key, out var data);
        return data;
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

    private static void AddMulticellularMembrane(long cellKey, MembranePointData data)
    {
        if (FinishedMulticellularMembranes.TryRemove(cellKey, out var previous))
        {
            GD.PrintErr("FinishedMulticellularMembranes was overwritten");
            previous.Dispose();
        }

        FinishedMulticellularMembranes[cellKey] = data;
    }

    public class ColonyTracker
    {
        public int ExpectedCount;
        public ConcurrentDictionary<long, MembraneShapeStretcher.CellGenerationData> CellsData = new();
        private readonly object trackerLock = new();
        private bool secondPassStarted;

        /// <summary>
        ///   Adds cell data to this tracker if the second pass has not started yet. Returns false if the second
        ///   pass has already begun so the caller can retry with a fresh tracker.
        /// </summary>
        public bool TryAddCell(long leaderCellKey, long cellKey,
            MembraneShapeStretcher.CellGenerationData cellGenerationData, int expectedCount,
            out bool readyForSecondPass)
        {
            lock (trackerLock)
            {
                if (secondPassStarted)
                {
                    readyForSecondPass = false;
                    return false;
                }

                if (expectedCount > ExpectedCount)
                    ExpectedCount = expectedCount;

                CellsData[cellKey] = cellGenerationData;

                if (CellsData.Count >= ExpectedCount)
                {
                    secondPassStarted = true;
                    readyForSecondPass = true;
                    ColonyTrackers.TryRemove(new KeyValuePair<long, ColonyTracker>(leaderCellKey, this));
                    return true;
                }

                readyForSecondPass = false;
                return true;
            }
        }
    }
}
