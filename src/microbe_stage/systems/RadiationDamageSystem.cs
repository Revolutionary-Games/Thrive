namespace Systems;

using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;

/// <summary>
///   Causes radiation damage based on stored compounds and radiation resistance of microbes
/// </summary>
[With(typeof(Health))]
[With(typeof(CompoundStorage))]
[ReadsComponent(typeof(OrganelleContainer))]
[ReadsComponent(typeof(CellProperties))]
[RunsAfter(typeof(OsmoregulationAndHealingSystem))]
[RunsAfter(typeof(IrradiationSystem))]
[RunsBefore(typeof(DamageSoundSystem))]
public sealed class RadiationDamageSystem : AEntitySetSystem<float>
{
    public RadiationDamageSystem(World world, IParallelRunner runner) : base(world, runner,
        Constants.SYSTEM_HIGH_ENTITIES_PER_THREAD)
    {
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var storage = ref entity.Get<CompoundStorage>();
        var compounds = storage.Compounds;

        var radiationAmount = compounds.GetCompoundAmount(Compound.Radiation);

        // Early return if not too much radiation to cause damage
        if (radiationAmount < Constants.RADIATION_DAMAGE_THRESHOLD)
            return;

        ref var health = ref entity.Get<Health>();

        // Apply natural decay to radiation so that it doesn't infinitely stack in cells that cannot process it
        compounds.TakeCompound(Compound.Radiation, Constants.RADIATION_NATURAL_DECAY * delta);

        // Offset damage by protective organelles
        if (entity.Has<OrganelleContainer>())
        {
            ref var organelles = ref entity.Get<OrganelleContainer>();

            radiationAmount -= organelles.RadiationProtection * Constants.RADIATION_PROTECTION_PER_ORGANELLE;
        }

        var rawDamage = radiationAmount * Constants.RADIATION_DAMAGE_MULTIPLIER * delta;

        // Apply damage if there is some to apply
        if (rawDamage > 0 && !health.Dead)
        {
            // TODO: need to only apply the damage in a fixed rate otherwise the sound loudness depends on the game framerate...

            health.DealMicrobeDamage(ref entity.Get<CellProperties>(), rawDamage, "radiation");
        }
    }
}
