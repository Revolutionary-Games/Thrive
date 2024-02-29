namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Updates visual positions of entities for rendering by Godot
    /// </summary>
    [With(typeof(WorldPosition))]
    [With(typeof(SpatialInstance))]
    [ReadsComponent(typeof(WorldPosition))]
    [RuntimeCost(36)]
    [RunsOnMainThread]
    public sealed class SpatialPositionSystem : AEntitySetSystem<float>
    {
        public SpatialPositionSystem(World world) : base(world, null)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var position = ref entity.Get<WorldPosition>();
            ref var spatial = ref entity.Get<SpatialInstance>();

            if (spatial.GraphicalInstance == null)
                return;

            if (spatial.ApplyVisualScale)
            {
                spatial.GraphicalInstance.Transform =
                    new Transform(new Basis(position.Rotation).Scaled(spatial.VisualScale), position.Position);
            }
            else
            {
                spatial.GraphicalInstance.Transform =
                    new Transform(new Basis(position.Rotation), position.Position);
            }
        }
    }
}
