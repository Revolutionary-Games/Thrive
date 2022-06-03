using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   A species shape specified by metaballs
/// </summary>
public class MetaballLayout<T> : ICollection<T>
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

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
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
        // First metaball can be placed anywhere
        if (Count < 1 && metaball.Parent == null)
            return true;

        if (metaball.Parent == null)
            return false;

        // Fail if parent missing
        var parent = metaball.Parent;
        if (metaballs.All(m => m != parent))
            return false;

        // TODO: distance check to parent?
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
}
