namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;

/// <summary>
///   System that deletes nodes that are in the timed group after their lifespan expires.
/// </summary>
[RuntimeCost(0.25f)]
public partial class TimedLifeSystem(IEntityContainer entityContainer, World world) : BaseSystem<World, float>(world)
{
    /// <summary>
    ///   Despawns all timed entities
    /// </summary>
    public void DespawnAll()
    {
        World.Query(new QueryDescription().WithAll<TimedLife>(), entity => entityContainer.DestroyEntity(entity));
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref TimedLife timed, in Entity entity)
    {
        // This system now also handles fading timing
        if (timed.FadeTimeRemainingSet)
        {
            timed.FadeTimeRemaining -= delta;

            if (timed.FadeTimeRemaining <= 0)
            {
                // Fade time ended
                entityContainer.DestroyEntity(entity);
            }

            return;
        }

        timed.TimeToLiveRemaining -= delta;

        if (timed.TimeToLiveRemaining <= 0.0f && !timed.OnTimeOverTriggered)
        {
            timed.OnTimeOverTriggered = true;
            var callback = timed.CustomTimeOverCallback;

            // If there is a custom callback, call it first as it can set the fade time
            bool wantsToLive = callback != null && !callback.Invoke(entity, ref timed);

            if (timed.FadeTimeRemainingSet && timed.FadeTimeRemaining > 0)
            {
                // Entity doesn't want to die just yet
                wantsToLive = true;
            }

            if (!wantsToLive)
            {
                entityContainer.DestroyEntity(entity);
            }
            else
            {
                // Disable saving for this entity as fade out states are not programmed to resume well after loading
                // a save
                entityContainer.ReportEntityDyingSoon(entity);
            }
        }
    }
}
