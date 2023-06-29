namespace Systems
{
    using System;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles fading out animations on entities
    /// </summary>
    [With(typeof(FadeOutActions))]
    [With(typeof(TimedLife))]
    public sealed class FadeOutActionSystem : AEntitySetSystem<float>
    {
        public FadeOutActionSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var actions = ref entity.Get<FadeOutActions>();

            if (actions.CallbackRegistered)
                return;

            actions.CallbackRegistered = true;

            ref var timed = ref entity.Get<TimedLife>();

            // Register the callback which we use to trigger the actions for
            timed.CustomTimeOverCallback = PerformTimeOverActions;
        }

        private bool PerformTimeOverActions(Entity entity, ref TimedLife timedLife)
        {
            try
            {
                ref var actions = ref entity.Get<FadeOutActions>();

                if (actions.FadeTime <= 0)
                {
                    // For now this indicates bad data but in the future we might get some more custom "time over"
                    // actions
                    GD.PrintErr("Custom fade out actions fade time is zero");
                    return true;
                }

                timedLife.FadeTimeRemaining = actions.FadeTime;

                if (actions.DisableCollisions || actions.RemoveVelocity || actions.RemoveAngularVelocity)
                {
                    PerformPhysicsOperations(entity, actions.RemoveVelocity, actions.RemoveAngularVelocity,
                        actions.DisableCollisions);
                }

                if (actions.DisableParticles)
                    DisableParticleEmission(entity);

                if (actions.UsesMicrobialDissolveEffect)
                {
                    entity.StartDissolveAnimation(true);
                }

                if (actions.VentCompounds)
                {
                    // TODO: implement this
                    GD.PrintErr("TODO: implement vent compounds on fade");
                    throw new NotImplementedException();
                }

                // Fade started, don't destroy yet
                return false;
            }
            catch (Exception e)
            {
                GD.PrintErr("Failed to perform custom fade out actions on timer over: ", e);
                return true;
            }
        }

        private void PerformPhysicsOperations(Entity entity, bool removeVelocity, bool removeAngularVelocity,
            bool disableCollisions)
        {
            try
            {
                ref var physicsControl = ref entity.Get<ManualPhysicsControl>();

                physicsControl.RemoveVelocity = removeVelocity;
                physicsControl.RemoveAngularVelocity = removeAngularVelocity;

                if (disableCollisions)
                    physicsControl.DisableCollisions = disableCollisions;

                physicsControl.PhysicsApplied = false;
            }
            catch (Exception e)
            {
                GD.PrintErr(
                    $"Cannot apply all fade out actions due to missing {nameof(ManualPhysicsControl)} " +
                    "component on entity: ", e);
            }
        }

        private void DisableParticleEmission(Entity entity)
        {
            try
            {
                ref var spatial = ref entity.Get<SpatialInstance>();

                var particles = spatial.GraphicalInstance as Particles;

                if (particles == null)
                    throw new NullReferenceException("Graphical instance casted as particles is null");

                particles.Emitting = false;

                // TODO: do we need a feature to automatically read the particle lifetime here and then adjust the
                // fade out time accordingly?
                // particleFadeTimer = particles.Lifetime;
            }
            catch (Exception e)
            {
                GD.PrintErr(
                    $"Cannot apply all fade out actions due to missing {nameof(SpatialInstance)} " +
                    "or the visuals not being particles: ", e);
            }
        }
    }
}
