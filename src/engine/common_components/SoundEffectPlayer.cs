namespace Components
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    ///   Entity that can play sound effects (short sounds). Requires a <see cref="WorldPosition"/> to function.
    /// </summary>
    public struct SoundEffectPlayer
    {
        public SoundEffectSlot[]? SoundEffectSlots;

        /// <summary>
        ///   If not 0 then this is the max distance from the player that this sound player will play anything at all
        /// </summary>
        public float AbsoluteMaxDistance;

        /// <summary>
        ///   When true the played sounds are automatically played in 2D for the player's entity
        /// </summary>
        public bool AutoDetectPlayer;

        [JsonIgnore]
        public bool SoundsApplied;
    }

    public struct SoundEffectSlot
    {
        /// <summary>
        ///   Set to true when this slot should play. Automatically unset when the sound finishes by the sound system
        /// </summary>
        public bool Play;

        /// <summary>
        ///   If true then this sound keeps playing (looping) and <see cref="Play"/> never automatically stops
        /// </summary>
        public bool Loop;

        /// <summary>
        ///   Volume multiplier for this sound. Should be in range [0, 1]
        /// </summary>
        public float Volume;

        /// <summary>
        ///   Internal flag don't touch
        /// </summary>
        [JsonIgnore]
        public bool InternalPlayingState;
    }

    public static class SoundEffectPlayerHelpers
    {
        /// <summary>
        ///   Starts playing a new sound effect
        /// </summary>
        /// <param name="soundEffectPlayer">The player component to use</param>
        /// <returns>True if the sound was started, false if all playing slots were full already</returns>
        public static bool PlaySound(this ref SoundEffectPlayer soundEffectPlayer)
        {
            SoundEffectSlot[]? slots = soundEffectPlayer.SoundEffectSlots;

            if (slots == null)
            {
                slots ??= new SoundEffectSlot[Constants.MAX_CONCURRENT_SOUNDS_PER_ENTITY];
                soundEffectPlayer.SoundEffectSlots = slots;
            }

            lock (slots)
            {
                int slotCount = slots.Length;

                for (int i = 0; i < slotCount; ++i)
                {
                    ref var slot = ref slots[i];

                    if (!slot.Play)
                    {
                        // Found an empty slot
                        soundEffectPlayer.SoundsApplied = false;

                        // TODO: actually set the sound data
                        slot.Play = true;
                        slot.Volume = 1;
                        throw new NotImplementedException();

                        return true;
                    }
                }
            }

            return false;
        }
    }
}
