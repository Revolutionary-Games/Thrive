﻿namespace Systems;

using System;
using System.Collections.Generic;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using Godot;
using World = DefaultEcs.World;

/// <summary>
///   Plays the sounds from <see cref="SoundEffectPlayer"/>
/// </summary>
[With(typeof(SoundEffectPlayer))]
[With(typeof(WorldPosition))]
[ReadsComponent(typeof(WorldPosition))]
[WritesToComponent(typeof(SoundEffectPlayer))]
[RuntimeCost(25)]
[RunsOnMainThread]
public sealed class SoundEffectSystem : AEntitySetSystem<float>
{
    private const string AudioBus = "SFX";

    private readonly Dictionary<string, CachedSound> soundCache = new();
    private readonly List<string> soundCacheEntriesToClear = new();

    // TODO: could maybe pool the playing audio player wrappers?

    private readonly Stack<AudioStreamPlayer3D> freePositionalPlayers = new();
    private readonly List<PlayingPositionalPlayer> usedPositionalPlayers = new();

    private readonly Stack<AudioStreamPlayer> free2DPlayers = new();
    private readonly List<NonPlayingPositionalPlayer> used2DPlayers = new();

    private readonly List<(Entity Entity, float Distance)> entitiesThatNeedProcessing = new();

    private readonly Node soundPlayerParent;

    private int playingSoundCount;

    private float timeCounter;
    private float lastClearedSoundCacheTime;

    private ushort soundIdentifierCounter;

    private Vector3 playerPosition;

    public SoundEffectSystem(Node soundPlayerParentNode, World world) : base(world, null)
    {
        soundPlayerParent = soundPlayerParentNode;
    }

    public void ReportPlayerPosition(Vector3 position)
    {
        playerPosition = position;
    }

    public void FreeNodeResources()
    {
        // Free all used player nodes
        foreach (var player in freePositionalPlayers)
        {
            player.QueueFree();
        }

        freePositionalPlayers.Clear();

        foreach (var player in usedPositionalPlayers)
        {
            player.Player.QueueFree();
        }

        usedPositionalPlayers.Clear();

        foreach (var player in free2DPlayers)
        {
            player.QueueFree();
        }

        free2DPlayers.Clear();

        foreach (var player in used2DPlayers)
        {
            player.Player.QueueFree();
        }

        used2DPlayers.Clear();
    }

    public override void Dispose()
    {
        Dispose(true);
        base.Dispose();
    }

    protected override void PreUpdate(float delta)
    {
        base.PreUpdate(delta);

        timeCounter += delta;

        playingSoundCount = 0;

        // First check the status of any sound players to detect when some end playing, and handle looping
        int positionalCount = usedPositionalPlayers.Count;
        for (int i = 0; i < positionalCount; ++i)
        {
            var playerWrapper = usedPositionalPlayers[i];
            if (playerWrapper.Player.Playing)
            {
                ++playingSoundCount;

                // TODO: should very long sounds be stopped if the entity is dead?
            }
            else if (playerWrapper.Loop && playerWrapper.HasAliveEntity)
            {
                playerWrapper.Player.Play();
                ++playingSoundCount;
            }
            else
            {
                // Add to the free list
                playerWrapper.MarkEnded();
                freePositionalPlayers.Push(playerWrapper.Player);

                usedPositionalPlayers.RemoveWithoutPreservingOrder(i);
                --positionalCount;

                // Need to go back two spaces as this current slot may have something swapped into it now
                i -= 2;

                // Next loop increments i by one so invalid values are -2 and below as that + 1 won't be a valid
                // index
                if (i < -1)
                    break;
            }
        }

        int nonPositionalCount = used2DPlayers.Count;
        for (int i = 0; i < nonPositionalCount; ++i)
        {
            var playerWrapper = used2DPlayers[i];
            if (playerWrapper.Player.Playing)
            {
                ++playingSoundCount;
            }
            else if (playerWrapper.Loop && playerWrapper.HasAliveEntity)
            {
                playerWrapper.Player.Play();
                ++playingSoundCount;
            }
            else
            {
                playerWrapper.MarkEnded();
                free2DPlayers.Push(playerWrapper.Player);

                used2DPlayers.RemoveWithoutPreservingOrder(i);
                --nonPositionalCount;

                i -= 2;

                if (i < -1)
                    break;
            }
        }
    }

