namespace Systems;

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Generates the visuals needed for microbes. Handles the membrane and organelle graphics. Attaching to the
///   Godot scene tree is handled by <see cref="SpatialAttachSystem"/>
/// </summary>
[RunsBefore(typeof(SpatialAttachSystem))]
[RunsBefore(typeof(EntityMaterialFetchSystem))]
[RunsBefore(typeof(SpatialPositionSystem))]
[RuntimeCost(5)]
[RunsOnMainThread]
public partial class MicrobeVisualsSystem : BaseSystem<World, float>
{
    private readonly Lazy<PackedScene> membraneScene =
        new(() => GD.Load<PackedScene>("res://src/microbe_stage/Membrane.tscn"));

    private readonly StringName tintParameterName = new("tint");

    private readonly FluidCurrentsSystem? fluidCurrentsSystem;

    private readonly List<ShaderMaterial> tempMaterialsList = new();
    private readonly List<PlacedOrganelle> tempVisualsToDelete = new();

    /// <summary>
    ///   Used to detect which organelle graphics are no longer used and should be deleted
    /// </summary>
    private readonly HashSet<PlacedOrganelle> inUseOrganelles = new();

    private readonly ConcurrentQueue<MembraneGenerationParameters> membranesToGenerate = new();

    /// <summary>
    ///   Used to avoid requesting the same membrane data to be generated multiple times
    /// </summary>
    private readonly HashSet<long> pendingGenerationsOfMembraneHashes = new();

    /// <summary>
    ///   Keeps track of generated tasks, just to allow Disposing this object safely by waiting for them all
    /// </summary>
    private readonly List<Task> activeGenerationTasks = new();

    private bool pendingMembraneGenerations;

    private volatile int runningMembraneTaskCount;

    public MicrobeVisualsSystem(World world, FluidCurrentsSystem? fluidCurrentsSystem) : base(world)
    {
        this.fluidCurrentsSystem = fluidCurrentsSystem;
    }

    public bool HasPendingOperations()
    {
        return pendingMembraneGenerations;
    }

    public override void BeforeUpdate(in float delta)
    {
        pendingMembraneGenerations = false;

        activeGenerationTasks.RemoveAll(t => t.IsCompleted);
    }

    public override void AfterUpdate(in float delta)
    {
        // TODO: if we need a separate mechanism to communicate our results back, then cleaning up that mechanism
        // here and in on PreUpdate will be needed
        // // Clear any ready resources that weren't required to not keep them forever (but only ones that were
        // // ready in PreUpdate to ensure no resources that managed to finish while update was running are lost)

        // Ensure we have at least some tasks running even if no new membrane generation requests were started
        // this frame
        lock (pendingGenerationsOfMembraneHashes)
        {
            if (pendingGenerationsOfMembraneHashes.Count > runningMembraneTaskCount / 2 ||
                (runningMembraneTaskCount <= 0 && pendingGenerationsOfMembraneHashes.Count > 0))
            {
                StartMembraneGenerationJobs();
            }
        }
    }

    public override void Dispose()
    {
        Dispose(true);
        base.Dispose();
    }

