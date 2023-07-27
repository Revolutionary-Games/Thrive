using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

/// <summary>
///   Network serializable variables.
/// </summary>
[JSONDynamicTypeAllowed]
public class Vars : INetworkSerializable
{
    [YamlMember]
    [JsonProperty]
    protected Dictionary<string, object> entries = new();

    /// <summary>
    ///   Sets the value for the specified key. If null is given, removes the value for that key.
    /// </summary>
    public virtual void SetVar<T>(string key, T? variant)
    {
        if (variant == null)
        {
            entries.Remove(key);
        }
        else
        {
            entries[key] = variant;
        }
    }

    /// <summary>
    ///   Gets the value associated with the given <paramref name="key"/>.
    /// </summary>
    /// <returns>Casted value of type <typeparamref name="T"/> or throws if not found/incorrect cast.</returns>
    public T GetVar<T>(string key)
    {
        var entry = entries[key];

        if (entry is T t)
            return t;

        return (T)Convert.ChangeType(entry, typeof(T));
    }

    /// <summary>
    ///   Checks whether a variable with the given <paramref name="key"/> exists.
    /// </summary>
    /// <returns>True if the value for <paramref name="key"/> is set.</returns>
    public bool HasVar(string key)
    {
        return entries.ContainsKey(key);
    }

    /// <summary>
    ///   Gets the value associated with the given <paramref name="key"/>.
    /// </summary>
    /// <returns>
    ///   True if <paramref name="value"/> exists and is of type <typeparamref name="T"/>, otherwise false.
    /// </returns>
    public bool TryGetVar<T>(string key, out T value)
    {
        try
        {
            value = GetVar<T>(key);
        }
        catch
        {
            value = default!;
            return false;
        }

        return true;
    }

    /// <summary>
    ///   Clears all the stored variables.
    /// </summary>
    public void Clear()
    {
        entries.Clear();
    }

    public virtual void NetworkSerialize(BytesBuffer buffer)
    {
        buffer.Write((byte)entries.Count);
        foreach (var entry in entries)
        {
            buffer.Write(entry.Key);
            buffer.WriteVariant(entry.Value);
        }
    }

    public virtual void NetworkDeserialize(BytesBuffer buffer)
    {
        var nEntries = buffer.ReadByte();
        for (int i = 0; i < nEntries; ++i)
        {
            var key = buffer.ReadString();
            var value = buffer.ReadVariant();
            entries[key] = value;
        }
    }

    public override string ToString()
    {
        // TODO: Use YAML
        return JsonConvert.SerializeObject(entries, Formatting.Indented);
    }
}
