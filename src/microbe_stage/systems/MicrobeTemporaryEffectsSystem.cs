namespace Systems;

using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;

/// <summary>
///   Handles applying the effects specified by <see cref="MicrobeTemporaryEffects"/>
/// </summary>
[With(typeof(MicrobeTemporaryEffects))]
[With(typeof(MicrobeControl))]
[With(typeof(CellProperties))]
[With(typeof(BioProcesses))]
[RunsAfter(typeof(ToxinCollisionSystem))]
public partial class MicrobeTemporaryEffectsSystem : BaseSystem<World, float>
{
    public MicrobeTemporaryEffectsSystem(World world) : base(world)
    {
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref TODO components, in Entity entity)
    {
        ref var temporaryEffects = ref entity.Get<MicrobeTemporaryEffects>();

        if (temporaryEffects.StateApplied)
            return;

        bool hasDebuff = false;

        // TODO: would be nice to have a place in the GUI to show with icons the current effects on the player

        if (temporaryEffects.SpeedDebuffDuration > 0)
        {
            temporaryEffects.SpeedDebuffDuration -= delta;
            hasDebuff = true;

            // The speed debuff is directly taken into account by MicrobeMovementSystem
        }

        ref var processes = ref entity.Get<BioProcesses>();

        if (temporaryEffects.ATPDebuffDuration > 0)
        {
            temporaryEffects.ATPDebuffDuration -= delta;
            hasDebuff = true;

            // Make sure effect is applied
            processes.ATPProductionSpeedModifier = 1 - Constants.CHANNEL_INHIBITOR_ATP_DEBUFF;
        }
        else
        {
            // Remove effect
            processes.ATPProductionSpeedModifier = 0;
        }

        if (!hasDebuff)
        {
            // When no longer need to handle this microbe, set it to be handled
            temporaryEffects.StateApplied = true;
        }
    }
}
