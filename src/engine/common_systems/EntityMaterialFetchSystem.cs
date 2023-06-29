namespace Systems
{
    using System;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Fetches the materials for <see cref="EntityMaterial"/> that have auto fetch on
    /// </summary>
    [With(typeof(EntityMaterial))]
    public sealed class EntityMaterialFetchSystem : AEntitySetSystem<float>
    {
        public EntityMaterialFetchSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var materialComponent = ref entity.Get<EntityMaterial>();

            if (materialComponent.MaterialFetchPerformed || materialComponent.Material != null)
                return;

            if (materialComponent.AutoRetrieveFromSpatial)
            {
                try
                {
                    ref var spatial = ref entity.Get<SpatialInstance>();

                    // Wait until spatial gets an instance
                    if (spatial.GraphicalInstance == null)
                        return;

                    materialComponent.Material = spatial.GraphicalInstance.GetMaterial();

                    if (materialComponent.Material == null)
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
