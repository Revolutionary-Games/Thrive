using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SharedBase.Archive;

/// <summary>
///   A species shape specified by metaballs
/// </summary>
public class MetaballLayout<T> : ICollection<T>, IReadOnlyMetaballLayout<T>, IArchivable
    where T : Metaball
{
    // TODO: make a serializer for this like for hex layout serializer
    public const ushort SERIALIZATION_VERSION = 1;

    [JsonProperty]
    protected Action<T>? onAdded;

    [JsonProperty]
    protected Action<T>? onRemoved;

    [JsonProperty]
    protected List<T> metaballs = new();

    public MetaballLayout(Action<T>? onAdded = null, Action<T>? onRemoved = null)
    {
        this.onAdded = onAdded;
        this.onRemoved = onRemoved;
    }

    [JsonIgnore]
    public int Count => metaballs.Count;

    [JsonIgnore]
    public bool IsReadOnly => false;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.ExtendedMetaballLayout;
    public bool CanBeReferencedInArchive => false;

    public T this[int index] => metaballs[index];

    public void WriteToArchive(ISArchiveWriter writer)
    {
        throw new NotImplementedException();
    }

    [MustDisposeResource]
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
        if (metaball.ModifiableParent == metaball)
            throw new ArgumentException("Metaball can't be its own parent");

        // First metaball (or adding the root back) can be placed anywhere
        if (metaball.ModifiableParent == null && (Count < 1 || metaballs.All(m => m.ModifiableParent != null)))
            return true;

        if (metaball.ModifiableParent == null)
            return false;

        // Fail if parent missing
        var parent = metaball.ModifiableParent;
        bool found = true;
        foreach (var existing in metaballs)
        {
            if (existing == parent)
            {
                found = true;
                break;
            }
        }

        if (!found)
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
            if (metaball.ModifiableParent == null)
                return;

            var distance = (metaball.ModifiableParent.Position - metaball.Position).Length();

            if (distance - metaball.Radius - metaball.ModifiableParent.Radius > MathUtils.EPSILON)
                throw new Exception("Metaball is not touching its parent");
        }
    }

    public IEnumerable<T> GetChildrenOf(T metaball)
    {
        foreach (var layoutMetaball in this)
        {
            if (layoutMetaball.ModifiableParent == metaball)
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
        if (descendant.ModifiableParent == null)
            return false;

        if (descendant.ModifiableParent == parent)
            return true;

        return IsDescendantsOf(descendant.ModifiableParent, parent);
    }

    [MustDisposeResource]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerable<T> GetMetaballsNotTouchingParents(float contactThreshold = 0.1f, float toleranceMultiplier = 2)
    {
        foreach (var metaball in metaballs)
        {
            // Root is a special case as it doesn't have a parent, it is allowed to be anywhere
            if (metaball.ModifiableParent == null)
                continue;

            var maxDistance = toleranceMultiplier * (metaball.Radius + metaball.ModifiableParent.Radius);
            var distance = metaball.Position.DistanceTo(metaball.ModifiableParent.Position);

            if (distance > maxDistance + contactThreshold)
            {
                yield return metaball;
            }
        }
    }

    public T? GetClosestMetaballToPosition(Vector3 position)
    {
        var closestDistance = float.MaxValue;
        T? closestMetaball = null;

        foreach (var metaball in metaballs)
        {
            var distance = metaball.Position.DistanceSquaredTo(position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestMetaball = metaball;
            }
        }

        return closestMetaball;
    }
}
