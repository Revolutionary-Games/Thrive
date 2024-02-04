namespace Systems
{
    using System.Collections.Generic;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles the various <see cref="MicrobeEventCallbacks"/> that are not handled directly by other systems.
    ///   This is mostly used just for the player
    /// </summary>
    [With(typeof(MicrobeEventCallbacks))]
    [With(typeof(MicrobeStatus))]
    [With(typeof(Health))]
    [With(typeof(WorldPosition))]
    [ReadsComponent(typeof(WorldPosition))]
    [RunsBefore(typeof(DamageSoundSystem))]
    [RunsAfter(typeof(OrganelleTickSystem))]
    [RunsAfter(typeof(SpawnSystem))]
    [RunsAfter(typeof(MicrobeAISystem))]
    [RunsOnMainThread]
    public sealed class MicrobeEventCallbackSystem : AEntitySetSystem<float>
    {
        private readonly IReadonlyCompoundClouds compoundClouds;
        private readonly ISpeciesMemberLocationData microbeLocationData;

        public MicrobeEventCallbackSystem(IReadonlyCompoundClouds compoundClouds,
            ISpeciesMemberLocationData microbeLocationData, World world) :
            base(world, null)
        {
            this.compoundClouds = compoundClouds;
            this.microbeLocationData = microbeLocationData;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var callbacks = ref entity.Get<MicrobeEventCallbacks>();
            ref var status = ref entity.Get<MicrobeStatus>();
            ref var health = ref entity.Get<Health>();

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
                    //     new SimpleHUDMessage(TranslationServer.Translate("NOTICE_DAMAGED_BY_ENVIRONMENTAL_TOXIN")));
                }
                else if (damageEvent.DamageSource == "atpDamage")
                {
                    entity.SendNoticeIfPossible(() =>
                        new SimpleHUDMessage(TranslationServer.Translate("NOTICE_DAMAGED_BY_NO_ATP"),
                            DisplayDuration.Short));
                }
            }
        }
    }
}
