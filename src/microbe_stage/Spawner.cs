using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Spawner that can be added to a SpawnSystem to be used for spawning things
/// </summary>
public abstract class Spawner
{
    private float[] binomialValuesCache;

    public virtual int BinomialN => 0;
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
    ///   The calculated binomial values. Should add up to roughly 1f. Lazily calculated.
    /// </summary>
    private float[] BinomialValues
    {
        get
        {
            if (binomialValuesCache != null)
                return binomialValuesCache;

            var nFactorial = MathUtils.Factorial(BinomialN);

            // nCr(BinomialN, r) * BinomialP^r * (1 - BinomialP)^(BinomialN - r)
            binomialValuesCache = Enumerable.Range(0, BinomialN).Select(r =>
                MathUtils.NCr(BinomialN, nFactorial, r) * Mathf.Pow(BinomialP, r) *
                Mathf.Pow(1 - BinomialP, BinomialN - r)).ToArray();
            return binomialValuesCache;
        }
    }

    /// <summary>
    ///   Evenly distributes the spawns in a sector.
    /// </summary>
    /// <returns>Returns the relative points where stuff should spawn</returns>
    public virtual IEnumerable<Vector2> GetSpawnPoints(float sectorDensity,
        Random random)
    {
        var spawns = GetSpawnsInASector(sectorDensity, random);
        var results = new List<Vector2>(spawns);
        for (var i = 0; i < spawns; i++)
        {
            var vector = new Vector2(random.NextFloat(), random.NextFloat());
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
    public abstract IEnumerable<SpawnedRigidBody> Instantiate(Vector2 location);

    /// <summary>
    ///   Used in <see cref="GetSpawnPoints"/> to determine how many spawns should occur in this sector and therefore
    ///   the length of the list returned vector list.
    /// </summary>
    /// <returns>Returns the amount of spawns in a sector.</returns>
    protected virtual int GetSpawnsInASector(float sectorDensity, Random random)
    {
        if (BinomialN == 0)
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