    [Query]
    [All<CellProperties, SpatialInstance, EntityMaterial, RenderPriorityOverride>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref OrganelleContainer organelleContainer, in Entity entity)
    {
        if (organelleContainer.OrganelleVisualsCreated)
            return;

        // Skip if no organelle data
        if (organelleContainer.Organelles == null)
        {
            GD.PrintErr("Missing organelles list for MicrobeVisualsSystem");
            return;
        }

        ref var cellProperties = ref entity.Get<CellProperties>();

        ref var spatialInstance = ref entity.Get<SpatialInstance>();

        // Create graphics top level node if missing for the entity
        spatialInstance.GraphicalInstance ??= new Node3D();

#if DEBUG

        // Check scale is applied properly (but only if not attached as being attached can mean engulfment and at
        // that time the scale can be modified)
        if (!entity.Has<AttachedToEntity>())
        {
            if (cellProperties.IsBacteria)
            {
                if (!spatialInstance.ApplyVisualScale ||
                    spatialInstance.VisualScale != Vector3.One * Constants.BACTERIA_CELL_SCALE)
                {
                    GD.PrintErr("Microbe spatial component doesn't have scale correctly set for bacteria");

                    if (Debugger.IsAttached)
                        Debugger.Break();
                }
            }
            else
            {
                if (spatialInstance.ApplyVisualScale &&
                    spatialInstance.VisualScale != Vector3.One)
                {
                    GD.PrintErr("Microbe spatial component doesn't have scale correctly set for eukaryote");

                    if (Debugger.IsAttached)
                        Debugger.Break();
                }
            }
        }
#endif

        ref var materialStorage = ref entity.Get<EntityMaterial>();

        MembranePointData? data;

        if (entity.Has<MicrobeColonyMember>())
        {
            var member = entity.Get<MicrobeColonyMember>();

            // Only get the membrane for THIS entity's cell (not all cells in the colony)
            // var cellIndex = member.MulticellularBodyPlanPartIndex;
            // var cell = member.Species.ModifiableGameplayCells[cellIndex];

            var colonyLeader = member.ColonyLeader.Get<MicrobeColony>();
            var members = colonyLeader.ColonyMembers;
            var structure = colonyLeader.ColonyStructure;
            GD.Print();
        }

        // Background thread membrane generation
        if (entity.Has<MulticellularSpeciesMember>())
        {
            Entity colonyLeader;
            if (entity.Has<MicrobeColonyMember>())
            {
                colonyLeader = entity.Get<MicrobeColonyMember>().ColonyLeader;
            }
            else
            {
                colonyLeader = entity;
            }

            var speciesMember = entity.Get<MulticellularSpeciesMember>();

            if (colonyLeader.Has<MulticellularGrowth>())
            {
                var growthOrder = colonyLeader.Get<MulticellularGrowth>();
                var nextBodyPlanCellToGrowIndex = growthOrder.NextBodyPlanCellToGrowIndex;
                var lostCells = (growthOrder.LostPartsOfBodyPlan ?? []).ToHashSet();

                // var nextBodyPlanCellToGrowIndex = speciesMember.Species.ModifiableGameplayCells.Count;
                // HashSet<int> lostCells = [];

                // Only get the membrane for THIS entity's cell (not all cells in the colony)
                var cellIndex = speciesMember.MulticellularBodyPlanPartIndex;
                var cell = speciesMember.Species.ModifiableGameplayCells[cellIndex];
                data = GetMulticellularMembraneDataIfReadyOrStartGenerating(cell, cell.ModifiableOrganelles,
                    ref speciesMember,
                    cellIndex, nextBodyPlanCellToGrowIndex, lostCells);
            }
            else
            {
                data = GetMembraneDataIfReadyOrStartGenerating(ref cellProperties, ref organelleContainer);
            }
        }
        else
        {
            data = GetMembraneDataIfReadyOrStartGenerating(ref cellProperties, ref organelleContainer);
        }

        if (data == null)
        {
            // Let other users of the membrane know that we are in the process of re-creating the shape
            cellProperties.CreatedMembrane?.IsChangingShape = true;

            // Need to wait for membrane generation. Organelle visuals aren't created yet even if they could be
            // to avoid the organelles popping in before the membrane.
            pendingMembraneGenerations = true;

            return;
        }

        if (cellProperties.CreatedMembrane == null)
        {
            // TODO: pooling for membrane instances?
            var membrane = membraneScene.Value.Instantiate<Membrane>() ??
                throw new Exception("Invalid membrane scene");

            SetMembraneDisplayData(membrane, data, ref cellProperties);

            spatialInstance.GraphicalInstance.AddChild(membrane);
            cellProperties.CreatedMembrane = membrane;

            membrane.FluidCurrentsSystem = fluidCurrentsSystem;
        }
        else
        {
            // Existing membrane should have its properties updated to make sure they are up to date.
            // For example, an engulfed cell has its membrane wigglyness removed
            SetMembraneDisplayData(cellProperties.CreatedMembrane, data, ref cellProperties);
        }

        // Material is initialized in _Ready, so this is after AddChild of membrane
        tempMaterialsList.Add(cellProperties.CreatedMembrane!.MembraneShaderMaterial ??
            throw new Exception("Membrane didn't set material to edit"));

        // TODO: should this hide organelles when the microbe is dead? (hiding / deleting organelle instances is
        // also talked about in the microbe death system)

        CreateOrganelleVisuals(spatialInstance.GraphicalInstance, ref organelleContainer, ref cellProperties);

        materialStorage.Materials = tempMaterialsList.ToArray();
        tempMaterialsList.Clear();

        organelleContainer.OrganelleVisualsCreated = true;

        // Need to update render priority of the visuals
        entity.Get<RenderPriorityOverride>().RenderPriorityApplied = false;

        // Force recreation of the physics body in case organelles changed to make sure the shape matches growth status
        cellProperties.ShapeCreated = false;
    }

