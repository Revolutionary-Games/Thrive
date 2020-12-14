﻿using System.Collections.Generic;
using Godot;

/// <summary>
///   Spawner that can be added to a SpawnSystem to be used for spawning things
/// </summary>
public abstract class Spawner
{
    /// <summary>
    ///   The distance at which spawning happens
    /// </summary>
    public int SpawnRadius { get; set; }

    /// <summary>
    ///   Squared spawn distance for faster computations when spawning
    /// </summary>
    /// <value>The spawn radius sqr.</value>
    public int SpawnRadiusSqr { get; set; }

    /// <summary>
    ///   Squared minimum spawn distance allowed
    /// </summary>
    /// <value>The minimum allowed spawn radius sqr.</value>
    public float MinSpawnRadiusSqr { get; set; }

    /// <summary>
    ///   How much stuff spawns
    /// </summary>
    /// <value>The spawn frequency.</value>
    public int SpawnFrequency { get; set; }

    /// <summary>
    ///   If this is queued to be destroyed the spawn system will remove this on next update
    /// </summary>
    /// <value><c>true</c> if destroy queued; otherwise, <c>false</c>.</value>
    public bool DestroyQueued { get; set; }

    /// <summary>
    ///   Spawns the next thing. This is an enumerator to be able to control how many things to spawn per frame easily
    /// </summary>
    /// <param name="worldNode">The parent node of spawned entities</param>
    /// <param name="location">Location the spawn system wants to spawn a thing at</param>
    /// <returns>An enumerator that on each next call spawns one thing</returns>
    public abstract IEnumerable<ISpawned> Spawn(Node worldNode, Vector3 location);

    public void SetFrequencyFromDensity(float spawnDensity)
    {
        SpawnFrequency = (int)(spawnDensity * SpawnRadiusSqr * 4);
    }
}
