using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   A species shape specified by metaballs
/// </summary>
public class MetaballLayout<T> : ICollection<T>
    where T : Metaball
{
    private readonly List<T> metaballs = new();

    [JsonIgnore]
    public int Count => metaballs.Count;

    [JsonIgnore]
    public bool IsReadOnly => false;

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
    }

    public bool CanAdd(T metaball)
    {
        throw new NotImplementedException();
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
        return metaballs.Remove(metaball);
    }
}
