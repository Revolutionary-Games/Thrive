using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Converter for <see cref="ConvexPolygonShape"/>
/// </summary>
public class ConvexPolygonShapeConverter : JsonConverter
{
    public override bool CanRead => true;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, ((ConvexPolygonShape)value).Points);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var points = serializer.Deserialize<Vector3[]>(reader);

        if (points == null)
            return null;

        return new ConvexPolygonShape { Points = points };
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ConvexPolygonShape);
    }
}
