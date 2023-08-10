namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Plays the sounds from <see cref="SoundEffectPlayer"/>
    /// </summary>
    [With(typeof(SoundListener))]
    [With(typeof(WorldPosition))]
    public sealed class SoundListenerSystem : AEntitySetSystem<float>
    {
        private readonly Listener listener;

        private Transform? wantedListenerPosition;

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
                listener.GlobalTransform = wantedListenerPosition.Value;

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
