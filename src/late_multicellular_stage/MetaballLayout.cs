using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   A species shape specified by metaballs
/// </summary>
public class MetaballLayout<T> : ICollection<T>, IReadOnlyCollection<T>
    where T : Metaball
{
    [JsonProperty]
    protected Action<T>? onAdded;

    [JsonProperty]
    protected Action<T>? onRemoved;

    [JsonProperty]
    private List<T> metaballs = new();

    public MetaballLayout(Action<T>? onAdded = null, Action<T>? onRemoved = null)
    {
        this.onAdded = onAdded;
        this.onRemoved = onRemoved;
    }

    [JsonIgnore]
    public int Count => metaballs.Count;

    [JsonIgnore]
    public bool IsReadOnly => false;

    public T this[int index] => metaballs[index];

    public IEnumerator<T> GetEnumerator()
    {
        return metaballs.GetEnumerator();
    }

    public void Add(T metaball)
    {
        if (!CanAdd(metaball))
            throw new ArgumentException("Can't place metaball at specified position");

        metaballs.Add(metaball);
        onAdded?.Invoke(metaball);
    }

    public bool CanAdd(T metaball)
    {
        if (metaball.Parent == metaball)
            throw new ArgumentException("Metaball can't be its own parent");

        // First metaball (or adding the root back) can be placed anywhere
        if (metaball.Parent == null && (Count < 1 || metaballs.All(m => m.Parent != null)))
            return true;

        if (metaball.Parent == null)
            return false;

        // Fail if parent missing
        var parent = metaball.Parent;
        if (metaballs.All(m => m != parent))
            return false;

        // TODO: distance check to parent? (need to fix MetaballTest if this is changed)
        // Metaballs need to be touching (close enough) to their parent metaball
        return true;
    }

    public void Clear()
    {
        metaballs.Clear();
    }

    public bool Contains(T metaball)
    {
        return metaballs.Contains(metaball);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        foreach (var metaball in this)
        {
            array[arrayIndex++] = metaball;
        }
    }

    public bool Remove(T metaball)
    {
        if (metaballs.Remove(metaball))
        {
            onRemoved?.Invoke(metaball);
            return true;
        }

        return false;
    }

    /// <summary>
    ///   Detects if a new metaball would overlap and also returns the closest metaball to the position
    /// </summary>
    /// <param name="metaball">The metaball to check overlap for</param>
    /// <param name="assumeOverlapIsClosest">
    ///   If true the search for closest metaball ends when an overlap is detected
    /// </param>
    /// <returns>Tuple of overlap status and the closest metaball</returns>
    public (bool Overlap, T ClosestMetaball) CheckOverlapAndFindClosest(T metaball, bool assumeOverlapIsClosest = true)
    {
        float closestDistance = float.MaxValue;
        T? closestMetaball = null;
        bool overlap = false;

        foreach (var existingMetaball in metaballs)
        {
            var distance = (existingMetaball.Position - metaball.Position).Length();

            if (distance < closestDistance)
                closestMetaball = existingMetaball;

            if (distance - existingMetaball.Radius - metaball.Radius < MathUtils.EPSILON)
            {
                // Overlapping metaball
                overlap = true;

                if (assumeOverlapIsClosest)
                    return (true, existingMetaball);
            }
        }

        if (closestMetaball == null)
            throw new InvalidOperationException("No metaballs exist, can't find closest");

        return (overlap, closestMetaball);
    }

    /// <summary>
    ///   Sanity check that metaballs are touching and not detached, throws if invalid
    /// </summary>
    public void VerifyMetaballsAreTouching()
    {
        foreach (var metaball in metaballs)
        {
            if (metaball.Parent == null)
                return;

            var distance = (metaball.Parent.Position - metaball.Position).Length();

            if (distance - metaball.Radius - metaball.Parent.Radius > MathUtils.EPSILON)
                throw new Exception("Metaball is not touching its parent");
        }
    }

    public IEnumerable<T> GetChildrenOf(T metaball)
    {
        foreach (var layoutMetaball in this)
        {
            if (layoutMetaball.Parent == metaball)
                yield return layoutMetaball;
        }
    }

    public void DescendantsOfAndSelf(ICollection<T> list, T metaball)
    {
        if (list.Contains(metaball))
            return;

        list.Add(metaball);

        foreach (var child in GetChildrenOf(metaball))
        {
            DescendantsOfAndSelf(list, child);
        }
    }

    public bool IsDescendantsOf(Metaball descendant, Metaball parent)
    {
        if (descendant.Parent == null)
            return false;

        if (descendant.Parent == parent)
            return true;

        return IsDescendantsOf(descendant.Parent, parent);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerable<T> GetMetaballsNotTouchingParents(float contactThreshold = 0.1f, float toleranceMultiplier = 2)
    {
        foreach (var metaball in this)
        {
            if (metaball.Parent == null)
                continue;

            var maxDistance = toleranceMultiplier * (metaball.Radius + metaball.Parent.Radius);
            var distance = metaball.Position.DistanceTo(metaball.Parent.Position);

            if (distance > maxDistance + contactThreshold)
            {
                yield return metaball;
            }
        }
    }
}
