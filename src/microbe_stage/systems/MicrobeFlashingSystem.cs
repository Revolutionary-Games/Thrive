namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles flashing microbes different colour based on the mode they are in or if they are taking damage. Needs
    ///   to run before the damage events are cleared.
    /// </summary>
    [With(typeof(MicrobeControl))]
    [With(typeof(ColourAnimation))]
    [With(typeof(Health))]
    [With(typeof(Selectable))]
    [RunsAfter(typeof(OsmoregulationAndHealingSystem))]
    [RunsBefore(typeof(DamageSoundSystem))]
    public sealed class MicrobeFlashingSystem : AEntitySetSystem<float>
    {
        public MicrobeFlashingSystem(World world, IParallelRunner parallelRunner) : base(world, parallelRunner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var animation = ref entity.Get<ColourAnimation>();

            if (HasReceivedDamage(entity))
            {
                // Flash the microbe red
                animation.Flash(new Color(1, 0, 0, 0.5f), Constants.MICROBE_FLASH_DURATION, 1);
                return;
            }

            // Flash based on current state of the microbe
            ref var control = ref entity.Get<MicrobeControl>();

            switch (control.State)
            {
                default:
                case MicrobeState.Normal:
                    break;
                case MicrobeState.Binding:
                    animation.Flash(new Color(0.2f, 0.5f, 0.0f, 0.5f), Constants.MICROBE_FLASH_DURATION);
                    break;
                case MicrobeState.Unbinding:
                {
                    if (entity.Get<Selectable>().Selected)
                    {
                        animation.Flash(new Color(1.0f, 0.0f, 0.0f, 0.5f), Constants.MICROBE_FLASH_DURATION);
                    }
                    else
                    {
                        animation.Flash(new Color(1.0f, 0.5f, 0.2f, 0.5f), Constants.MICROBE_FLASH_DURATION);
                    }

                    break;
                }

                case MicrobeState.Engulf:
                    // Flash the membrane blue.
                    animation.Flash(new Color(0.2f, 0.5f, 1.0f, 0.5f), Constants.MICROBE_FLASH_DURATION);
                    break;
            }
        }

        private bool HasReceivedDamage(in Entity entity)
        {
            ref var health = ref entity.Get<Health>();

            var damageEvents = health.RecentDamageReceived;

            if (damageEvents == null)
                return false;

            lock (damageEvents)
            {
                return damageEvents.Count > 0;
            }
        }
    }
}
