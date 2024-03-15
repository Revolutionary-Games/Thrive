using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Converter for <see cref="ConvexPolygonShape3D"/>
/// </summary>
public class ConvexPolygonShapeConverter : JsonConverter
{
    public override bool CanRead => true;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        serializer.Serialize(writer, ((ConvexPolygonShape3D)value).ResourcePath);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        var path = serializer.Deserialize<string>(reader);

        if (string.IsNullOrEmpty(path))
            return null;

        return GD.Load<ConvexPolygonShape3D>(path);
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ConvexPolygonShape3D);
    }
}
