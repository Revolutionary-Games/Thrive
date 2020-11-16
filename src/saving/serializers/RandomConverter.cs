using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

public class RandomConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Type type = typeof(Random);
        FieldInfo seedArrayInfo = type.GetField("_seedArray", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo inextInfo = type.GetField("_inext", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo inextpInfo = type.GetField("_inextp", BindingFlags.NonPublic | BindingFlags.Instance);

        if (seedArrayInfo != null && inextInfo != null && inextpInfo != null)
        {
            int[] seedArray = (int[])seedArrayInfo.GetValue((Random)value);
            int inext = (int)inextInfo.GetValue((Random)value);
            int inextp = (int)inextpInfo.GetValue((Random)value);
            int currSeed = seedArray[inext] - seedArray[inextp];

            var formatter = new BinaryFormatter();
            using (var dataWriter = new MemoryStream())
            {
                formatter.Serialize(dataWriter, currSeed);
                serializer.Serialize(writer, Convert.ToBase64String(dataWriter.GetBuffer()));
            }
        }
        else
        {
            throw new NullReferenceException("One or more fields did not exist in the type or did not match the " +
                "specified binding constraints");
        }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var formatter = new BinaryFormatter();
        var encoded = serializer.Deserialize<string>(reader);

        if (string.IsNullOrEmpty(encoded))
            return null;

        using (var dataReader = new MemoryStream(Convert.FromBase64String(encoded)))
        {
            int seed = (int)formatter.Deserialize(dataReader);
            return new Random(seed);
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Random);
    }
}
