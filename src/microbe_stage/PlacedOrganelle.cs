using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   And organelle that has been placed in a microbe.
/// </summary>
[JsonConverter(typeof(PlacedOrganelleConverter))]
public class PlacedOrganelle : Spatial
{
    public OrganelleDefinition Definition;
    public Hex Position;

    /// <summary>
    ///   This is now the number of times to rotate. This used to be the angle in degrees
    /// </summary>
    public int Orientation;

    [JsonIgnore]
    private Microbe parentMicrobe;
    [JsonIgnore]
    private List<uint> shapes = new List<uint>();

    public void OnAddedToMicrobe(Microbe microbe, Hex position, int rotation)
    {
        if (Definition == null)
        {
            throw new Exception("PlacedOrganelle has no definition set");
        }

        microbe.AddChild(this);

        // Store parameters
        parentMicrobe = microbe;
        Position = position;
        Orientation = rotation;

        // Graphical display
        if (Definition.LoadedScene != null)
        {
            AddChild(Definition.LoadedScene.Instance());
        }

        // Position relative to origin of cell
        RotateY(Orientation * 60);
        Translation = Hex.AxialToCartesian(Position);
        Scale = Vector3.One * Constants.DEFAULT_HEX_SIZE;

        // Physics
        microbe.Mass += Definition.Mass;

        foreach (Hex hex in Definition.Hexes)
        {
            var shape = new SphereShape();
            shape.Radius = Constants.DEFAULT_HEX_SIZE * 2.0f;

            var ownerId = microbe.CreateShapeOwner(shape);
            microbe.ShapeOwnerAddShape(ownerId, shape);
            Vector3 shapePosition = Hex.AxialToCartesian(
                Hex.RotateAxialNTimes(hex, Orientation) + Position);
            var transform = new Transform(Quat.Identity, shapePosition);
            microbe.ShapeOwnerSetTransform(ownerId, transform);

            shapes.Add(ownerId);
        }
    }

    public void OnRemovedFromMicrobe()
    {
    }
}

/// <summary>
///   Custom serializer for PlacedOrganelle and PlacedOrganelle
/// </summary>
internal class PlacedOrganelleConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(PlacedOrganelle) ||
            objectType == typeof(OrganelleTemplate);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
        JsonSerializer serializer)
    {
        // TODO: implement reading
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            serializer.Serialize(writer, null);
            return;
        }

        // Get all properties
        var properties = value.GetType().GetProperties().Where(
            (p) => !p.CustomAttributes.Any(
                (a) => a.AttributeType == typeof(JsonIgnoreAttribute)));

        writer.WriteStartObject();

        foreach (var property in properties)
        {
            writer.WritePropertyName(property.Name);

            // Use default serializer on everything except Definition
            if (property.Name == "Definition")
            {
                serializer.Serialize(writer,
                    ((OrganelleDefinition)property.GetValue(value, null)).InternalName);
            }
            else
            {
                serializer.Serialize(writer, property.GetValue(value, null));
            }
        }

        writer.WriteEndObject();
    }
}