    private MembranePointData? GetMembraneDataIfReadyOrStartGenerating(ref CellProperties cellProperties,
        ref OrganelleContainer organelleContainer)
    {
        // TODO: should we consider the situation where a membrane was requested on the previous update but is not
        // ready yet? This causes extra memory usage here in those cases.
        var hexes = MembraneComputationHelpers.PrepareHexPositionsForMembraneCalculations(
            organelleContainer.Organelles!.Organelles, out var hexCount);

        var hash = MembraneComputationHelpers.ComputeMembraneDataHash(hexes, hexCount, cellProperties.MembraneType);

        var cachedMembrane = ProceduralDataCache.Instance.ReadMembraneData(hash);

        if (cachedMembrane != null)
        {
            // TODO: hopefully this can't get into a permanent loop where 2 conflicting membranes want to
            // re-generate on each game update cycle
            if (!cachedMembrane.MembraneDataFieldsEqual(hexes, hexCount, cellProperties.MembraneType, null, null))
            {
                GD.Print(
                    $"Cache equality mismatch for hash {hash}. cached.VertexCount={cachedMembrane.VertexCount}, hexCount={hexCount}");
                CacheableDataExtensions.OnCacheHashCollision<MembranePointData>(hash);
                cachedMembrane = null;
            }
        }

        if (cachedMembrane != null)
        {
            // Membrane was ready now
            return cachedMembrane;
        }

        // Need to generate a new membrane

        lock (pendingGenerationsOfMembraneHashes)
        {
            if (!pendingGenerationsOfMembraneHashes.Add(hash))
            {
                // Already queued, don't need to queue again

                // Return the unnecessary array that there won't be a cache entry to hold to the pool
                ArrayPool<Vector2>.Shared.Return(hexes);

                return null;
            }
        }

        membranesToGenerate.Enqueue(new MembraneGenerationParameters(hexes, hexCount, cellProperties.MembraneType));

        // Immediately start some jobs to give background threads something to do while the main thread is busy
        // potentially setting up other visuals
        StartMembraneGenerationJobs();

        return null;
    }

