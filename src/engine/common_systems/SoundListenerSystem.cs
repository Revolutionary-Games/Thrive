namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Hears the sounds from <see cref="SoundEffectPlayer"/> (this marks where the player's ears are)
    /// </summary>
    [With(typeof(SoundListener))]
    [With(typeof(WorldPosition))]
    [ReadsComponent(typeof(WorldPosition))]
    [RunsAfter(typeof(PhysicsUpdateAndPositionSystem))]
    [RunsAfter(typeof(AttachedEntityPositionSystem))]
    [RunsOnMainThread]
    public sealed class SoundListenerSystem : AEntitySetSystem<float>
    {
        private readonly Listener listener;

        private Transform? wantedListenerPosition;

        private bool useTopDownOrientation;

        private bool printedError;

        public SoundListenerSystem(Node listenerParentNode, World world, IParallelRunner runner) : base(world, runner)
        {
            listener = new Listener();
            listener.ClearCurrent();
            listenerParentNode.AddChild(listener);
        }

        public override void Dispose()
        {
            Dispose(true);
            base.Dispose();

            // GC.SuppressFinalize(this);
        }

        protected override void PreUpdate(float state)
        {
            base.PreUpdate(state);

            wantedListenerPosition = null;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var soundListener = ref entity.Get<SoundListener>();

            if (soundListener.Disabled)
                return;

            ref var position = ref entity.Get<WorldPosition>();

            if (wantedListenerPosition != null)
            {
                if (!printedError)
                {
                    GD.PrintErr("Multiple SoundListener entities are active at once. Only last one will work! " +
                        "This error won't be printed again.");
                    printedError = true;
                }
            }

            useTopDownOrientation = soundListener.UseTopDownRotation;
            wantedListenerPosition = position.ToTransform();
        }

        protected override void PostUpdate(float state)
        {
            base.PostUpdate(state);

            if (wantedListenerPosition == null)
            {
                if (listener.IsCurrent())
                    listener.ClearCurrent();
            }
            else
            {
                if (useTopDownOrientation)
                {
                    // Listener is directional, so in this case we want to separate the rotation out from the entity
                    // transform to not use it
                    Transform transform = wantedListenerPosition.Value;
                    transform.basis = new Basis(new Vector3(0.0f, 0.0f, -1.0f));
                    listener.GlobalTransform = transform;
                }
                else
                {
                    listener.GlobalTransform = wantedListenerPosition.Value;
                }

                if (!listener.IsCurrent())
                    listener.MakeCurrent();
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                listener.Dispose();
            }
        }
    }
}
