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
        ///   If not 0 then this is the max distance from (squared) the player that this sound player will play
        ///   anything at all
        /// </summary>
        public float AbsoluteMaxDistanceSquared;

        /// <summary>
        ///   When true the played sounds are automatically played in 2D for the player's entity (by having a
        ///   <see cref="SoundListener"/> component that is active)
        /// </summary>
        public bool AutoDetectPlayer;

        [JsonIgnore]
        public bool SoundsApplied;
    }

    public struct SoundEffectSlot
    {
        /// <summary>
        ///   What this slot should be playing
        /// </summary>
        public string? SoundFile;

        /// <summary>
        ///   Volume multiplier for this sound. Should be in range [0, 1]
        /// </summary>
        public float Volume;

        /// <summary>
        ///   Set to true when this slot should play. Automatically unset when the sound finishes by the sound system
        /// </summary>
        public bool Play;

        /// <summary>
        ///   If true then this sound keeps playing (looping) and <see cref="Play"/> never automatically stops. Can
        ///   be set to false to stop the audio playing after the current loop or manually immediately stopped.
        /// </summary>
        public bool Loop;

        /// <summary>
        ///   Internal flag don't touch
        /// </summary>
        [JsonIgnore]
        public ushort InternalPlayingState;

        [JsonIgnore]
        public bool InternalAppliedState;
    }

    public static class SoundEffectPlayerHelpers
    {
        /// <summary>
        ///   Starts playing a new sound effect
        /// </summary>
        /// <param name="soundEffectPlayer">The player component to use</param>
        /// <param name="sound">The sound file to play</param>
        /// <param name="volume">Volume to play the sound at</param>
        /// <returns>True if the sound was started, false if all playing slots were full already</returns>
        public static bool PlaySoundEffect(this ref SoundEffectPlayer soundEffectPlayer, string sound, float volume = 1)
        {
            // There's a race condition here but it should only extremely rarely happen if two sounds want to start
            // at the exact same moment in time
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

                    if (slot.Play)
                        continue;

                    // Found an empty slot to play in
                    slot.SoundFile = sound;
                    slot.Volume = volume;
                    slot.Play = true;

                    slot.Loop = false;

                    slot.InternalAppliedState = false;
                    soundEffectPlayer.SoundsApplied = false;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   Plays a looping sound. If the sound is already playing only adjusts the volume, doesn't start another
        ///   sound.
        /// </summary>
        /// <param name="soundEffectPlayer">The player component to use</param>
        /// <param name="sound">The sound file to play in a looping way</param>
        /// <param name="volume">
        ///   Volume to play the sound at, note if currently playing this is immediately applied
        /// </param>
        /// <returns>True if sound is now playing, false if the sound could not be started</returns>
        public static bool PlayLoopingSound(this ref SoundEffectPlayer soundEffectPlayer, string sound,
            float volume = 1)
        {
            // TODO: should looping sounds steal slots from non-looping sounds to ensure looping sounds play?
            // When there aren't enough slots

            // See the comments in PlaySoundEffect
            SoundEffectSlot[]? slots = soundEffectPlayer.SoundEffectSlots;

            if (slots == null)
            {
                slots ??= new SoundEffectSlot[Constants.MAX_CONCURRENT_SOUNDS_PER_ENTITY];
                soundEffectPlayer.SoundEffectSlots = slots;
            }

            lock (slots)
            {
                int slotCount = slots.Length;

                int emptySlot = -1;

                for (int i = 0; i < slotCount; ++i)
                {
                    ref var slot = ref slots[i];

                    // Detect already playing sound
                    if (slot.SoundFile == sound)
                    {
                        // These are explicitly set by calling code so exact values should be fine
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        if (slot.Volume != volume || slot.Loop != true)
                        {
                            slot.Volume = volume;
                            slot.Loop = true;
                            slot.InternalAppliedState = false;
                            soundEffectPlayer.SoundsApplied = false;
                        }

                        return true;
                    }

                    if (!slot.Play && emptySlot == -1)
                    {
                        emptySlot = i;
                    }
                }

                // Need a new slot
                if (emptySlot != -1)
                {
                    ref var slot = ref slots[emptySlot];

                    slot.SoundFile = sound;
                    slot.Volume = volume;
                    slot.Loop = true;
                    slot.Play = true;

                    slot.InternalAppliedState = false;
                    soundEffectPlayer.SoundsApplied = false;
                }
            }

            return false;
        }

        /// <summary>
        ///   Stops a sound that is currently playing
        /// </summary>
        /// <param name="soundEffectPlayer">The player that may have the sound</param>
        /// <param name="sound">
        ///   The sound to stop. The first slot playing this sound is stopped. If there are multiple instances of the
        ///   same sound playing not all of them will be stopped.
        /// </param>
        /// <param name="immediately">
        ///   If false and the sound is looping, then it will only stop playing after the current loop ends
        /// </param>
        /// <returns>True if found a sound to stop</returns>
        public static bool StopSound(this ref SoundEffectPlayer soundEffectPlayer, string sound, bool immediately = true)
        {
            SoundEffectSlot[]? slots = soundEffectPlayer.SoundEffectSlots;

            if (slots == null)
                return false;

            lock (slots)
            {
                int slotCount = slots.Length;

                for (int i = 0; i < slotCount; ++i)
                {
                    ref var slot = ref slots[i];

                    if (!slot.Play || slot.SoundFile != sound)
                        continue;

                    if (slot.Loop && !immediately)
                    {
                        // Stop after current loop
                        slot.Loop = false;
                    }
                    else
                    {
                        slot.Play = false;
                    }

                    slot.InternalAppliedState = false;
                    return true;
                }
            }

            return false;
        }
    }
}