    private MembranePointData? GetMulticellularMembraneDataIfReadyOrStartGenerating(CellTemplate cellProperties,
        OrganelleLayout<OrganelleTemplate> organelleContainer, ref MulticellularSpeciesMember multicellular,
        int currentCellIndex, int nextBodyPlanCellToGrowIndex, HashSet<int> lostCells)
    {
        // TODO: should we consider the situation where a membrane was requested on the previous update but is not
        // ready yet? This causes extra memory usage here in those cases.
        var hexes = MembraneComputationHelpers.PrepareHexPositionsForMembraneCalculations(organelleContainer.Organelles,
            out var hexCount);

        List<Vector2> positions = new List<Vector2>();
        List<int> orientations = new List<int>();

        for (int i = 0; i < nextBodyPlanCellToGrowIndex; ++i)
        {
            if (lostCells.Contains(i))
                continue;

            var cell = multicellular.Species.ModifiableGameplayCells[i];
            var cartesian = Hex.AxialToCartesian(cell.Position);
            positions.Add(new Vector2(cartesian.X, cartesian.Z) * Constants.MULTICELLULAR_CELL_DISTANCE_MULTIPLIER);
            orientations.Add(multicellular.Species.ModifiableGameplayCells[i].Orientation);
        }

        var positionsArray = positions.ToArray();
        var rotationsArray = orientations.ToArray();

        // Use the actual cell index to get the correct position for this specific cell
        var thisCartesian =
            Hex.AxialToCartesian(multicellular.Species
                .ModifiableGameplayCells[currentCellIndex].Position);
        var cellPositionInMulticellular = new Vector2(thisCartesian.X, thisCartesian.Z) *
            Constants.MULTICELLULAR_CELL_DISTANCE_MULTIPLIER;

        // Use the simple hash function that includes all parameters
        var hash = MembraneComputationHelpers.ComputeMembraneDataHash(hexes, hexCount, cellProperties.MembraneType,
            positionsArray, cellPositionInMulticellular, rotationsArray,
            multicellular.Species.ModifiableGameplayCells[currentCellIndex].Orientation);

        var cachedMembrane = ProceduralDataCache.Instance.ReadMembraneData(hash);

        if (cachedMembrane != null)
        {
            // TODO: hopefully this can't get into a permanent loop where 2 conflicting membranes want to
            // re-generate on each game update cycle
            if (!cachedMembrane.MembraneDataFieldsEqual(hexes, hexCount, cellProperties.MembraneType, positionsArray,
                    cellPositionInMulticellular, rotationsArray,
                    multicellular.Species.ModifiableGameplayCells[currentCellIndex].Orientation))
            {
                GD.Print($"Multicell cache equality mismatch for hash {hash}." +
                    $"\n  positions: {cachedMembrane.CellPositionInMulticellular} vs {cellPositionInMulticellular}" +
                    $"\n  hexes: {cachedMembrane.HexPositionCount} vs {hexCount}" +
                    $"\n  positions: {cachedMembrane.MulticellularPositions?.Length} vs {positionsArray.Length}" +
                    $"\n  cellIndex: {currentCellIndex}" +
                    $"\n  orientation: {cachedMembrane.CellOrientation} vs {multicellular.Species.ModifiableGameplayCells[currentCellIndex].Orientation}" +
                    $"\n  cached hex[0..4]: {HexDump(cachedMembrane.HexPositions, cachedMembrane.HexPositionCount)}" +
                    $"\n  request hex[0..4]: {HexDump(hexes, hexCount)}" +
                    $"\n  cached multicellularPos[0..4]: {PosDump(cachedMembrane.MulticellularPositions)}" +
                    $"\n  request multicellularPos[0..4]: {PosDump(positionsArray)}");
                CacheableDataExtensions.OnCacheHashCollision<MembranePointData>(hash);
                cachedMembrane = null;

            }
        }

        if (cachedMembrane != null)
        {
            // Membrane was ready now
            return cachedMembrane;
        }

        // Need to generate a new membrane

        lock (pendingGenerationsOfMembraneHashes)
        {
            if (!pendingGenerationsOfMembraneHashes.Add(hash))
            {
                // Already queued, don't need to queue again

                // Return the unnecessary array that there won't be a cache entry to hold to the pool
                ArrayPool<Vector2>.Shared.Return(hexes);

                return null;
            }
        }

        membranesToGenerate.Enqueue(new MembraneGenerationParameters(hexes, hexCount, cellProperties.MembraneType,
            positionsArray, cellPositionInMulticellular, rotationsArray,
            multicellular.Species.ModifiableGameplayCells[currentCellIndex].Orientation));

        // Immediately start some jobs to give background threads something to do while the main thread is busy
        // potentially setting up other visuals
        StartMembraneGenerationJobs();

        return null;
    }
    
