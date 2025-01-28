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
[RuntimeCost(0.5f)]
public sealed class RadiationDamageSystem : AEntitySetSystem<float>
{
    /// <summary>
    ///   Used to apply damage not on each game update
    /// </summary>
    private float elapsedSinceUpdate;

    private bool trigger;

    public RadiationDamageSystem(World world, IParallelRunner runner) : base(world, runner,
        Constants.SYSTEM_HIGH_ENTITIES_PER_THREAD)
    {
    }

    protected override void PreUpdate(float delta)
    {
        base.PreUpdate(delta);
        elapsedSinceUpdate += delta;

        trigger = elapsedSinceUpdate >= Constants.RADIATION_DAMAGE_INTERVAL;
    }

    protected override void Update(float delta, in Entity entity)
    {
        if (!trigger)
            return;

        var compounds = entity.Get<CompoundStorage>().Compounds;

        var radiationAmount = compounds.GetCompoundAmount(Compound.Radiation);

        // Extra early return for cells with no radiation
        if (radiationAmount <= MathUtils.EPSILON)
            return;

        var radiationFraction = radiationAmount / compounds.GetCapacityForCompound(Compound.Radiation);

        if (radiationFraction < Constants.RADIATION_DAMAGE_THRESHOLD)
            return;

        ref var health = ref entity.Get<Health>();

        // Apply natural decay to radiation so that it doesn't infinitely stack in cells that cannot process it
        compounds.TakeCompound(Compound.Radiation, Constants.RADIATION_NATURAL_DECAY * elapsedSinceUpdate);

        // Offset damage by protective organelles
        if (entity.Has<OrganelleContainer>())
        {
            ref var organelles = ref entity.Get<OrganelleContainer>();

            radiationFraction -= organelles.RadiationProtection * Constants.RADIATION_PROTECTION_PER_ORGANELLE;
        }

        var rawDamage = radiationFraction * Constants.RADIATION_DAMAGE_MULTIPLIER * elapsedSinceUpdate;

        // Apply damage if there is some to apply
        if (rawDamage > 0 && !health.Dead)
        {
            // TODO: need to only apply the damage in a fixed rate otherwise the sound loudness depends on the game framerate...

            health.DealMicrobeDamage(ref entity.Get<CellProperties>(), rawDamage, "radiation");

            entity.SendNoticeIfPossible(() =>
                new SimpleHUDMessage(Localization.Translate("NOTICE_RADIATION_DAMAGE"), DisplayDuration.Short));
        }
    }

    protected override void PostUpdate(float state)
    {
        base.PostUpdate(state);

        if (trigger)
            elapsedSinceUpdate = 0;
    }
}