    protected override void Update(float delta, ReadOnlySpan<Entity> entities)
    {
        // Collect sound playing entities that need processing
        foreach (ref readonly var entity in entities)
        {
            ref var soundEffectPlayer = ref entity.Get<SoundEffectPlayer>();

            if (soundEffectPlayer.SoundsApplied)
                continue;

            ref var position = ref entity.Get<WorldPosition>();

            var distance = position.Position.DistanceSquaredTo(playerPosition);

            // Skip so far away players that they shouldn't be handled at all
            // TODO: maybe still stop sounds in these from playing? (for example if some code wanted to stop a
            // sound on an entity that doesn't get processed due to distance, that sound playing won't stop)
            if (soundEffectPlayer.AbsoluteMaxDistanceSquared > 0 &&
                distance > soundEffectPlayer.AbsoluteMaxDistanceSquared)
            {
                continue;
            }

            entitiesThatNeedProcessing.Add((entity, distance));
        }

        // Play sounds starting from the nearest to the player to make the concurrently playing limit
        // work correctly
        entitiesThatNeedProcessing.Sort((x, y) => (int)(x.Distance - y.Distance));

        HandleSoundEntityStateApply();

        entitiesThatNeedProcessing.Clear();
    }

    protected override void PostUpdate(float delta)
    {
        base.PostUpdate(delta);

        // Update active positional players to have the right positions for the sounds
        foreach (var usedPositionalPlayer in usedPositionalPlayers)
        {
            if (usedPositionalPlayer.GetUpdatedPositionIfEntityIsValid(out var position))
            {
                usedPositionalPlayer.Player.Position = position;
            }
        }

        ExpireOldAudioCacheEntries(delta);
    }

    private static void MarkSoundEndedOnEntityIfPossible(in Entity entity, uint slotId, string sound)
    {
        if (!entity.IsAlive)
            return;

        ref var entityPlayer = ref entity.Get<SoundEffectPlayer>();

        var slots = entityPlayer.SoundEffectSlots;

        if (slots == null)
            return;

        lock (slots)
        {
            var count = slots.Length;

            for (int i = 0; i < count; ++i)
            {
                ref var slot = ref slots[i];

                if (slot.InternalPlayingState == slotId && slot.SoundFile == sound)
                {
                    slot.InternalPlayingState = 0;
                    slot.Play = false;
                    return;
                }
            }

            // If no exact match, do a partial match to report the player is no longer associated with the
            // sound slot

            for (int i = 0; i < count; ++i)
            {
                ref var slot = ref slots[i];

                if (slot.InternalPlayingState == slotId)
                {
                    slot.InternalPlayingState = 0;

                    // Don't reset play here unconditionally, instead let the full update determine what to do next

                    if (slot.SoundFile == null)
                    {
                        // Situation was that the file to play was cleared, it should be fully safe here to reset
                        // play to false
                        slot.Play = false;
                    }

                    return;
                }
            }
        }
    }

