using System;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class GodotQuaternionConverter : JsonConverter
{
    private static readonly Type QuatType = typeof(Quaternion);

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
            throw new JsonException("Quaternion can't be null as it is a value type");

        var quat = (Quaternion)value;

        writer.WriteStartObject();

        writer.WritePropertyName("x");
        writer.WriteValue(quat.X);

        writer.WritePropertyName("y");
        writer.WriteValue(quat.Y);

        writer.WritePropertyName("z");
        writer.WriteValue(quat.Z);

        writer.WritePropertyName("w");
        writer.WriteValue(quat.W);

        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
            return default(Quaternion);

        var item = JObject.Load(reader);

        try
        {
            // ReSharper disable AssignNullToNotNullAttribute
            return new Quaternion(item["x"]!.Value<float>(),
                item["y"]!.Value<float>(),
                item["z"]!.Value<float>(),
                item["w"]!.Value<float>());

            // ReSharper restore AssignNullToNotNullAttribute
        }
        catch (Exception e) when (
            e is NullReferenceException or ArgumentNullException)
        {
            throw new JsonException("can't read Quaternion (missing property)", e);
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == QuatType;
    }
}
