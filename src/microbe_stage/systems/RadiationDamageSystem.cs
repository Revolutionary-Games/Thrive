namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;

/// <summary>
///   Causes radiation damage based on stored compounds and radiation resistance of microbes
/// </summary>
[ReadsComponent(typeof(OrganelleContainer))]
[ReadsComponent(typeof(CellProperties))]
[RunsAfter(typeof(OsmoregulationAndHealingSystem))]
[RunsAfter(typeof(IrradiationSystem))]
[RunsBefore(typeof(DamageSoundSystem))]
[RuntimeCost(0.5f)]
public partial class RadiationDamageSystem : BaseSystem<World, float>
{
    /// <summary>
    ///   Used to apply damage not on each game update
    /// </summary>
    private float elapsedSinceUpdate;

    public RadiationDamageSystem(World world) : base(world)
    {
    }

    public override void Update(in float delta)
    {
        elapsedSinceUpdate += delta;

        if (elapsedSinceUpdate >= Constants.RADIATION_DAMAGE_INTERVAL)
        {
            UpdateQuery(World);

            elapsedSinceUpdate = 0;
        }
    }

    [Query]
    [All<Health, CellProperties>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref CompoundStorage compoundStorage, in Entity entity)
    {
        var compounds = compoundStorage.Compounds;

        var radiationAmount = compounds.GetCompoundAmount(Compound.Radiation);

        // Extra early return for cells with no radiation
        if (radiationAmount <= MathUtils.EPSILON)
            return;

        var radiationFraction = radiationAmount / compounds.GetCapacityForCompound(Compound.Radiation);

        if (radiationFraction < Constants.RADIATION_DAMAGE_THRESHOLD)
        {
            // Apply natural decay to radiation so that it doesn't infinitely stack in cells that cannot process it
            compounds.TakeCompound(Compound.Radiation, Constants.RADIATION_NATURAL_DECAY * elapsedSinceUpdate);
            return;
        }

        // Apply faster decay as it is not fun to take damage indefinitely
        compounds.TakeCompound(Compound.Radiation,
            Constants.RADIATION_NATURAL_DECAY_WHEN_TAKING_DAMAGE * elapsedSinceUpdate);

        ref var health = ref entity.Get<Health>();

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
            health.DealMicrobeDamage(ref entity.Get<CellProperties>(), rawDamage, "radiation",
                HealthHelpers.GetInstantKillProtectionThreshold(entity));

            entity.SendNoticeIfPossible(() =>
                new SimpleHUDMessage(Localization.Translate("NOTICE_RADIATION_DAMAGE"), DisplayDuration.Short));
        }
    }
}
