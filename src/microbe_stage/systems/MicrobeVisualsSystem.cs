namespace Systems
{
    using System;
    using System.Buffers;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Generates the visuals needed for microbes. Handles the membrane and organelle graphics. Attaching to the
    ///   Godot scene tree is handled by <see cref="SpatialAttachSystem"/>
    /// </summary>
    [With(typeof(OrganelleContainer))]
    [With(typeof(CellProperties))]
    [With(typeof(SpatialInstance))]
    [With(typeof(EntityMaterial))]
    [With(typeof(RenderPriorityOverride))]
    [RunsBefore(typeof(SpatialAttachSystem))]
    [RunsBefore(typeof(EntityMaterialFetchSystem))]
    [RunsBefore(typeof(SpatialPositionSystem))]
    [RuntimeCost(5)]
    [RunsOnMainThread]
    public sealed class MicrobeVisualsSystem : AEntitySetSystem<float>
    {
        private readonly Lazy<PackedScene> membraneScene =
            new(() => GD.Load<PackedScene>("res://src/microbe_stage/Membrane.tscn"));

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
        ///   Keeps track of generated tasks, just to allow disposing this object safely by waiting for them all
        /// </summary>
        private readonly List<Task> activeGenerationTasks = new();

        private bool pendingMembraneGenerations;

        private int runningMembraneTaskCount;

        public MicrobeVisualsSystem(World world) : base(world, null)
        {
        }

        public bool HasPendingOperations()
        {
            return pendingMembraneGenerations;
        }

        public override void Dispose()
        {
            base.Dispose();

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

        protected override void PreUpdate(float delta)
        {
            base.PreUpdate(delta);

            pendingMembraneGenerations = false;

            activeGenerationTasks.RemoveAll(t => t.IsCompleted);
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var organelleContainer = ref entity.Get<OrganelleContainer>();

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

            // Create graphics top level node if missing for entity
            spatialInstance.GraphicalInstance ??= new Spatial();

#if DEBUG

            // Check scale is applied properly (but only if not attached as being attached can mean engulfment and at
            // that time the scale can be modified)
            if (!entity.Has<AttachedToEntity>())
            {
                if (cellProperties.IsBacteria)
                {
                    if (spatialInstance.ApplyVisualScale != true ||
                        spatialInstance.VisualScale != new Vector3(0.5f, 0.5f, 0.5f))
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

            // Background thread membrane generation
            var data = GetMembraneDataIfReadyOrStartGenerating(ref cellProperties, ref organelleContainer);

            if (data == null)
            {
                if (cellProperties.CreatedMembrane != null)
                {
                    // Let other users of the membrane know that we are in the process of re-creating the shape
                    cellProperties.CreatedMembrane.IsChangingShape = true;
                }

                // Need to wait for membrane generation. Organelle visuals aren't created yet even if they could be
                // to avoid the organelles popping in before the membrane.
                pendingMembraneGenerations = true;

                return;
            }

            if (cellProperties.CreatedMembrane == null)
            {
                var membrane = membraneScene.Value.Instance<Membrane>() ??
                    throw new Exception("Invalid membrane scene");

                SetMembraneDisplayData(membrane, data, ref cellProperties);

                spatialInstance.GraphicalInstance.AddChild(membrane);
                cellProperties.CreatedMembrane = membrane;
            }
            else
            {
                // Existing membrane should have its properties updated to make sure they are up to date
                // For example an engulfed cell has its membrane wigglyness removed
                SetMembraneDisplayData(cellProperties.CreatedMembrane, data, ref cellProperties);
            }

            // Material is initialized in _Ready so this is after AddChild of membrane
            tempMaterialsList.Add(cellProperties.CreatedMembrane!.MaterialToEdit ??
                throw new Exception("Membrane didn't set material to edit"));

            // TODO: should this hide organelles when the microbe is dead? (hiding / deleting organelle instances is
            // also talked about in the microbe death system)

            CreateOrganelleVisuals(spatialInstance.GraphicalInstance, ref organelleContainer, ref cellProperties);

            materialStorage.Materials = tempMaterialsList.ToArray();
            tempMaterialsList.Clear();

            organelleContainer.OrganelleVisualsCreated = true;

            // Need to update render priority of the visuals
            entity.Get<RenderPriorityOverride>().RenderPriorityApplied = false;

            // Force recreation of physics body in case organelles changed to make sure the shape matches growth status
            cellProperties.ShapeCreated = false;
        }

        protected override void PostUpdate(float state)
        {
            base.PostUpdate(state);

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
                if (!cachedMembrane.MembraneDataFieldsEqual(hexes, hexCount, cellProperties.MembraneType))
                {
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

        private void CreateOrganelleVisuals(Spatial parentNode, ref OrganelleContainer organelleContainer,
            ref CellProperties cellProperties)
        {
            organelleContainer.CreatedOrganelleVisuals ??= new Dictionary<PlacedOrganelle, Spatial>();

            var organelleColour = PlacedOrganelle.CalculateHSVForOrganelle(cellProperties.Colour);

            foreach (var placedOrganelle in organelleContainer.Organelles!)
            {
                // Only handle organelles that have graphics
                if (placedOrganelle.Definition.LoadedScene == null)
                    continue;

                inUseOrganelles.Add(placedOrganelle);

                Transform transform;

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

                if (!organelleContainer.CreatedOrganelleVisuals.ContainsKey(placedOrganelle))
                {
                    // New visuals needed

                    // TODO: slime jet handling (and other animation controlled organelles handling)

                    // For organelle visuals to work, they need to be wrapped in an extra layer of Spatial to not
                    // mess with the normal scale that is used by many organelle scenes
                    var extraLayer = new Spatial
                    {
                        Transform = transform,
                    };

                    var visualsInstance = placedOrganelle.Definition.LoadedScene.Instance<Spatial>();
                    placedOrganelle.ReportCreatedGraphics(visualsInstance);

                    extraLayer.AddChild(visualsInstance);
                    parentNode.AddChild(extraLayer);

                    organelleContainer.CreatedOrganelleVisuals.Add(placedOrganelle, visualsInstance);
                }

                // Visuals already exist
                var graphics = placedOrganelle.OrganelleGraphics;

                if (graphics == null)
                    throw new Exception("Organelle graphics should not get reset to null");

                // Materials need to be always fully fetched again to make sure we don't forget any active ones
                int start = tempMaterialsList.Count;
                if (graphics is OrganelleMeshWithChildren organelleMeshWithChildren)
                {
                    organelleMeshWithChildren.GetChildrenMaterials(tempMaterialsList);
                }

                var material = graphics.GetMaterial(placedOrganelle.Definition.DisplaySceneModelNodePath);
                tempMaterialsList.Add(material);

                // Apply tint (again) to make sure it is up to date
                int count = tempMaterialsList.Count;
                for (int i = start; i < count; ++i)
                {
                    tempMaterialsList[i].SetShaderParam("tint", organelleColour);
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
                var generator = MembraneShapeGenerator.GetThreadSpecificGenerator();

                var cacheEntry = generator.GenerateShape(ref generationParameters);

                // Cache entry now owns the array data that was in the generationParameters and will return it to the
                // pool when the cache disposes it

                var hash = ProceduralDataCache.Instance.WriteMembraneData(cacheEntry);

                // TODO: already generate the 3D points here for use on the main thread for faster membrane
                // creation?

                lock (pendingGenerationsOfMembraneHashes)
                {
                    if (!pendingGenerationsOfMembraneHashes.Remove(hash))
                        GD.PrintErr("Membrane generation result is a hash that wasn't in the pending hashes");
                }

                // TODO: can we always rely on the dynamic data cache or should we have an explicit method for
                // communicating the results back to ourselves in Update? It's fine as long as there isn't a max size
                // for the cache and the clear time is long enough for this to not have to worry about that
            }

            Interlocked.Decrement(ref runningMembraneTaskCount);
        }
    }
}