    private void HandleSoundEntityStateApply()
    {
        foreach (var entry in entitiesThatNeedProcessing)
        {
            var entity = entry.Entity;

            ref var soundEffectPlayer = ref entity.Get<SoundEffectPlayer>();

            var slots = soundEffectPlayer.SoundEffectSlots;

            if (slots == null)
            {
                // Don't continuously re-check entities that haven't initialized their slots. When adding the slots
                // and a sound the helper methods will mark this as needing handling again
                soundEffectPlayer.SoundsApplied = true;
                continue;
            }

            bool play2D = soundEffectPlayer.AutoDetectPlayer && entity.Has<SoundListener>();

            bool skippedSomething = false;

            // Slots are locked so that they aren't modified while this system runs. If any systems modify the
            // sounds after this system, then that sound will be picked up on next sound update
            lock (slots)
            {
                // The slots are intentionally left unlocked as if sound effects are modified while this system
                // runs it would lead to sometimes unexpected results

                int slotCount = slots.Length;

                for (int i = 0; i < slotCount; ++i)
                {
                    ref var slot = ref slots[i];

                    if (slot.InternalAppliedState)
                        continue;

                    if (!slot.Play)
                    {
                        // See if there is a sound to stop

                        if (slot.InternalPlayingState != 0)
                        {
                            var existing = FindPlayingSlot(play2D, entity, slot.InternalPlayingState);

                            existing?.Stop();
                        }

                        slot.InternalPlayingState = 0;
                    }
                    else
                    {
                        // This slot wants to play a sound

                        bool startNew = slot.InternalPlayingState == 0;

                        if (!startNew)
                        {
                            // Adjusting existing sound
                            var existing = FindPlayingSlot(play2D, entity, slot.InternalPlayingState);

                            if (existing != null)
                            {
                                if (existing.Sound == slot.SoundFile)
                                {
                                    existing.AdjustProperties(slot.Loop, slot.Volume);
                                }
                                else
                                {
                                    // Sound file changed but it wasn't fully correctly cleared
                                    GD.PrintErr("Playing sound effect file changed incorrectly");
                                    existing.Stop();
                                    startNew = true;
                                }
                            }
                            else
                            {
                                // Existing player no longer valid
                                startNew = true;
                            }
                        }

                        if (startNew && !string.IsNullOrEmpty(slot.SoundFile))
                        {
                            // Only start playing if can
                            // 2D sounds ignore the limit to make sure player sounds can always play
                            if (playingSoundCount >= Constants.MAX_CONCURRENT_SOUNDS && !play2D)
                            {
                                // This leaves SoundsApplied false so that this entity can keep trying until there
                                // are empty sound playing slots
                                skippedSomething = true;
                                continue;
                            }

                            // New sound start requested
                            slot.InternalPlayingState = NextSoundIdentifier();

                            // TODO: do we need to guard against a situation where a long playing sound has an ID
                            // we have wrapped around to here? (by scanning the slot internal playing states on
                            // this entity to see if there are duplicates)

                            StartPlaying(play2D, entity, slot.InternalPlayingState, slot.SoundFile!, slot.Loop,
                                slot.Volume);
                        }
                    }

                    slot.InternalAppliedState = true;
                }

                // Only mark as applied if we had room to start all sounds that should be played
                if (skippedSomething)
                    continue;

                soundEffectPlayer.SoundsApplied = true;
            }
        }
    }

    private void StartPlaying(bool play2D, in Entity entity, ushort id, string sound, bool loop, float volume)
    {
        ++playingSoundCount;

        if (!play2D)
        {
            AudioStreamPlayer3D player;

            // Use a free player if available
            if (freePositionalPlayers.Count > 0)
            {
                player = freePositionalPlayers.Pop();
            }
            else
            {
                // Allocate a new one if we ran out
                player = CreateNewPositional();
            }

            player.Stream = GetAudioStream(sound);
            player.VolumeDb = Mathf.LinearToDb(volume);
            player.Play();

            usedPositionalPlayers.Add(new PlayingPositionalPlayer(player, entity, sound, id, loop));
        }
        else
        {
            AudioStreamPlayer player;

            if (free2DPlayers.Count > 0)
            {
                player = free2DPlayers.Pop();
            }
            else
            {
                // Allocate a new one if we ran out
                player = CreateNew2DPlayer();
            }

            player.Stream = GetAudioStream(sound);
            player.VolumeDb = Mathf.LinearToDb(volume);
            player.Play();

            used2DPlayers.Add(new NonPlayingPositionalPlayer(player, entity, sound, id, loop));
        }
    }

    private CurrentlyPlayingBase? FindPlayingSlot(bool is2D, in Entity entity, ushort id)
    {
        // TODO: do we need to switch to a map based (or maybe structs in an array?) storage to speed this up?
        if (is2D)
        {
            foreach (var used2DPlayer in used2DPlayers)
            {
                if (used2DPlayer.SoundId == id && used2DPlayer.Entity == entity)
                    return used2DPlayer;
            }
        }
        else
        {
            foreach (var usedPositional in usedPositionalPlayers)
            {
                if (usedPositional.SoundId == id && usedPositional.Entity == entity)
                    return usedPositional;
            }
        }

        return null;
    }

    /// <summary>
    ///   Returns next pseudo-unique identifier for a sound
    /// </summary>
    /// <returns>The next identifier to use</returns>
    private ushort NextSoundIdentifier()
    {
        ++soundIdentifierCounter;

        if (soundIdentifierCounter == 0)
            ++soundIdentifierCounter;

        return soundIdentifierCounter;
    }

    private AudioStreamPlayer3D CreateNewPositional()
    {
        var player = new AudioStreamPlayer3D
        {
            Bus = AudioBus,

            // TODO: should max distance be set here?
            // MaxDistance = Constants.MICROBE_SOUND_MAX_DISTANCE,
        };

        soundPlayerParent.AddChild(player);

        return player;
    }

