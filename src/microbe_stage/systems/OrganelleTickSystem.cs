namespace Systems
{
    using System.Collections.Concurrent;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles calling <see cref="IOrganelleComponent.UpdateAsync"/> and other tick methods on organelles each game
    ///   update
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This runs after <see cref="MicrobeMovementSystem"/> as this mostly deals with animating movement
    ///     organelles. Other operations are less time sensitive so they are fine to be detected next frame.
    ///   </para>
    /// </remarks>
    [With(typeof(OrganelleContainer))]
    [With(typeof(CompoundStorage))]
    [With(typeof(WorldPosition))]
    [ReadsComponent(typeof(Engulfable))]
    [ReadsComponent(typeof(MicrobeControl))]
    [ReadsComponent(typeof(Physics))]
    [ReadsComponent(typeof(WorldPosition))]
    [RunsAfter(typeof(MicrobeMovementSystem))]
    [RunsOnMainThread]
    public sealed class OrganelleTickSystem : AEntitySetSystem<float>
    {
        private readonly ConcurrentStack<(IOrganelleComponent Component, Entity Entity)> queuedSyncRuns = new();

        public OrganelleTickSystem(World world, IParallelRunner parallelRunner) : base(world, parallelRunner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var organelleContainer = ref entity.Get<OrganelleContainer>();

            if (organelleContainer.Organelles == null)
                return;

            // Clear state that needs to be rebuilt each frame
            organelleContainer.ActiveCompoundDetections?.Clear();
            organelleContainer.ActiveSpeciesDetections?.Clear();

            foreach (var organelle in organelleContainer.Organelles.Organelles)
            {
                foreach (var component in organelle.Components)
                {
                    component.UpdateAsync(ref organelleContainer, entity, delta);

                    if (component.UsesSyncProcess)
                        queuedSyncRuns.Push((component, entity));
                }
            }
        }

        protected override void PostUpdate(float delta)
        {
            base.PostUpdate(delta);

            while (queuedSyncRuns.TryPop(out var entry))
            {
                // TODO: determine if it is a good idea to always fetch the container like for UpdateAsync here
                // ref entry.Entity.Get<OrganelleContainer>()
                entry.Component.UpdateSync(entry.Entity, delta);
            }

            if (!queuedSyncRuns.IsEmpty)
                GD.PrintErr("Queued sync runs for organelle updates is not empty after processing");
        }
    }
}
