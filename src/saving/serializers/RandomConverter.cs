using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

public class RandomConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var settings = new JsonSerializerSettings();
        string json = JsonConvert.SerializeObject((Random)value, settings);

        var formatter = new BinaryFormatter();
        using (var dataWriter = new MemoryStream())
        {
            formatter.Serialize(dataWriter, json);
            serializer.Serialize(writer, Convert.ToBase64String(dataWriter.GetBuffer()));
        }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var settings = new JsonSerializerSettings();
        var formatter = new BinaryFormatter();
        var encoded = serializer.Deserialize<string>(reader);

        if (string.IsNullOrEmpty(encoded))
            return null;

        using (var dataReader = new MemoryStream(Convert.FromBase64String(encoded)))
        {
            string json = (string)formatter.Deserialize(dataReader);
            return JsonConvert.DeserializeObject<Random>(json, settings);
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Random);
    }
}
