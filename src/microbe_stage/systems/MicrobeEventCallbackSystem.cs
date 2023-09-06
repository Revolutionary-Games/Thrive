namespace Systems
{
    using System.Collections.Generic;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
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
    [RunsBefore(typeof(DamageSoundSystem))]
    public sealed class MicrobeEventCallbackSystem : AEntitySetSystem<float>
    {
        private readonly IReadonlyCompoundClouds compoundClouds;

        public MicrobeEventCallbackSystem(IReadonlyCompoundClouds compoundClouds, World world,
            IParallelRunner parallelRunner) : base(world, parallelRunner)
        {
            this.compoundClouds = compoundClouds;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var callbacks = ref entity.Get<MicrobeEventCallbacks>();
            ref var status = ref entity.Get<MicrobeStatus>();

            HandleChemoreceptorLines(entity, ref status, ref callbacks, delta);

            // Damage callbacks
            ref var health = ref entity.Get<Health>();

            var damage = health.RecentDamageReceived;

            if (damage != null)
            {
                lock (damage)
                {
                    ProcessDamageEvents(entity, ref callbacks, damage);
                }
            }
        }

        private void HandleChemoreceptorLines(in Entity entity, ref MicrobeStatus status,
            ref MicrobeEventCallbacks callbacks, float delta)
        {
            if (callbacks.OnCompoundChemoreceptionInfo == null)
                return;

            status.TimeUntilChemoreceptionUpdate -= delta;

            if (status.TimeUntilChemoreceptionUpdate > 0)
                return;

            status.TimeUntilChemoreceptionUpdate = Constants.CHEMORECEPTOR_COMPOUND_UPDATE_INTERVAL;

            if (!entity.Has<OrganelleContainer>())
            {
                GD.PrintErr($"Entity wanting chemoreception callback is missing {nameof(OrganelleContainer)}");
                return;
            }

            callbacks.OnCompoundChemoreceptionInfo.Invoke(entity,
                entity.Get<OrganelleContainer>()
                    .PerformCompoundDetection(entity, entity.Get<WorldPosition>().Position, compoundClouds));
        }

        private void ProcessDamageEvents(in Entity entity, ref MicrobeEventCallbacks callbacks,
            List<DamageEventNotice> damageEvents)
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
                    callbacks.OnNoticeMessage?.Invoke(entity,
                        new SimpleHUDMessage(TranslationServer.Translate("NOTICE_DAMAGED_BY_NO_ATP"),
                            DisplayDuration.Short));
                }
            }
        }
    }
}
