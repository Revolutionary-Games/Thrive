namespace Systems;

using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;

/// <summary>
///   Handles applying the effects specified by <see cref="MicrobeTemporaryEffects"/>
/// </summary>
[With(typeof(MicrobeTemporaryEffects))]
[With(typeof(MicrobeControl))]
[With(typeof(CellProperties))]
[With(typeof(BioProcesses))]
[RunsAfter(typeof(ToxinCollisionSystem))]
public class MicrobeTemporaryEffectsSystem : AEntitySetSystem<float>
{
    public MicrobeTemporaryEffectsSystem(World world, IParallelRunner parallelRunner) : base(world, parallelRunner)
    {
    }

    protected override void Update(float delta, in Entity entity)
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
