using System;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class GodotQuatConverter : JsonConverter
{
    private static readonly Type QuatType = typeof(Quat);

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
            throw new JsonException("Quat can't be null as it is a value type");

        var quat = (Quat)value;

        writer.WriteStartObject();

        writer.WritePropertyName("x");
        writer.WriteValue(quat.x);

        writer.WritePropertyName("y");
        writer.WriteValue(quat.y);

        writer.WritePropertyName("z");
        writer.WriteValue(quat.z);

        writer.WritePropertyName("w");
        writer.WriteValue(quat.w);

        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
            return default(Quat);

        var item = JObject.Load(reader);

        try
        {
            // ReSharper disable AssignNullToNotNullAttribute
            return new Quat(item["x"]!.Value<float>(),
                item["y"]!.Value<float>(),
                item["z"]!.Value<float>(),
                item["w"]!.Value<float>());

            // ReSharper restore AssignNullToNotNullAttribute
        }
        catch (Exception e) when (
            e is NullReferenceException or ArgumentNullException)
        {
            throw new JsonException("can't read Quat (missing property)", e);
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == QuatType;
    }
}
