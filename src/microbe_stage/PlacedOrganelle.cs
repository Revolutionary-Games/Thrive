using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   And organelle that has been placed in a microbe.
/// </summary>
[JsonConverter(typeof(PlacedOrganelleConverter))]
public class PlacedOrganelle : Spatial, IPositionedOrganelle
{
    [JsonIgnore]
    private Microbe parentMicrobe;
    [JsonIgnore]
    private List<uint> shapes = new List<uint>();

    public OrganelleDefinition Definition { get; set; }

    public Hex Position { get; set; }

    public int Orientation { get; set; }

    public void OnAddedToMicrobe(Microbe microbe)
    {
        if (Definition == null)
        {
            throw new Exception("PlacedOrganelle has no definition set");
        }

        // Store parameters
        parentMicrobe = microbe;

        parentMicrobe.AddChild(this);

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
        parentMicrobe.Mass += Definition.Mass;

        foreach (Hex hex in Definition.Hexes)
        {
            var shape = new SphereShape();
            shape.Radius = Constants.DEFAULT_HEX_SIZE * 2.0f;

            var ownerId = parentMicrobe.CreateShapeOwner(shape);
            parentMicrobe.ShapeOwnerAddShape(ownerId, shape);
            Vector3 shapePosition = Hex.AxialToCartesian(
                Hex.RotateAxialNTimes(hex, Orientation) + Position);
            var transform = new Transform(Quat.Identity, shapePosition);
            parentMicrobe.ShapeOwnerSetTransform(ownerId, transform);

            shapes.Add(ownerId);
        }
    }

    public void OnRemovedFromMicrobe()
    {
        parentMicrobe.RemoveChild(this);

        // Remove physics
        parentMicrobe.Mass -= Definition.Mass;

        foreach (var shape in shapes)
        {
            parentMicrobe.RemoveShapeOwner(shape);
        }

        parentMicrobe = null;
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
