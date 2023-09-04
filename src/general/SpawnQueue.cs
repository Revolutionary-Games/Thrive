using System;
using System.Collections.Generic;
using DefaultEcs.Command;
using Godot;

/// <summary>
///   Spawn queue used by <see cref="Spawner"/> to facilitate spawning entities in limited amount per frame but still
///   allowing spawners to create big spawn groups that spawn over multiple frames
/// </summary>
public abstract class SpawnQueue : IDisposable
{
    protected SpawnQueue(Spawner relatedSpawnType)
    {
        RelatedSpawnType = relatedSpawnType;
    }

    /// <summary>
    ///   The spawn type used in this queue. This needs to be known by the spawn system when spawning things over
    ///   multiple frames.
    /// </summary>
    public Spawner RelatedSpawnType { get; }

    /// <summary>
    ///   True once this queue has ended and should be destroyed
    /// </summary>
    public abstract bool Ended { get; protected set; }

    /// <summary>
    ///   Returns true if a potential spawn location is too close to the player
    /// </summary>
    /// <param name="spawnLocation">The location to check</param>
    /// <param name="playerPosition">Where the player is (approximately) known to be</param>
    /// <returns>True if spawn should be skipped</returns>
    public static bool IsTooCloseToPlayer(Vector3 spawnLocation, Vector3 playerPosition)
    {
        if ((playerPosition - spawnLocation).Length() < Constants.MIN_DISTANCE_FROM_PLAYER_FOR_SPAWN)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///   Prunes too close spawn locations from list
    /// </summary>
    /// <param name="positions">The location list to prune</param>
    /// <param name="playerPosition">Approximate player position</param>
    /// <returns>True if there are no more valid positions and the list is now empty</returns>
    public static bool PruneSpawnListPositions(IList<Vector3> positions, Vector3 playerPosition)
    {
        while (true)
        {
            if (positions.Count < 1)
                return true;

            if (IsTooCloseToPlayer(positions[0], playerPosition))
            {
                positions.RemoveAt(0);
            }
            else
            {
                // Found a valid position
                return false;
            }
        }
    }

    public abstract (EntityCommandRecorder CommandRecorder, float SpawnedWeight) SpawnNext(out EntityRecord entity);

    /// <summary>
    ///   Checks that this spawn queue is still good to spawn from
    /// </summary>
    /// <param name="playerPosition">
    ///   The player position to check against. If spawning is too close to the player it should be skipped.
    /// </param>
    /// <remarks>
    ///   <para>
    ///     Derived classes should set <see cref="Ended"/> to true if they override this and determine a spawn
    ///     shouldn't happen
    ///   </para>
    /// </remarks>
    public virtual void CheckIsSpawningStillPossible(Vector3 playerPosition)
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        _ = disposing;

        // Ensure this is ended so that no one calls SpawnNext again
        Ended = true;
    }
}

/// <summary>
///   A spawn queue that can just spawn a single item (from a callback)
/// </summary>
public class SingleItemSpawnQueue : SpawnQueue
{
    private readonly Factory factory;

    public SingleItemSpawnQueue(Factory factory, Spawner fromSpawnType) : base(fromSpawnType)
    {
        this.factory = factory;
    }

    public delegate (EntityCommandRecorder CommandRecorder, float SpawnedWeight) Factory(out EntityRecord entity);

    public override bool Ended { get; protected set; }

    public override (EntityCommandRecorder CommandRecorder, float SpawnedWeight) SpawnNext(out EntityRecord entity)
    {
        Ended = true;
        return factory(out entity);
    }
}

/// <summary>
///   A callback based spawn queue with multiple items
/// </summary>
/// <typeparam name="T">The state to pass to the spawn function</typeparam>
public class CallbackSpawnQueue<T> : SpawnQueue
{
    private readonly Factory factory;
    private readonly CheckTooCloseToPlayer cancelCheck;
    private T stateData;

    public CallbackSpawnQueue(Factory factory, T initialData, CheckTooCloseToPlayer cancelCheck, Spawner fromSpawnType)
        : base(fromSpawnType)
    {
        this.factory = factory;
        stateData = initialData;
        this.cancelCheck = cancelCheck;
    }

    public delegate (EntityCommandRecorder Recorder, float SpawnedWeight, bool LastItem) Factory(T state,
        out EntityRecord entity);

    public delegate bool CheckTooCloseToPlayer(T state, Vector3 playerPosition);

    public override bool Ended { get; protected set; }

    public override (EntityCommandRecorder CommandRecorder, float SpawnedWeight) SpawnNext(out EntityRecord entity)
    {
        var (recorder, weight, ended) = factory(stateData, out entity);

        if (ended)
            Ended = true;

        return (recorder, weight);
    }

    public override void CheckIsSpawningStillPossible(Vector3 playerPosition)
    {
        if (cancelCheck(stateData, playerPosition))
        {
            Ended = true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            if (stateData is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}

/// <summary>
///   Spawn queue that consists of other spawn queues
/// </summary>
public class CombinedSpawnQueue : SpawnQueue
{
    private readonly SpawnQueue[] spawnQueues;
    private int usedSpawnIndex;

    public CombinedSpawnQueue(params SpawnQueue[] spawns) : base(spawns[0].RelatedSpawnType)
    {
        spawnQueues = spawns;
    }

    public override bool Ended
    {
        get => usedSpawnIndex >= spawnQueues.Length;
        protected set
        {
            if (value == false)
                throw new NotSupportedException("Can't reset spawn index");

            usedSpawnIndex = int.MaxValue;
        }
    }

    public override (EntityCommandRecorder CommandRecorder, float SpawnedWeight) SpawnNext(out EntityRecord entity)
    {
        if (Ended)
            throw new InvalidOperationException("Spawn queue has ended");

        var result = spawnQueues[usedSpawnIndex].SpawnNext(out entity);

        // When one queue ends, we move onto the next one
        if (spawnQueues[usedSpawnIndex].Ended)
            ++usedSpawnIndex;

        return result;
    }

    public override void CheckIsSpawningStillPossible(Vector3 playerPosition)
    {
        if (Ended)
            return;

        spawnQueues[usedSpawnIndex].CheckIsSpawningStillPossible(playerPosition);

        // Automatically end the queue if CheckIsSpawningStillPossible set it to ended
        if (spawnQueues[usedSpawnIndex].Ended)
            ++usedSpawnIndex;
    }
}
