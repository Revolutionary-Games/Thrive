using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Allows two collections to act as one (with duplicate items ignored when reading)
/// </summary>
/// <typeparam name="T">The type this collection holds</typeparam>
/// <remarks>
///   <para>
///     The count operation is expensive and should be avoided
///   </para>
/// </remarks>
public class MultiCollection<T> : ICollection<T>
{
    [JsonProperty]
    private readonly ICollection<T> primary;

    [JsonProperty]
    private readonly ICollection<T> secondary;

    [JsonConstructor]
    public MultiCollection(ICollection<T> primary, ICollection<T> secondary)
    {
        this.primary = primary;
        this.secondary = secondary;
    }

    /// <summary>
    ///   Count of unique items in the two collections. Note that this is an expensive operation
    /// </summary>
    [JsonIgnore]
    public int Count => primary.Concat(secondary).Distinct().Count();

    [JsonIgnore]
    public int RoughCount => primary.Count + secondary.Count;

    [JsonIgnore]
    public bool IsReadOnly => primary.IsReadOnly;

    public IEnumerator<T> GetEnumerator()
    {
        return primary.Concat(secondary).Distinct().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(T item)
    {
        primary.Add(item);
    }

    public void Clear()
    {
        primary.Clear();
    }

    public bool Contains(T item)
    {
        return primary.Contains(item) || secondary.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        foreach (var item in this)
        {
            array[arrayIndex++] = item;
        }
    }

    public bool Remove(T item)
    {
        if (primary.Remove(item))
            return true;

        // TODO: should we remove from secondary here?
        return false;
    }
}
