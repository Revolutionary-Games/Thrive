using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Spawner that can be added to a SpawnSystem to be used for spawning things
/// </summary>
public abstract class Spawner
{
    private float[]? binomialValuesCache;

    /// <summary>
    ///   The number of trials
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     You can try out values <a href="https://homepage.divms.uiowa.edu/~mbognar/applets/bin.html">here</a>
    ///   </para>
    /// </remarks>
    public virtual int BinomialN => 0;
    /// <summary>
    ///   The success probability for each trial
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     You can try out values <a href="https://homepage.divms.uiowa.edu/~mbognar/applets/bin.html">here</a>
    ///   </para>
    /// </remarks>
    public virtual float BinomialP => 0;

    /// <summary>
    ///   The minimum distance between spawns.
    /// </summary>
    public virtual float MinDistanceSquared => 0;

    /// <summary>
    ///   If this is queued to be destroyed the spawn system will remove this on next update
    /// </summary>
    /// <value><c>true</c> if destroy queued; otherwise, <c>false</c>.</value>
    public bool DestroyQueued { get; set; }

    /// <summary>
    ///   The calculated binomial values. Lazily calculated.
    /// </summary>
    private float[] BinomialValues => binomialValuesCache ??= MathUtils.BinomialValues(BinomialN, BinomialP);

    /// <summary>
    ///   Evenly distributes the spawns in a sector.
    /// </summary>
    /// <returns>Returns the relative points where stuff should spawn</returns>
    public virtual IEnumerable<Vector3> GetSpawnPoints(float sectorDensity,
        Random random)
    {
        var spawns = GetSpawnsInASector(sectorDensity, random);
        var results = new List<Vector3>(spawns);
        for (var i = 0; i < spawns; i++)
        {
            var vector = new Vector3(random.NextFloat(), 0, random.NextFloat());
            vector *= Constants.SECTOR_SIZE;

            // Check if another spawn is too close
            if (results.Any(v => (v - vector).LengthSquared() < MinDistanceSquared))
            {
                i--;
                continue;
            }

            results.Add(vector);
        }

        return results;
    }

    /// <summary>
    ///   Instantiate the next thing.
    ///   This is an enumerator to be able to control how many things to spawn per frame easily.
    ///   Do not add those instances to the world, that's the spawn system's job.
    /// </summary>
    /// <param name="location">Location the spawn system wants to spawn a thing at</param>
    /// <returns>An enumerator which defines the instances of the object to spawn</returns>
    public abstract IEnumerable<SpawnedRigidBody>? Instantiate(Vector3 location);

    /// <summary>
    ///   Used in <see cref="GetSpawnPoints"/> to determine how many spawns should occur in this sector and therefore
    ///   the length of the list returned vector list.
    /// </summary>
    /// <returns>Returns the amount of spawns in a sector.</returns>
    protected virtual int GetSpawnsInASector(float sectorDensity, Random random)
    {
        if (BinomialP < MathUtils.EPSILON)
            return 0;

        var nextRandom = random.NextFloat() * sectorDensity;
        var binomialSum = 0f;
        var i = 0;
        do
        {
            binomialSum += BinomialValues[i++];
        }
        while (nextRandom >= binomialSum);

        return i - 1;
    }

    /// <summary>
    ///   Clears the cache for the binomial values.
    ///   Call this when <see cref="BinomialN"/> or <see cref="BinomialP"/> change.
    /// </summary>
    protected void ClearBinomialValues()
    {
        binomialValuesCache = null;
    }
}
