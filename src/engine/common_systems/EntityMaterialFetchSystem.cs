namespace Systems
{
    using System;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Fetches the materials for <see cref="EntityMaterial"/> that have auto fetch on
    /// </summary>
    [With(typeof(EntityMaterial))]
    [ReadsComponent(typeof(SpatialInstance))]
    [ReadsComponent(typeof(SpatialInstance))]
    [RunsAfter(typeof(PathBasedSceneLoader))]
    [RunsAfter(typeof(PredefinedVisualLoaderSystem))]
    [RuntimeCost(0.5f)]
    [RunsOnMainThread]
    public sealed class EntityMaterialFetchSystem : AEntitySetSystem<float>
    {
        // TODO: determine if it is thread safe to fetch Godot materials
        public EntityMaterialFetchSystem(World world) : base(world, null)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var materialComponent = ref entity.Get<EntityMaterial>();

            if (materialComponent.MaterialFetchPerformed || materialComponent.Materials != null)
                return;

            if (materialComponent.AutoRetrieveFromSpatial)
            {
                try
                {
                    ref var spatial = ref entity.Get<SpatialInstance>();

                    // Wait until spatial gets an instance
                    if (spatial.GraphicalInstance == null)
                        return;

                    var nodeToFetchFrom = spatial.GraphicalInstance;

                    if (!materialComponent.AutoRetrieveAssumesNodeIsDirectlyAttached)
                    {
                        nodeToFetchFrom = nodeToFetchFrom.GetChild<Node3D>(0);
                    }

                    if (string.IsNullOrEmpty(materialComponent.AutoRetrieveModelPath))
                    {
                        materialComponent.Materials = new[] { nodeToFetchFrom.GetMaterial() };
                    }
                    else
                    {
                        using var nodePath = new NodePath(materialComponent.AutoRetrieveModelPath);
                        materialComponent.Materials = new[] { nodeToFetchFrom.GetMaterial(nodePath) };
                    }

                    if (materialComponent.Materials is not { Length: > 0 } || materialComponent.Materials[0] == null)
                    {
                        throw new NullReferenceException(
                            "Expected material not set, this component doesn't wait for material to be set later");
                    }
                }
                catch (Exception e)
                {
                    GD.PrintErr("Entity with material auto retrieve from spatial cannot access " +
                        "spatial component's material: ", e);
                }
            }

            materialComponent.MaterialFetchPerformed = true;
        }
    }
}
