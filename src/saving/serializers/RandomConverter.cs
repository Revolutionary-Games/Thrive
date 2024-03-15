using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class RandomConverter : JsonConverter
{
    // ReSharper disable StringLiteralTypo
    private readonly FieldInfo seedArrayInfo = typeof(Random).GetField("_seedArray", BindingFlags.NonPublic |
        BindingFlags.Instance) ?? throw new Exception("Random is missing expected field _seedArray");

    private readonly FieldInfo iNextInfo = typeof(Random).GetField("_inext", BindingFlags.NonPublic |
        BindingFlags.Instance) ?? throw new Exception("Random is missing expected field _inext");

    private readonly FieldInfo iNextPInfo = typeof(Random).GetField("_inextp", BindingFlags.NonPublic |
        BindingFlags.Instance) ?? throw new Exception("Random is missing expected field _inextp");

    // ReSharper restore StringLiteralTypo

    public RandomConverter()
    {
        if (seedArrayInfo == null || iNextInfo == null || iNextPInfo == null)
            throw new NullReferenceException("RandomConverter could not find a specified field in Random");
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var converted = (Random)value;

        var seedArray = (int[]?)seedArrayInfo.GetValue(converted) ??
            throw new JsonException("Missing random seed array");
        var iNext = (int?)iNextInfo.GetValue(converted) ?? throw new JsonException("Missing random next");
        var iNextP = (int?)iNextPInfo.GetValue(converted) ?? throw new JsonException("Missing random next p");

        writer.WriteStartObject();

        writer.WritePropertyName("seedArray");
        serializer.Serialize(writer, seedArray);

        // ReSharper disable StringLiteralTypo
        writer.WritePropertyName("inext");
        serializer.Serialize(writer, iNext);

        writer.WritePropertyName("inextp");
        serializer.Serialize(writer, iNextP);

        // ReSharper restore StringLiteralTypo

        writer.WriteEndObject();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
            return null;

        var item = JObject.Load(reader);

        var random = new Random();

        // Backwards compatibility with no Random saved state
        if (!item.HasValues)
            return random;

        try
        {
            // ReSharper disable StringLiteralTypo
            var iNext = item["inext"]!.Value<int>();
            var iNextP = item["inextp"]!.Value<int>();
            var seedArray = item["seedArray"]!.ToObject<int[]>();

            // ReSharper restore StringLiteralTypo

            seedArrayInfo.SetValue(random, seedArray);
            iNextInfo.SetValue(random, iNext);
            iNextPInfo.SetValue(random, iNextP);

            return random;
        }
        catch (Exception e) when (
            e is ArgumentException or NullReferenceException)
        {
            throw new JsonException("Can't read Random (missing property)", e);
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Random);
    }
}
