namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;

/// <summary>
///   Processes cooldowns for <see cref="DamageCooldown"/>
/// </summary>
[RunsBefore(typeof(ToxinCollisionSystem))]
[RunsBefore(typeof(PilusDamageSystem))]
[RunsBefore(typeof(DamageOnTouchSystem))]
[RuntimeCost(0.25f)]
public partial class DamageCooldownSystem : BaseSystem<World, float>
{
    public DamageCooldownSystem(World world) : base(world)
    {
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref DamageCooldown cooldown)
    {
        if (cooldown.CooldownRemaining <= 0)
            return;

        cooldown.CooldownRemaining -= delta;

        if (cooldown.CooldownRemaining < 0)
            cooldown.CooldownRemaining = 0;
    }
}
