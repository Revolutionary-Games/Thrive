namespace Systems;

using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;

/// <summary>
///   Handles applying pilus damage to microbes
/// </summary>
[With(typeof(CollisionManagement))]
[With(typeof(MicrobePhysicsExtraData))]
[With(typeof(SpeciesMember))]
[ReadsComponent(typeof(CollisionManagement))]
[ReadsComponent(typeof(MicrobePhysicsExtraData))]
[ReadsComponent(typeof(CellProperties))]
[ReadsComponent(typeof(MicrobeColony))]
[ReadsComponent(typeof(Health))]
[WritesToComponent(typeof(DamageCooldown))]
[RunsAfter(typeof(PhysicsCollisionManagementSystem))]
[RuntimeCost(1)]
public sealed class PilusDamageSystem : AEntitySetSystem<float>
{
    public PilusDamageSystem(World world, IParallelRunner parallelRunner) : base(world, parallelRunner)
    {
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var collisionManagement = ref entity.Get<CollisionManagement>();

        var count = collisionManagement.GetActiveCollisions(out var collisions);
        if (count < 1)
            return;

        bool isPlayer = entity.Has<PlayerMarker>();

        ref var ourExtraData = ref entity.Get<MicrobePhysicsExtraData>();
        ref var ourSpecies = ref entity.Get<SpeciesMember>();

        for (int i = 0; i < count; ++i)
        {
            ref var collision = ref collisions![i];

            // Only process just started collisions for pilus damage
            if (collision.JustStarted != 1)
                continue;

            if (!collision.SecondEntity.Has<MicrobePhysicsExtraData>())
                continue;

            ref var otherExtraData = ref collision.SecondEntity.Get<MicrobePhysicsExtraData>();

            bool otherIsPilus = otherExtraData.IsSubShapePilus(collision.SecondSubShapeData);
            bool oursIsPilus = ourExtraData.IsSubShapePilus(collision.FirstSubShapeData);

            // Pilus logic
            if (otherIsPilus && oursIsPilus)
            {
                // Pilus on pilus doesn't deal damage
                continue;
            }

            if (!oursIsPilus)
                continue;

            // Us attacking the other microbe. In the case the other entity is attacking us it will be
            // detected by that entity's physics callback

            // Disallow cannibalism
            if (ourSpecies.ID == collision.SecondEntity.Get<SpeciesMember>().ID)
                return;

            if (collision.SecondEntity.Has<MicrobeColony>())
            {
                if (collision.SecondEntity.Get<MicrobeColony>().GetMicrobeFromSubShape(ref otherExtraData,
                        collision.SecondSubShapeData, out var hitEntity))
                {
                    DealPilusDamage(ref ourExtraData, ref collision, hitEntity, isPlayer);
                    continue;
                }
            }

            DealPilusDamage(ref ourExtraData, ref collision, collision.SecondEntity, isPlayer);
        }
    }

    private void DealPilusDamage(ref MicrobePhysicsExtraData ourExtraData, ref PhysicsCollision collision,
        in Entity targetEntity, bool playerDealsDamage)
    {
        // Skip applying damage while the previous damage cooldown is still active
        ref var cooldown = ref collision.SecondEntity.Get<DamageCooldown>();

        if (cooldown.IsInCooldown())
            return;

        ref var targetHealth = ref targetEntity.Get<Health>();

        if (ourExtraData.IsSubShapeInjectisomeIfIsPilus(collision.FirstSubShapeData))
        {
            // Injectisome attack
            targetHealth.DealMicrobeDamage(ref collision.SecondEntity.Get<CellProperties>(),
                Constants.INJECTISOME_BASE_DAMAGE, "injectisome",
                HealthHelpers.GetInstantKillProtectionThreshold(targetEntity));

            cooldown.StartInjectisomeCooldown();
            return;
        }

        float damage = Constants.PILUS_BASE_DAMAGE * collision.PenetrationAmount;

        // Skip too small damage
        if (damage < 0.01f)
            return;

        if (damage > Constants.PILUS_MAX_DAMAGE)
            damage = Constants.PILUS_MAX_DAMAGE;

        var previousHealth = targetHealth.CurrentHealth;

        targetHealth.DealMicrobeDamage(ref collision.SecondEntity.Get<CellProperties>(), damage, "pilus",
            HealthHelpers.GetInstantKillProtectionThreshold(targetEntity));

        if (playerDealsDamage && previousHealth > 0 && targetHealth.CurrentHealth <= 0 && !targetHealth.Invulnerable)
        {
            // Player dealt lethal damage
            AchievementEvents.ReportPlayerMicrobeKill();
        }

        cooldown.StartDamageScaledCooldown(damage, Constants.PILUS_MIN_DAMAGE_TRIGGER_COOLDOWN,
            Constants.PILUS_MAX_DAMAGE, Constants.PILUS_MIN_COOLDOWN, Constants.PILUS_MAX_COOLDOWN);
    }
}
