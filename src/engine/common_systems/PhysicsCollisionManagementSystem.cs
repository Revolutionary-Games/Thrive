namespace Systems
{
    using System;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    [With(typeof(Physics))]
    [With(typeof(CollisionManagement))]
    public sealed class PhysicsCollisionManagementSystem : AEntitySetSystem<float>
    {
        public PhysicsCollisionManagementSystem(World world, IParallelRunner runner)
            : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var physics = ref entity.Get<Physics>();
            ref var collisionManagement = ref entity.Get<CollisionManagement>();

            if (collisionManagement.Dirty)
            {
                collisionManagement.Dirty = false;

                throw new NotImplementedException();
            }
        }

        protected void DisableAllCollisions(body)
        {
            if (AllCollisionsDisabled)
                return;

            if (Body == null || !CheckWeHaveWorldReference())
                return;

            AllCollisionsDisabled = true;

            BodyCreatedInWorld!.SetBodyCollisionsEnabledState(Body, false);
        }

        protected void EnableCollisions()
        {
            if (!AllCollisionsDisabled)
                return;

            if (Body == null || !CheckWeHaveWorldReference())
                return;

            AllCollisionsDisabled = false;

            BodyCreatedInWorld!.SetBodyCollisionsEnabledState(Body, false);
        }

        protected void DisableCollisionsWith(PhysicsBody otherBody)
        {
            if (Body == null || !CheckWeHaveWorldReference())
                return;

            try
            {
                BodyCreatedInWorld!.BodyIgnoreCollisionsWithBody(Body, otherBody);
            }
            catch (Exception e)
            {
                GD.PrintErr("Cannot ignore collisions with another body: ", e);
            }
        }

        protected void RestoreCollisionsWith(PhysicsBody otherBody)
        {
            if (Body == null || !CheckWeHaveWorldReference())
                return;

            try
            {
                BodyCreatedInWorld!.BodyRemoveCollisionIgnoreWith(Body, otherBody);
            }
            catch (Exception e)
            {
                GD.PrintErr("Cannot remove collision ignore with another body: ", e);
            }
        }
    }
}
