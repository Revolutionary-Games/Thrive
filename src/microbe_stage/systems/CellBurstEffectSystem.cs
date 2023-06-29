namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Updates cell burst effects as time elapses
    /// </summary>
    [With(typeof(CellBurstEffect))]
    [With(typeof(TimedLife))]
    [With(typeof(SpatialInstance))]
    public sealed class CellBurstEffectSystem : AEntitySetSystem<float>
    {
        public CellBurstEffectSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var burstEffect = ref entity.Get<CellBurstEffect>();
            ref var timedLife = ref entity.Get<TimedLife>();
            ref var spatial = ref entity.Get<SpatialInstance>();

            if (burstEffect.Initialized)
                return;

            // Skip if can't initialize yet
            if (spatial.GraphicalInstance == null)
                return;

            burstEffect.Initialized = true;

            var particles = spatial.GraphicalInstance as Particles;

            if (particles == null)
            {
                GD.PrintErr("Cell burst effect visual instance is not particles");
                return;
            }

            timedLife.TimeToLiveRemaining = particles.Lifetime;

            var material = (ParticlesMaterial)particles.ProcessMaterial;

            material.EmissionSphereRadius = burstEffect.Radius / 2;
            material.LinearAccel = burstEffect.Radius / 2;
            particles.OneShot = true;
        }
    }
}
