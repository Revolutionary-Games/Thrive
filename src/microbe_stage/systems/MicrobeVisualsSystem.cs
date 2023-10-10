namespace Systems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Generates the visuals needed for microbes. Handles the membrane and organelle graphics.
    /// </summary>
    [With(typeof(OrganelleContainer))]
    [With(typeof(CellProperties))]
    [With(typeof(SpatialInstance))]
    [With(typeof(EntityMaterial))]
    [RunsOnMainThread]
    public sealed class MicrobeVisualsSystem : AEntitySetSystem<float>
    {
        private readonly Node visualsParent;

        private readonly Lazy<PackedScene> membraneScene =
            new(() => GD.Load<PackedScene>("res://src/microbe_stage/Membrane.tscn"));

        private readonly List<ShaderMaterial> tempMaterialsList = new();
        private readonly List<PlacedOrganelle> tempVisualsToDelete = new();

        /// <summary>
        ///   Used to detect which organelle graphics are no longer used and should be deleted
        /// </summary>
        private readonly HashSet<PlacedOrganelle> inUseOrganelles = new();

        // TODO: implement membrane background generation
        private Dictionary<Entity, Membrane> generatedMembranes;

        // TODO: implement a mode to purely create visuals without any physics

        private uint membraneGenerationRequestNumber;

        public MicrobeVisualsSystem(Node visualsParent, World world) : base(world, null)
        {
            this.visualsParent = visualsParent;
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

            cellProperties.CreatedMembrane = null;

            ref var spatialInstance = ref entity.Get<SpatialInstance>();

            // Create graphics top level node if missing for entity
            if (spatialInstance.GraphicalInstance == null)
            {
                spatialInstance.GraphicalInstance = new Spatial();
                visualsParent.AddChild(spatialInstance.GraphicalInstance);
            }

            // Bacteria is 50% of the scale of other microbes
            spatialInstance.GraphicalInstance.Scale =
                cellProperties.IsBacteria ? new Vector3(0.5f, 0.5f, 0.5f) : Vector3.One;

            ref var materialStorage = ref entity.Get<EntityMaterial>();

            // TODO: background thread membrane generation

            if (cellProperties.CreatedMembrane == null)
            {
                var membrane = membraneScene.Value.Instance<Membrane>() ??
                    throw new Exception("Invalid membrane scene");

                SetMembraneDisplayData(membrane, ref organelleContainer, ref cellProperties);

                spatialInstance.GraphicalInstance.AddChild(membrane);

                cellProperties.CreatedMembrane = membrane;
            }
            else
            {
                // Existing membrane should have its properties updated to make sure they are up to date
                // For example an engulfed cell has its membrane wigglyness removed
                SetMembraneDisplayData(cellProperties.CreatedMembrane, ref organelleContainer, ref cellProperties);
            }

            // Material is initialized in _Ready so this is after AddChild
            tempMaterialsList.Add(
                cellProperties.CreatedMembrane!.MaterialToEdit ??
                throw new Exception("Membrane didn't set material to edit"));

            // TODO: make the threaded membrane generation instead of forcing it here as getting the external organelle
            // positions forces the full calculation of the membrane data (to no longer be marked dirty)

            // TODO: should this hide organelles when the microbe is dead?

            CreateOrganelleVisuals(spatialInstance.GraphicalInstance, ref organelleContainer, ref cellProperties);

            materialStorage.Materials = tempMaterialsList.ToArray();
            tempMaterialsList.Clear();

            organelleContainer.OrganelleVisualsCreated = true;

            // Force recreation of physics body in case organelles changed to make sure the shape matches growth status
            cellProperties.ShapeCreated = false;
        }

        protected override void PostUpdate(float state)
        {
            base.PostUpdate(state);

            // Clear any ready resources that weren't required to not keep them forever
        }

        private void SetMembraneDisplayData(Membrane membrane, ref OrganelleContainer organelleContainer,
            ref CellProperties cellProperties)
        {
            ++membraneGenerationRequestNumber;

            // TODO: once background generation is used, this needs to make sure that no multiple generation requests
            // can affect the membrane at once (probably should refactor the membrane to just directly take in a
            // membrane generated cache entry)
            var organellePositions = membrane.ModifiableOrganelles;

            // Reserve a bit of memory before the loop
            var expectedMinCapacity = organelleContainer.Organelles!.Count * 2;
            if (organellePositions.Capacity < expectedMinCapacity)
                organellePositions.Capacity = expectedMinCapacity;

            organellePositions.Clear();

            foreach (var entry in organelleContainer.Organelles)
            {
                // The membrane needs hex positions (rather than organelle positions) to handle cells with multihex
                // organelles
                foreach (var hex in entry.Definition.GetRotatedHexes(entry.Orientation))
                {
                    var hexCartesian = Hex.AxialToCartesian(entry.Position + hex);
                    organellePositions.Add(new Vector2(hexCartesian.x, hexCartesian.z));
                }
            }

            membrane.Type = cellProperties.MembraneType;

            // TODO: this shouldn't override membrane wigglyness if it was set to 0 due to being engulfed (thankfully
            // it's probably the case that visuals aren't currently updated while something is engulfed)
            cellProperties.ApplyMembraneWigglyness(membrane);

            // TODO: don't mark dirty if positions didn't end up changing? If the background thread generation is done
            // then this probably won't matter at all thanks to caching
            membrane.Dirty = true;
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

                var material = graphics.GetMaterial(placedOrganelle.Definition.DisplaySceneModelPath);
                tempMaterialsList.Add(material);

                // Apply tint (again) to make sure it is up to date
                int count = tempMaterialsList.Count;
                for (int i = start; i < count; ++i)
                {
                    tempMaterialsList[i].SetShaderParam("tint", organelleColour);
                }

                // TODO: render order?
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
    }
}
