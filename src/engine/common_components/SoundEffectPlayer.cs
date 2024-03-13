namespace Components;

using System.Runtime.CompilerServices;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Entity that can play sound effects (short sounds). Requires a <see cref="WorldPosition"/> to function.
/// </summary>
[ComponentIsReadByDefault]
[JSONDynamicTypeAllowed]
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

                if (!IsSlotReadyForReUse(ref slot))
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
    ///   Plays a sound effect if it isn't already playing
    /// </summary>
    /// <returns>True if now playing or was playing already</returns>
    public static bool PlaySoundEffectIfNotPlayingAlready(this ref SoundEffectPlayer soundEffectPlayer,
        string sound, float volume = 1)
    {
        return soundEffectPlayer.EnsureSoundIsPlaying(sound, false, volume);
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

        return soundEffectPlayer.EnsureSoundIsPlaying(sound, true, volume);
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
    public static bool StopSound(this ref SoundEffectPlayer soundEffectPlayer, string sound,
        bool immediately = true)
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
                soundEffectPlayer.SoundsApplied = false;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///   Stops all sounds that are currently playing
    /// </summary>
    /// <param name="soundEffectPlayer">The player that may have sounds</param>
    /// <param name="immediately">
    ///   If false, any looping sounds will only stop playing after their current loop ends
    /// </param>
    public static void StopAllSounds(this ref SoundEffectPlayer soundEffectPlayer, bool immediately = true)
    {
        SoundEffectSlot[]? slots = soundEffectPlayer.SoundEffectSlots;

        if (slots == null)
            return;

        lock (slots)
        {
            int slotCount = slots.Length;

            for (int i = 0; i < slotCount; ++i)
            {
                ref var slot = ref slots[i];

                if (!slot.Play)
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
            }

            soundEffectPlayer.SoundsApplied = false;
        }
    }

    /// <summary>
    ///   Handles starting and turning up a looping sound effect. Used for microbe movement sound handling,
    ///   for example.
    /// </summary>
    /// <returns>True when the sound is now playing, false if out of slots</returns>
    public static bool PlayGraduallyTurningUpLoopingSound(this ref SoundEffectPlayer soundEffectPlayer,
        string sound, float maxVolume, float initialVolume, float changeSpeed)
    {
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
                    var targetVolume = Mathf.Clamp(slot.Volume + changeSpeed, 0, maxVolume);

                    // The volume mostly changes until it reaches the max volume which is always the exact same
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (slot.Volume != targetVolume || !slot.Loop || !slot.Play)
                    {
                        slot.Volume = targetVolume;
                        slot.Play = true;
                        slot.Loop = true;

                        slot.InternalAppliedState = false;
                        soundEffectPlayer.SoundsApplied = false;
                    }

                    return true;
                }

                if (IsSlotReadyForReUse(ref slot) && emptySlot == -1)
                {
                    emptySlot = i;
                }
            }

            // Need a new slot
            if (emptySlot != -1)
            {
                ref var slot = ref slots[emptySlot];

                slot.SoundFile = sound;
                slot.Volume = initialVolume;
                slot.Loop = true;
                slot.Play = true;

                slot.InternalAppliedState = false;
                soundEffectPlayer.SoundsApplied = false;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///   The opposite of <see cref="PlayGraduallyTurningUpLoopingSound"/> for handling stopping sounds started
    ///   like that
    /// </summary>
    /// <returns>True if there was a sound to lower volume of or stop</returns>
    public static bool PlayGraduallyTurningDownSound(this ref SoundEffectPlayer soundEffectPlayer, string sound,
        float changeSpeed)
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

                slot.Loop = false;
                slot.InternalAppliedState = false;
                soundEffectPlayer.SoundsApplied = false;

                var targetVolume = slot.Volume - changeSpeed;

                if (targetVolume <= 0)
                {
                    // Immediately stop when volume reaches zero
                    slot.Play = false;
                    return true;
                }

                slot.Volume = targetVolume;

                return true;
            }
        }

        return false;
    }

    public static bool EnsureSoundIsPlaying(this ref SoundEffectPlayer soundEffectPlayer, string sound, bool loop,
        float volume)
    {
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
                if (slot.Play && slot.SoundFile == sound)
                {
                    // These are explicitly set by calling code so exact values should be fine
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (slot.Volume != volume || slot.Loop != loop)
                    {
                        slot.Volume = volume;
                        slot.Loop = loop;
                        slot.InternalAppliedState = false;
                        soundEffectPlayer.SoundsApplied = false;
                    }

                    return true;
                }

                if (IsSlotReadyForReUse(ref slot) && emptySlot == -1)
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
                slot.Loop = loop;
                slot.Play = true;

                slot.InternalAppliedState = false;
                soundEffectPlayer.SoundsApplied = false;
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSlotReadyForReUse(ref SoundEffectSlot slot)
    {
        // The Play variable is used to both control the playback and to detect when it has ended, as such a slot
        // with Play false is not necessarily ready for re-use yet, so we kind of naughtily check the internal
        // state to detect when the slot is truly free
        return !slot.Play && slot.InternalPlayingState == 0;

        // TODO: would it be better to adjust the sound slot reuse logic when the slot is still playing?
        // There's currently an error print in HandleSoundEntityStateApply that triggers if a slot is reused
        // before its internal player is reset
    }
}
