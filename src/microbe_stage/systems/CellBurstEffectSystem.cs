namespace Systems;

using System.Runtime.CompilerServices;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Updates cell burst effects as time elapses. As this just setups the effect this doesn't need to run per frame
///   normal update frequency is fine.
/// </summary>
/// <remarks>
///   <para>
///     Marked as reading the spatial instance as this just tweaks some particle system properties on it
///     (if exists)
///   </para>
/// </remarks>
[ReadsComponent(typeof(SpatialInstance))]
[RunsBefore(typeof(TimedLifeSystem))]
[RuntimeCost(0.25f)]
[RunsOnMainThread]
public partial class CellBurstEffectSystem : BaseSystem<World, float>
{
    public CellBurstEffectSystem(World world) : base(world)
    {
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref CellBurstEffect burstEffect, ref TimedLife timedLife, ref SpatialInstance spatial)
    {
        if (burstEffect.Initialized)
            return;

        // Skip if this can't be initialized yet
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
