namespace Systems
{
    using System;
    using System.Collections.Generic;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using Newtonsoft.Json;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Plays the sounds from <see cref="SoundEffectPlayer"/>
    /// </summary>
    [With(typeof(SoundEffectPlayer))]
    [With(typeof(WorldPosition))]
    public sealed class SoundEffectSystem : AEntitySetSystem<float>
    {
        // TODO: probably could remove the hybrid player as this adds just some overhead which we could avoid by
        // having 2 lists of player types
        private readonly List<HybridAudioPlayer> createdPlayers = new();
        private readonly Node soundPlayerParent;

        [JsonProperty]
        private Vector3 playerPosition;

        public SoundEffectSystem(Node soundPlayerParentNode, World world, IParallelRunner runner) : base(world, runner)
        {
            soundPlayerParent = soundPlayerParentNode;
        }

        public void ReportPlayerPosition(Vector3 position)
        {
            playerPosition = position;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var soundEffectPlayer = ref entity.Get<SoundEffectPlayer>();

            if (soundEffectPlayer.SoundsApplied)
                return;

            ref var position = ref entity.Get<WorldPosition>();

            // TODO: actually playing the sounds
            throw new NotImplementedException();

            soundEffectPlayer.SoundsApplied = true;
        }
    }
}
