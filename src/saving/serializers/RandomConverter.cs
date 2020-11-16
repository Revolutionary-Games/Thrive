using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

public class RandomConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var formatter = new BinaryFormatter();
        using (var dataWriter = new MemoryStream())
        {
            formatter.Serialize(dataWriter, (Random)value);
            var state = new RandomState(dataWriter.ToArray());
            serializer.Serialize(writer, state);
        }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var formatter = new BinaryFormatter();
        var state = serializer.Deserialize<RandomState>(reader);

        using (var dataReader = new MemoryStream(state.State))
        {
            return formatter.Deserialize(dataReader);
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Random);
    }
}
