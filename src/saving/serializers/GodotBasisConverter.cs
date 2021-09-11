using System;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class GodotBasisConverter : JsonConverter
{
    public override bool CanRead => true;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var basis = (Basis)value;

        writer.WriteStartObject();

        writer.WritePropertyName("Column0");
        serializer.Serialize(writer, basis.Column0);

        writer.WritePropertyName("Column1");
        serializer.Serialize(writer, basis.Column1);

        writer.WritePropertyName("Column2");
        serializer.Serialize(writer, basis.Column2);

        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
        {
            return default(Basis);
        }

        var item = JObject.Load(reader);

        try
        {
            // ReSharper disable AssignNullToNotNullAttribute PossibleNullReferenceException
            return new Basis(item["Column0"].ToObject<Vector3>(),
                item["Column1"].ToObject<Vector3>(),
                item["Column2"].ToObject<Vector3>());

            // ReSharper restore AssignNullToNotNullAttribute PossibleNullReferenceException
        }
        catch (Exception e) when (
            e is NullReferenceException ||
            e is ArgumentNullException)
        {
            throw new JsonException("can't read Basis (missing property)", e);
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Basis);
    }
}
