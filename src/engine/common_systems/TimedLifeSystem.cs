﻿namespace Systems;

using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;

/// <summary>
///   System that deletes nodes that are in the timed group after their lifespan expires.
/// </summary>
/// <remarks>
///   <para>
///      See the TODOs on <see cref="FadeOutActionSystem"/> why this is marked as needing to run on the main thread.
///   </para>
/// </remarks>
[With(typeof(TimedLife))]
[RuntimeCost(0.25f)]
[RunsOnMainThread]
public sealed class TimedLifeSystem : AEntitySetSystem<float>
{
    private readonly IEntityContainer entityContainer;

    public TimedLifeSystem(IEntityContainer entityContainer, World world, IParallelRunner runner) :
        base(world, runner)
    {
        this.entityContainer = entityContainer;
    }

    /// <summary>
    ///   Despawns all timed entities
    /// </summary>
    public void DespawnAll()
    {
        foreach (var entity in World.GetEntities().With<TimedLife>().AsEnumerable())
        {
            entityContainer.DestroyEntity(entity);
        }
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var timed = ref entity.Get<TimedLife>();

        // Fading timing is now also handled by this system
        if (timed.FadeTimeRemaining != null)
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

            // If there is a custom callback call it first as it can set the fade time
            bool wantsToLive = callback != null && !callback.Invoke(entity, ref timed);

            if (timed.FadeTimeRemaining != null && timed.FadeTimeRemaining.Value > 0)
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
