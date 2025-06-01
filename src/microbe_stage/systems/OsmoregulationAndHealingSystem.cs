namespace Systems;

using System;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;

/// <summary>
///   Handles taking energy from microbes for osmoregulation (staying alive) cost and dealing damage if there's not
///   enough energy. If a microbe has non-zero ATP, then passive health regeneration happens.
/// </summary>
/// <remarks>
///   <para>
///     This is marked as just reading <see cref="MicrobeStatus"/> as this has a reserved variable in it just for
///     this systems use so writing to it doesn't conflict with other systems.
///   </para>
/// </remarks>
[With(typeof(OrganelleContainer))]
[With(typeof(CellProperties))]
[With(typeof(MicrobeStatus))]
[With(typeof(CompoundStorage))]
[With(typeof(Engulfable))]
[With(typeof(SpeciesMember))]
[With(typeof(Health))]
[ReadsComponent(typeof(OrganelleContainer))]
[ReadsComponent(typeof(CellProperties))]
[ReadsComponent(typeof(MicrobeStatus))]
[ReadsComponent(typeof(Engulfable))]
[ReadsComponent(typeof(MicrobeColony))]
[ReadsComponent(typeof(MicrobeColonyMember))]
[ReadsComponent(typeof(MicrobeEnvironmentalEffects))]
[RunsAfter(typeof(PilusDamageSystem))]
[RunsAfter(typeof(DamageOnTouchSystem))]
[RunsAfter(typeof(ToxinCollisionSystem))]
[RuntimeCost(4)]
public sealed class OsmoregulationAndHealingSystem : AEntitySetSystem<float>
{
    private GameWorld? gameWorld;

    public OsmoregulationAndHealingSystem(World world, IParallelRunner parallelRunner) :
        base(world, parallelRunner)
    {
    }

    public void SetWorld(GameWorld world)
    {
        gameWorld = world;
    }

    protected override void PreUpdate(float state)
    {
        base.PreUpdate(state);

        if (gameWorld == null)
            throw new InvalidOperationException("GameWorld not set");
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var status = ref entity.Get<MicrobeStatus>();
        ref var health = ref entity.Get<Health>();
        ref var cellProperties = ref entity.Get<CellProperties>();

        // Dead cells may not regenerate health
        if (health.Dead || health.CurrentHealth <= 0)
            return;

        var compounds = entity.Get<CompoundStorage>().Compounds;

        HandleHitpointsRegeneration(ref health, compounds, delta);

        TakeOsmoregulationEnergyCost(entity, ref cellProperties, compounds, delta);

        HandleOsmoregulationDamage(entity, ref status, ref health, ref cellProperties, compounds, delta);

        // There used to be the engulfing mode ATP handling here, but it is now in EngulfingSystem as it makes more
        // sense to be in there
    }

    private void HandleOsmoregulationDamage(in Entity entity, ref MicrobeStatus status, ref Health health,
        ref CellProperties cellProperties, CompoundBag compounds, float delta)
    {
        status.LastCheckedATPDamage += delta;

        // TODO: should this loop be made into a single if to ensure that ATP damage can't stack a lot if the game
        // lags?
        while (status.LastCheckedATPDamage >= Constants.ATP_DAMAGE_CHECK_INTERVAL)
        {
            status.LastCheckedATPDamage -= Constants.ATP_DAMAGE_CHECK_INTERVAL;

            // When engulfed osmoregulation cost is not taken
            if (entity.Get<Engulfable>().PhagocytosisStep != PhagocytosisPhase.None)
                return;

            ApplyATPDamage(compounds, ref health, ref cellProperties, entity);
        }
    }

    private void TakeOsmoregulationEnergyCost(in Entity entity, ref CellProperties cellProperties,
        CompoundBag compounds, float delta)
    {
        ref var organelles = ref entity.Get<OrganelleContainer>();

        float environmentalMultiplier = 1.0f;

        var osmoregulationCost = organelles.HexCount * cellProperties.MembraneType.OsmoregulationFactor *
            Constants.ATP_COST_FOR_OSMOREGULATION * delta;

        int colonySize = 0;
        if (entity.Has<MicrobeColony>())
        {
            colonySize = entity.Get<MicrobeColony>().ColonyMembers.Length;
        }
        else if (entity.Has<MicrobeColonyMember>())
        {
            if (entity.Get<MicrobeColonyMember>().GetColonyFromMember(out var colonyEntity))
            {
                colonySize = colonyEntity.Get<MicrobeColony>().ColonyMembers.Length;
            }
        }

        // 5% osmoregulation bonus per colony member
        if (colonySize != 0)
        {
            osmoregulationCost *= 20.0f / (20.0f + colonySize);
        }

        // TODO: remove this check on next save breakage point
        if (entity.Has<MicrobeEnvironmentalEffects>())
        {
            ref var environmentalEffects = ref entity.Get<MicrobeEnvironmentalEffects>();
            environmentalMultiplier = environmentalEffects.OsmoregulationMultiplier;

            // TODO: remove this safety check once it is no longer possible for this problem to happen
            // https://github.com/Revolutionary-Games/Thrive/issues/5928
            if (float.IsNaN(environmentalMultiplier) || environmentalMultiplier < 0)
            {
                GD.PrintErr("Microbe has invalid osmoregulation multiplier: ", environmentalMultiplier);

                // Reset the data to not spam the error
                environmentalEffects.OsmoregulationMultiplier = 1.0f;

                environmentalMultiplier = 1.0f;
            }
        }

        osmoregulationCost *= environmentalMultiplier;

        // Only player species benefits from lowered osmoregulation
        if (entity.Get<SpeciesMember>().Species.PlayerSpecies)
            osmoregulationCost *= gameWorld!.WorldSettings.OsmoregulationMultiplier;

        compounds.TakeCompound(Compound.ATP, osmoregulationCost);
    }

    /// <summary>
    ///   Damage the microbe if it's too low on ATP.
    /// </summary>
    private void ApplyATPDamage(CompoundBag compounds, ref Health health, ref CellProperties cellProperties,
        in Entity entity)
    {
        if (compounds.GetCompoundAmount(Compound.ATP) > Constants.ATP_DAMAGE_THRESHOLD)
            return;

        health.DealMicrobeDamage(ref cellProperties, health.MaxHealth * Constants.NO_ATP_DAMAGE_FRACTION,
            "atpDamage", HealthHelpers.GetInstantKillProtectionThreshold(entity));
    }

    /// <summary>
    ///   Regenerate hitpoints while the cell has atp
    /// </summary>
    private void HandleHitpointsRegeneration(ref Health health, CompoundBag compounds, float delta)
    {
        if (health.HealthRegenCooldown > 0)
        {
            health.HealthRegenCooldown -= delta;
        }
        else
        {
            if (health.CurrentHealth >= health.MaxHealth)
                return;

            var atpAmount = compounds.GetCompoundAmount(Compound.ATP);
            if (atpAmount < Constants.HEALTH_REGENERATION_ATP_THRESHOLD)
            {
                // Allow small cells to heal if they are almost full on ATP
                if (atpAmount / compounds.GetCapacityForCompound(Compound.ATP) <
                    Constants.HEALTH_REGENERATION_ALTERNATIVE_ATP_FRACTION)
                {
                    return;
                }
            }

            health.CurrentHealth += Constants.HEALTH_REGENERATION_RATE * delta;
            if (health.CurrentHealth > health.MaxHealth)
            {
                health.CurrentHealth = health.MaxHealth;
            }
        }
    }
}
