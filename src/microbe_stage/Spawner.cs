using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Spawner that can be added to a SpawnSystem to be used for spawning things
/// </summary>
public abstract class Spawner
{
    private float[] binomialValues;

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

    private float[] BinomialValues
    {
        get
        {
            if (binomialValues != null)
                return binomialValues;

            var nFactorial = MathUtils.Factorial(BinomialN);

            // nCr(BinomialN, r) * BinomialP^r * (1 - BinomialP)^(BinomialN - r)
            binomialValues = Enumerable.Range(0, BinomialN).Select(r =>
                MathUtils.NCr(BinomialN, nFactorial, r) * Mathf.Pow(BinomialP, r) *
                Mathf.Pow(1 - BinomialP, BinomialN - r)).ToArray();
            return binomialValues;
        }
    }

    public virtual int GetSpawnsInASector(float sectorDensity, Random random)
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
    ///   Evenly distributes the spawns in a sector.
    /// </summary>
    /// <returns>Returns the relative points where stuff should spawn</returns>
    public virtual List<Vector2> GetSpawnPoints(float sectorDensity, Random random)
    {
        var spawns = GetSpawnsInASector(sectorDensity, random);
        var results = new List<Vector2>();
        for (var i = 0; i < spawns; i++)
        {
            var x = random.NextFloat() * Constants.SECTOR_SIZE;
            var y = random.NextFloat() * Constants.SECTOR_SIZE;
            var vector = new Vector2(x, y);
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
    ///   Spawns the next thing. This is an enumerator to be able to control how many things to spawn per frame easily
    /// </summary>
    /// <param name="worldNode">The parent node of spawned entities</param>
    /// <param name="location">Location the spawn system wants to spawn a thing at</param>
    /// <returns>An enumerator that on each next call spawns one thing</returns>
    public abstract IEnumerable<SpawnedRigidBody> Spawn(Node worldNode, Vector2 location);
}
