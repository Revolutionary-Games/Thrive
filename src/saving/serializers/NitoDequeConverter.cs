using System;
using Newtonsoft.Json;
using Nito.Collections;

/// <summary>
///   Converter for Nito package's double-ended queue collection <see cref="Nito.Collections.Deque{T}"/>
/// </summary>
public class NitoDequeConverter : JsonConverter
{
    public override bool CanRead => true;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var casted = (Deque<object>)value;

        var array = casted.ToArray();

        serializer.Serialize(writer, array);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var array = serializer.Deserialize<object[]>(reader);

        if (array == null)
            return null;

        var deque = new Deque<object>();

        foreach (var entry in array)
        {
            deque.Insert(Array.IndexOf(array, entry), entry);
        }

        return deque;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Deque<object>);
    }
}
