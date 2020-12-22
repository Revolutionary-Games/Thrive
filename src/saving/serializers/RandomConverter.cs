using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class RandomConverter : JsonConverter
{
    private readonly FieldInfo seedArrayInfo = typeof(Random).GetField("_seedArray", BindingFlags.NonPublic |
        BindingFlags.Instance);

    private readonly FieldInfo inextInfo = typeof(Random).GetField("_inext", BindingFlags.NonPublic |
        BindingFlags.Instance);

    private readonly FieldInfo inextpInfo = typeof(Random).GetField("_inextp", BindingFlags.NonPublic |
        BindingFlags.Instance);

    public RandomConverter()
    {
        if (seedArrayInfo == null || inextInfo == null || inextpInfo == null)
            throw new NullReferenceException("RandomConverter could not find a specified field in Random");
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var converted = (Random)value;

        var seedArray = (int[])seedArrayInfo.GetValue(converted);
        var inext = (int)inextInfo.GetValue(converted);
        var inextp = (int)inextpInfo.GetValue(converted);

        writer.WriteStartObject();

        writer.WritePropertyName("seedArray");
        serializer.Serialize(writer, seedArray);

        writer.WritePropertyName("inext");
        serializer.Serialize(writer, inext);

        writer.WritePropertyName("inextp");
        serializer.Serialize(writer, inextp);

        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
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
            // ReSharper disable AssignNullToNotNullAttribute PossibleNullReferenceException
            var inext = item["inext"].Value<int>();
            var inextp = item["inextp"].Value<int>();
            var seedArray = item["seedArray"].ToObject<int[]>();

            // ReSharper restore AssignNullToNotNullAttribute PossibleNullReferenceException

            seedArrayInfo.SetValue(random, seedArray);
            inextInfo.SetValue(random, inext);
            inextpInfo.SetValue(random, inextp);

            return random;
        }
        catch (ArgumentException e)
        {
            throw new JsonException("Can't read Random (missing property)", e);
        }
        catch (NullReferenceException e)
        {
            throw new JsonException("Can't read Random (missing property)", e);
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Random);
    }
}