    private AudioStreamPlayer CreateNew2DPlayer()
    {
        var player = new AudioStreamPlayer
        {
            Bus = AudioBus,
        };

        soundPlayerParent.AddChild(player);

        return player;
    }

    private AudioStream GetAudioStream(string sound)
    {
        if (soundCache.TryGetValue(sound, out var result))
        {
            result.LastUsed = timeCounter;
            return result.Stream;
        }

        var stream = GD.Load<AudioStream>(sound);

        soundCache[sound] = new CachedSound(stream, timeCounter);

        return stream;
    }

    private void ExpireOldAudioCacheEntries(float delta)
    {
        lastClearedSoundCacheTime += delta;

        if (lastClearedSoundCacheTime < Constants.INTERVAL_BETWEEN_SOUND_CACHE_CLEAR)
            return;

        lastClearedSoundCacheTime = 0;

        foreach (var entry in soundCache)
        {
            if (timeCounter - entry.Value.LastUsed > Constants.DEFAULT_SOUND_CACHE_TIME)
                soundCacheEntriesToClear.Add(entry.Key);
        }

        foreach (var toDelete in soundCacheEntriesToClear)
        {
            soundCache.Remove(toDelete);
        }

        soundCacheEntriesToClear.Clear();
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Free cached sounds immediately
            soundCache.Clear();
        }
    }

    /// <summary>
    ///   This and derived classes include extra info that is needed on a currently playing audio player for this
    ///   system
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: reuse instances of this class as well to reduce memory allocation (instead of just reusing the
    ///     underlying player objects)
    ///   </para>
    /// </remarks>
    private abstract class CurrentlyPlayingBase
    {
        public readonly Entity Entity;

        public readonly string Sound;

        public readonly ushort SoundId;

        public bool Loop;

        protected CurrentlyPlayingBase(in Entity entity, string sound, ushort id, bool loop)
        {
            Entity = entity;
            Sound = sound;
            SoundId = id;
            Loop = loop;
        }

        /// <summary>
        ///   True when there is an alive entity related to this player. This is used to stop looping sounds for
        ///   dead entities instead of playing forever.
        /// </summary>
        public bool HasAliveEntity => Entity.IsAlive;

        public void MarkEnded()
        {
            MarkSoundEndedOnEntityIfPossible(Entity, SoundId, Sound);
        }

        public abstract void Stop();

        public void AdjustProperties(bool slotLoop, float slotVolume)
        {
            Loop = slotLoop;
            SetVolume(slotVolume);
        }

        public abstract void SetVolume(float linearVolume);
    }

    /// <summary>
    ///   Stores the info on where to read the updated position from for a positional player
    /// </summary>
    private class PlayingPositionalPlayer : CurrentlyPlayingBase
    {
        public readonly AudioStreamPlayer3D Player;

        public PlayingPositionalPlayer(AudioStreamPlayer3D player, in Entity entity, string sound, ushort id,
            bool loop) : base(entity, sound, id, loop)
        {
            Player = player;
        }

        public bool GetUpdatedPositionIfEntityIsValid(out Vector3 position)
        {
            if (!Entity.IsAlive)
            {
                position = Vector3.Zero;
                return false;
            }

            position = Entity.Get<WorldPosition>().Position;
            return true;
        }

        public override void Stop()
        {
            Player.Stop();
            Loop = false;
        }

        public override void SetVolume(float linearVolume)
        {
            Player.VolumeDb = Mathf.LinearToDb(linearVolume);
        }
    }

    private class NonPlayingPositionalPlayer : CurrentlyPlayingBase
    {
        public readonly AudioStreamPlayer Player;

        public NonPlayingPositionalPlayer(AudioStreamPlayer player, in Entity entity, string sound, ushort id,
            bool loop) : base(entity, sound, id, loop)
        {
            Player = player;
        }

        public override void Stop()
        {
            Player.Stop();
            Loop = false;
        }

        public override void SetVolume(float linearVolume)
        {
            Player.VolumeDb = Mathf.LinearToDb(linearVolume);
        }
    }

    private class CachedSound
    {
        public readonly AudioStream Stream;
        public float LastUsed;

        public CachedSound(AudioStream stream, float currentTime)
        {
            Stream = stream;
            LastUsed = currentTime;
        }
    }
}
