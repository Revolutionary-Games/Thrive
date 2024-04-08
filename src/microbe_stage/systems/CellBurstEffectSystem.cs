namespace Systems;

using Components;
using DefaultEcs;
using DefaultEcs.System;
using Godot;
using World = DefaultEcs.World;

/// <summary>
///   Updates cell burst effects as time elapses. As this just setups the effect this doesn't need to run per frame
///   normal update frequen.Y is fine.
/// </summary>
/// <remarks>
///   <para>
///     Marked as reading the spatial instance as this just tweaks some particle system properties on it
///     (if exists)
///   </para>
/// </remarks>
[With(typeof(CellBurstEffect))]
[With(typeof(TimedLife))]
[With(typeof(SpatialInstance))]
[ReadsComponent(typeof(SpatialInstance))]
[RunsBefore(typeof(TimedLifeSystem))]
[RuntimeCost(0.25f)]
[RunsOnMainThread]
public sealed class CellBurstEffectSystem : AEntitySetSystem<float>
{
    public CellBurstEffectSystem(World world) : base(world, null)
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

        var particles = spatial.GraphicalInstance as GpuParticles3D;

        if (particles == null)
        {
            GD.PrintErr("Cell burst effect visual instance is not particles");
            return;
        }

        timedLife.TimeToLiveRemaining = (float)particles.Lifetime;

        var material = (ParticleProcessMaterial)particles.ProcessMaterial;

        material.EmissionSphereRadius = burstEffect.Radius / 2;
        material.LinearAccelMax = burstEffect.Radius * 0.5f;
        material.LinearAccelMin = burstEffect.Radius * 0.25f;
        particles.OneShot = true;
    }
}
