namespace Systems
{
    using System;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using Newtonsoft.Json;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Gives a push from currents in a fluid to physics entities (that have <see cref="ManualPhysicsControl"/>).
    ///   Only acts on entities marked with <see cref="CurrentAffected"/>.
    /// </summary>
    [With(typeof(CurrentAffected))]
    [With(typeof(Physics))]
    [With(typeof(ManualPhysicsControl))]
    [With(typeof(WorldPosition))]
    [ReadsComponent(typeof(CurrentAffected))]
    [ReadsComponent(typeof(Physics))]
    [ReadsComponent(typeof(WorldPosition))]
    [RuntimeCost(8)]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class FluidCurrentsSystem : AEntitySetSystem<float>
    {
        private const float DISTURBANCE_TIMESCALE = 1.000f;
        private const float CURRENTS_TIMESCALE = 1.000f / 500.0f;
        private const float CURRENTS_STRETCHING_MULTIPLIER = 1.0f / 10.0f;
        private const float MIN_CURRENT_INTENSITY = 0.4f;
        private const float DISTURBANCE_TO_CURRENTS_RATIO = 0.15f;
        private const float POSITION_SCALING = 0.9f;

        private readonly FastNoiseLite noiseDisturbancesX;
        private readonly FastNoiseLite noiseDisturbancesY;
        private readonly FastNoiseLite noiseCurrentsX;
        private readonly FastNoiseLite noiseCurrentsY;

        // private readonly Vector2 scale = new Vector2(0.05f, 0.05f);

        [JsonProperty]
        private float currentsTimePassed;

        public FluidCurrentsSystem(World world, IParallelRunner runner) : base(world, runner,
            Constants.SYSTEM_HIGHER_ENTITIES_PER_THREAD)
        {
            noiseDisturbancesX = new FastNoiseLite(69);
            noiseDisturbancesX.SetNoiseType(FastNoiseLite.NoiseType.Perlin);

            noiseDisturbancesY = new FastNoiseLite(13);
            noiseDisturbancesY.SetNoiseType(FastNoiseLite.NoiseType.Perlin);

            noiseCurrentsX = new FastNoiseLite(420);
            noiseCurrentsX.SetNoiseType(FastNoiseLite.NoiseType.Perlin);

            noiseCurrentsY = new FastNoiseLite(1337);
            noiseCurrentsY.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        }

        /// <summary>
        ///   JSON constructor for creating temporary instances used to apply the child properties
        /// </summary>
        [JsonConstructor]
        public FluidCurrentsSystem(float currentsTimePassed) : base(TemporarySystemHelper.GetDummyWorldForLoad(), null)
        {
            this.currentsTimePassed = currentsTimePassed;

            noiseDisturbancesX = null!;
            noiseDisturbancesY = null!;
            noiseCurrentsX = null!;
            noiseCurrentsY = null!;
        }

        public Vector2 VelocityAt(Vector2 position)
        {
            var scaledPosition = position * POSITION_SCALING;

            float disturbancesX = noiseDisturbancesX.GetNoise(scaledPosition.x, scaledPosition.y,
                currentsTimePassed * DISTURBANCE_TIMESCALE);
            float disturbancesY = noiseDisturbancesY.GetNoise(scaledPosition.x, scaledPosition.y,
                currentsTimePassed * DISTURBANCE_TIMESCALE);

            float currentsX = noiseCurrentsX.GetNoise(scaledPosition.x * CURRENTS_STRETCHING_MULTIPLIER,
                scaledPosition.y, currentsTimePassed * CURRENTS_TIMESCALE);
            float currentsY = noiseCurrentsY.GetNoise(scaledPosition.x,
                scaledPosition.y * CURRENTS_STRETCHING_MULTIPLIER,
                currentsTimePassed * CURRENTS_TIMESCALE);

            var disturbancesVelocity = new Vector2(disturbancesX, disturbancesY);
            var currentsVelocity = new Vector2(Math.Abs(currentsX) > MIN_CURRENT_INTENSITY ? currentsX : 0.0f,
                Math.Abs(currentsY) > MIN_CURRENT_INTENSITY ? currentsY : 0.0f);

            return (disturbancesVelocity * DISTURBANCE_TO_CURRENTS_RATIO) +
                (currentsVelocity * (1.0f - DISTURBANCE_TO_CURRENTS_RATIO));
        }

        protected override void PreUpdate(float delta)
        {
            base.PreUpdate(delta);

            currentsTimePassed += delta;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var physics = ref entity.Get<Physics>();

            if (physics.Body == null)
                return;

            ref var position = ref entity.Get<WorldPosition>();
            ref var physicsControl = ref entity.Get<ManualPhysicsControl>();

            var pos = new Vector2(position.Position.x, position.Position.z);
            var vel = VelocityAt(pos) * Constants.MAX_FORCE_APPLIED_BY_CURRENTS;

            physicsControl.ImpulseToGive += new Vector3(vel.x, 0, vel.y) * delta;
        }
    }
}
