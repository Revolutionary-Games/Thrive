namespace Systems;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Handles the various <see cref="MicrobeEventCallbacks"/> that are not handled directly by other systems.
///   This is mostly used just for the player
/// </summary>
/// <remarks>
///   <para>
///     This technically writes to <see cref="MicrobeStatus"/> but this system has a reserved variable in there
///     so this doesn't conflict with other systems.
///   </para>
/// </remarks>
[ReadsComponent(typeof(MicrobeEventCallbacks))]
[ReadsComponent(typeof(MicrobeStatus))]
[ReadsComponent(typeof(WorldPosition))]
[ReadsComponent(typeof(OrganelleContainer))]
[ReadsComponent(typeof(MicrobeColony))]
[RunsBefore(typeof(DamageSoundSystem))]
[RunsAfter(typeof(OrganelleTickSystem))]
[RunsAfter(typeof(SpawnSystem))]
[RunsAfter(typeof(MicrobeAISystem))]
[RuntimeCost(0.25f)]
public partial class MicrobeEventCallbackSystem : BaseSystem<World, float>
{
    private readonly IReadonlyCompoundClouds compoundClouds;
    private readonly ISpeciesMemberLocationData microbeLocationData;

    public MicrobeEventCallbackSystem(IReadonlyCompoundClouds compoundClouds,
        ISpeciesMemberLocationData microbeLocationData, World world) : base(world)
    {
        this.compoundClouds = compoundClouds;
        this.microbeLocationData = microbeLocationData;
    }

    [Query]
    [All<WorldPosition>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref MicrobeEventCallbacks callbacks, ref MicrobeStatus status,
        ref Health health, in Entity entity)
    {
        // Don't run callbacks for dead cells
        if (health.Dead)
            return;

        HandleChemoreceptorLines(entity, ref status, ref callbacks, delta);

        // Damage callbacks
        var damage = health.RecentDamageReceived;

        if (damage != null)
        {
            lock (damage)
            {
                ProcessDamageEvents(entity, damage);
            }
        }
    }

    private void HandleChemoreceptorLines(in Entity entity, ref MicrobeStatus status,
        ref MicrobeEventCallbacks callbacks, float delta)
    {
        if (callbacks.OnChemoreceptionInfo == null)
            return;

        status.TimeUntilChemoreceptionUpdate -= delta;

        if (status.TimeUntilChemoreceptionUpdate > 0)
            return;

        status.TimeUntilChemoreceptionUpdate = Constants.CHEMORECEPTOR_SEARCH_UPDATE_INTERVAL;

        if (!entity.Has<OrganelleContainer>())
        {
            GD.PrintErr($"Entity wanting chemoreception callback is missing {nameof(OrganelleContainer)}");
            return;
        }

        ref var organelleContainer = ref entity.Get<OrganelleContainer>();
        var position = entity.Get<WorldPosition>().Position;

        callbacks.OnChemoreceptionInfo.Invoke(entity,
            organelleContainer.PerformCompoundDetection(entity, position, compoundClouds),
            organelleContainer.PerformMicrobeDetections(entity, position, microbeLocationData));
    }

    private void ProcessDamageEvents(in Entity entity, List<DamageEventNotice> damageEvents)
    {
        foreach (var damageEvent in damageEvents)
        {
            if (damageEvent.DamageSource is "toxin")
            {
                // TODO: fix this, currently "toxin" is used both by microbes and chunks, as well as damage from
                // ingested toxins
                // OnNoticeMessage?.Invoke(this,
                //     new SimpleHUDMessage(Localization.Translate("NOTICE_DAMAGED_BY_ENVIRONMENTAL_TOXIN")));
            }
            else if (damageEvent.DamageSource == "atpDamage")
            {
                entity.SendNoticeIfPossible(() =>
                    new SimpleHUDMessage(Localization.Translate("NOTICE_DAMAGED_BY_NO_ATP"),
                        DisplayDuration.Short));
            }
        }
    }
}
