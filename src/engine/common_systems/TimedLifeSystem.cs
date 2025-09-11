namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;

/// <summary>
///   System that deletes nodes that are in the timed group after their lifespan expires.
/// </summary>
/// <remarks>
///   <para>
///     See the TODOs on <see cref="FadeOutActionSystem"/> why this is marked as needing to run on the main thread.
///   </para>
/// </remarks>
[RuntimeCost(0.25f)]
[RunsOnMainThread]
public partial class TimedLifeSystem : BaseSystem<World, float>
{
    private readonly IEntityContainer entityContainer;

    public TimedLifeSystem(IEntityContainer entityContainer, World world) : base(world)
    {
        this.entityContainer = entityContainer;
    }

    /// <summary>
    ///   Despawns all timed entities
    /// </summary>
    public void DespawnAll()
    {
        World.Query(new QueryDescription().WithAll<TimedLife>(), entity =>
        {
            entityContainer.DestroyEntity(entity);
        });
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref TimedLife timed, in Entity entity)
    {
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

            // If there is a custom callback, call it first as it can set the fade time
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
