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
    public sealed class MicrobeVisualsSystem : AEntitySetSystem<float>
    {
        private readonly Lazy<PackedScene> membraneScene =
            new(() => GD.Load<PackedScene>("res://src/microbe_stage/Membrane.tscn"));

        // TODO: implement membrane background generation
        private Dictionary<Entity, Membrane> generatedMembranes;

        private uint membraneGenerationRequestNumber;

        public MicrobeVisualsSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void PreUpdate(float state)
        {
            base.PreUpdate(state);

            ;
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

            // TODO: background thread membrane generation

            ref var spatialInstance = ref entity.Get<SpatialInstance>();
            spatialInstance.GraphicalInstance?.QueueFree();
            spatialInstance.GraphicalInstance = new Spatial();

            ref var materialStorage = ref entity.Get<EntityMaterial>();

            // TODO: remove if this approach isn't used
            // var createdMaterials = new ShaderMaterial[1];

            // TODO: only recreate membrane entirely if missing
            var membrane = membraneScene.Value.Instance<Membrane>();
            ++membraneGenerationRequestNumber;

            SetMembraneDisplayData(membrane, ref organelleContainer, ref cellProperties);

            cellProperties.CreatedMembrane = membrane;
            spatialInstance.GraphicalInstance.AddChild(membrane);

            // Material is initialized in _Ready so this is after AddChild
            materialStorage.Material =
                membrane.MaterialToEdit ?? throw new Exception("Membrane didn't set material to edit");

            // TODO: health value applying

            // TODO: only recreate organelle visuals that are really needed (instead of all every time)
            CreateOrganelleVisuals(spatialInstance.GraphicalInstance, ref organelleContainer, ref cellProperties);

            organelleContainer.OrganelleVisualsCreated = true;
        }

        protected override void PostUpdate(float state)
        {
            base.PostUpdate(state);

            // Clear any ready resources that weren't required to not keep them forever
        }

        private static void SetMembraneDisplayData(Membrane membrane, ref OrganelleContainer organelleContainer,
            ref CellProperties cellProperties)
        {
            membrane.OrganellePositions = organelleContainer.Organelles!.Select(o =>
            {
                var pos = Hex.AxialToCartesian(o.Position);
                return new Vector2(pos.x, pos.z);
            }).ToList();

            membrane.Type = cellProperties.MembraneType;
            membrane.WigglyNess = cellProperties.MembraneType.BaseWigglyness;
            membrane.MovementWigglyNess = cellProperties.MembraneType.MovementWigglyness;
        }

        private static void CreateOrganelleVisuals(Spatial parentNode, ref OrganelleContainer organelleContainer,
            ref CellProperties cellProperties)
        {
            foreach (var placedOrganelle in organelleContainer.Organelles!)
            {
                // Only handle organelles that have graphics
                if (placedOrganelle.Definition.LoadedScene == null)
                    continue;

                // TODO: external organelle positioning

                // TODO: only overwrite scale when needed (otherwise organelle growth animation won't work)
                var transform = new Transform(new Basis(
                        MathUtils.CreateRotationForOrganelle(1 * placedOrganelle.Orientation)).Scaled(new Vector3(
                        Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
                        Constants.DEFAULT_HEX_SIZE)),
                    Hex.AxialToCartesian(placedOrganelle.Position) + placedOrganelle.Definition.ModelOffset);

                // For organelle visuals to work, they need to be wrapped in an extra layer of Spatial to not
                // mess with the normal scale that is used by many organelle scenes
                var extraLayer = new Spatial
                {
                    Transform = transform,
                };

                var visualsInstance = placedOrganelle.Definition.LoadedScene.Instance<Spatial>();

                var material = visualsInstance.GetMaterial(placedOrganelle.Definition.DisplaySceneModelPath);
                material.SetShaderParam("tint", cellProperties.Colour);

                // TODO: render order

                extraLayer.AddChild(visualsInstance);
                parentNode.AddChild(extraLayer);
            }
        }
    }
}
