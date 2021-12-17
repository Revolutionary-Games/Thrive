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
    public virtual List<Vector2> GetSpawnPoints(List<Vector2> spawnsInNeighbourSectors, float sectorDensity,
        Random random)
    {
        var spawns = GetSpawnsInASector(sectorDensity, random);
        var results = new List<Vector2>();
        for (var i = 0; i < spawns; i++)
        {
            var vector = new Vector2(random.NextFloat(), random.NextFloat());
            vector *= Constants.SECTOR_SIZE;

            // Check if another spawn is too close
            if (results.Concat(spawnsInNeighbourSectors).Any(v => (v - vector).LengthSquared() < MinDistanceSquared))
            {
                i--;
                continue;
            }

            results.Add(vector);
        }

        return results;
    }

    /// <summary>
    ///   Spawns the next thing. This is an enumerator to be able to control how many things to spawn per frame easily
    /// </summary>
    /// <param name="worldNode">The parent node of spawned entities</param>
    /// <param name="location">Location the spawn system wants to spawn a thing at</param>
    /// <returns>An enumerator that on each next call spawns one thing</returns>
    public abstract IEnumerable<SpawnedRigidBody> Spawn(Node worldNode, Vector2 location);

    /// <summary>
    ///   Used in <see cref="GetSpawnPoints"/> to determine how many spawns should occur in this sector and therefore
    ///   the length of the list returned vector list.
    /// </summary>
    /// <returns>Returns the amount of spawns in a sector.</returns>
    protected virtual int GetSpawnsInASector(float sectorDensity, Random random)
    {
        var nextRandom = random.NextFloat() * sectorDensity;
        var binomialSum = 0f;
        var i = 0;
        do
        {
            binomialSum += BinomialValues[i++];
        }
        while (binomialSum >= nextRandom);

        return i;
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
