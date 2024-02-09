namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Processes cooldowns for <see cref="DamageCooldown"/>
    /// </summary>
    [With(typeof(DamageCooldown))]
    [RunsBefore(typeof(ToxinCollisionSystem))]
    [RunsBefore(typeof(PilusDamageSystem))]
    [RunsBefore(typeof(DamageOnTouchSystem))]
    [RuntimeCost(0.25f)]
    public sealed class DamageCooldownSystem : AEntitySetSystem<float>
    {
        public DamageCooldownSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var cooldown = ref entity.Get<DamageCooldown>();

            if (cooldown.CooldownRemaining <= 0)
                return;

            cooldown.CooldownRemaining -= delta;

            if (cooldown.CooldownRemaining < 0)
                cooldown.CooldownRemaining = 0;
        }
    }
}