    private static string HexDump(Vector2[] arr, int count)
    {
        int n = Math.Min(5, count);
        return string.Join(", ", Enumerable.Range(0, n).Select(i => arr[i].ToString()));
    }

    private static string PosDump(Vector2[]? arr)
    {
        if (arr == null) return "null";
        int n = Math.Min(5, arr.Length);
        return string.Join(", ", Enumerable.Range(0, n).Select(i => arr[i].ToString()));
    }

    private void SetMembraneDisplayData(Membrane membrane, MembranePointData cacheData,
        ref CellProperties cellProperties)
    {
#if DEBUG
        var oldData = membrane.MembraneData;
#endif

        membrane.MembraneData = cacheData;

#if DEBUG
        if (membrane.IsChangingShape && !ReferenceEquals(oldData, cacheData))
            throw new Exception("This field should have been reset automatically");
#endif

        // TODO: this shouldn't override membrane wigglyness if it was set to 0 due to being engulfed (thankfully
        // it's probably the case that visuals aren't currently updated while something is engulfed)
        cellProperties.ApplyMembraneWigglyness(membrane);
    }

    /// <summary>
    ///   Creates visuals for organelles in a container
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: could try pooling some microbe visuals if possible (would be relatively hard to detect when ones are
    ///     unused as this system doesn't handle deleting the visuals after use)
    ///   </para>
    /// </remarks>
    private void CreateOrganelleVisuals(Node3D parentNode, ref OrganelleContainer organelleContainer,
        ref CellProperties cellProperties)
    {
        organelleContainer.CreatedOrganelleVisuals ??= new Dictionary<PlacedOrganelle, Node3D>();

        var organelleColour = PlacedOrganelle.CalculateHSVForOrganelle(cellProperties.Colour)
            * Constants.ORGANELLE_TINT_STRENGTH
            + Colors.White * (1.0f - Constants.ORGANELLE_TINT_STRENGTH);

        foreach (var placedOrganelle in organelleContainer.Organelles!)
        {
            // Only handle organelles that have graphics
            if (!placedOrganelle.Definition.TryGetGraphicsScene(placedOrganelle.Upgrades, out var graphicsInfo))
                continue;

            inUseOrganelles.Add(placedOrganelle);

            Transform3D transform;

            if (!placedOrganelle.Definition.PositionedExternally)
            {
                // Get the transform with right scale (growth) and position
                transform = placedOrganelle.CalculateVisualsTransform();
            }
            else
            {
                // Positioned externally
                var externalPosition = cellProperties.CalculateExternalOrganellePosition(placedOrganelle.Position,
                    placedOrganelle.Orientation, out var rotation);

                transform = placedOrganelle.CalculateVisualsTransformExternal(externalPosition, rotation);
            }

            if (organelleContainer.CreatedOrganelleVisuals.TryGetValue(placedOrganelle, out var existingWrapper))
            {
                // Existing visuals still need their wrapper transform refreshed when the membrane or physics shape
                // changes.
                existingWrapper.Transform = transform;
            }
            else
            {
                // New visuals needed

                // TODO: slime jet handling (and other animation controlled organelles handling)

                // For organelle visuals to work, they need to be wrapped in an extra layer of Spatial to not
                // mess with the normal scale that is used by many organelle scenes
                var extraLayer = new Node3D
                {
                    Transform = transform,
                };

                var visualsInstance = graphicsInfo.LoadedScene.Instantiate<Node3D>();
                placedOrganelle.ReportCreatedGraphics(visualsInstance, graphicsInfo);

                extraLayer.AddChild(visualsInstance);
                parentNode.AddChild(extraLayer);

                organelleContainer.CreatedOrganelleVisuals.Add(placedOrganelle, extraLayer);
            }

            // Visuals already exist
            var graphics = placedOrganelle.OrganelleGraphics;

            if (graphics == null)
                throw new Exception("Organelle graphics should not get reset to null");

            // Materials need to be always fully fetched again to make sure we don't forget any active ones
            int start = tempMaterialsList.Count;

            // Use the model data from when the graphics were loaded for consistency
            if (!graphics.GetMaterial(tempMaterialsList, placedOrganelle.LoadedGraphicsSceneInfo.ModelPath))
            {
                GD.PrintErr("Failed to fetch organelle materials for created: ",
                    placedOrganelle.Definition.InternalName);
            }

            // Apply tint (again) to make sure it is up-to-date
            int count = tempMaterialsList.Count;
            for (int i = start; i < count; ++i)
            {
                tempMaterialsList[i].SetShaderParameter(tintParameterName, organelleColour);
            }
        }

        // Delete unused visuals
        foreach (var entry in organelleContainer.CreatedOrganelleVisuals)
        {
            if (!inUseOrganelles.Contains(entry.Key))
            {
                entry.Value.QueueFree();
                tempVisualsToDelete.Add(entry.Key);
            }
        }

        foreach (var toDelete in tempVisualsToDelete)
        {
            organelleContainer.CreatedOrganelleVisuals.Remove(toDelete);
        }

        inUseOrganelles.Clear();
        tempVisualsToDelete.Clear();
    }

    /// <summary>
    ///   Starts more membrane generation task instances if it makes sense to do so
    /// </summary>
    private void StartMembraneGenerationJobs()
    {
        var executor = TaskExecutor.Instance;

        // Limit concurrent tasks
        int max = Math.Max(1, executor.ParallelTasks - Constants.MEMBRANE_TASKS_LEAVE_EMPTY_THREADS);
        if (runningMembraneTaskCount >= max)
            return;

        // Don't uselessly spawn too many tasks
        if (runningMembraneTaskCount >= membranesToGenerate.Count)
            return;

        var task = new Task(RunMembraneGenerationThread);

        activeGenerationTasks.Add(task);
        executor.AddTask(task);
    }

    private void RunMembraneGenerationThread()
    {
        Interlocked.Increment(ref runningMembraneTaskCount);

        // Process membrane generation requests until empty
        while (membranesToGenerate.TryDequeue(out var generationParameters))
        {
            // Use coordinator to handle both single-cell and multicellular two-pass generation.
            var writtenHashes = MembraneGenerationCoordinator.HandleGenerationRequest(ref generationParameters);

            // writtenHashes contains the cache hashes that correspond to the final results that should be removed
            // from the pending set. For single-cell requests this is the single written hash. For multicellular
            // requests this will contain the multicellular-modified hash for this cell when available.
            lock (pendingGenerationsOfMembraneHashes)
            {
                foreach (var hash in writtenHashes)
                {
                    if (!pendingGenerationsOfMembraneHashes.Remove(hash))
                        GD.PrintErr("Membrane generation result is a hash that wasn't in the pending hashes");
                }
            }

            // TODO: can we always rely on the dynamic data cache or should we have an explicit method for
            // communicating the results back to ourselves in Update? It's fine as long as there isn't a max size
            // for the cache and the clear time is long enough for this to not have to worry about that
        }

        Interlocked.Decrement(ref runningMembraneTaskCount);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            tintParameterName.Dispose();
        }

        var maxWait = TimeSpan.FromSeconds(10);
        foreach (var task in activeGenerationTasks)
        {
            if (!task.Wait(maxWait))
            {
                GD.PrintErr("Failed to wait for a background membrane generation task to finish on " +
                    "dispose");
            }
        }

        activeGenerationTasks.Clear();
    }
}
