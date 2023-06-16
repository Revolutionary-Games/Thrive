namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    [With(typeof(WorldPosition))]
    [With(typeof(SpatialInstance))]
    public sealed class SpatialPositionSystem : AEntitySetSystem<float>
    {
        public SpatialPositionSystem(World world, IParallelRunner runner)
            : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var position = ref entity.Get<WorldPosition>();
            ref var spatial = ref entity.Get<SpatialInstance>();

            if (spatial.GraphicalInstance != null)
            {
                // TODO: scale
                spatial.GraphicalInstance.Transform = new Transform(new Basis(position.Rotation), position.Position);
            }
        }
    }
}
