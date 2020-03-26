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

    /// <summary>
    ///   True when organelle was split in preparation for reproducing
    /// </summary>
    public bool WasSplit { get; set; } = false;

    /// <summary>
    ///   The components instantiated for this placed organelle
    /// </summary>
    [JsonIgnore]
    public List<IOrganelleComponent> Components { get; private set; }

    /// <summary>
    ///   Computes the total storage capacity of this organelle. Works
    ///   only after being added to a microbe and before being
    ///   removed.
    /// </summary>
    public float StorageCapacity
    {
        get
        {
            float value = 0.0f;

            foreach (var component in Components)
            {
                if (component is StorageComponent storage)
                {
                    value += storage.Capacity;
                }
            }

            return value;
        }
    }

    /// <summary>
    ///   Checks if this organelle has the specified component type
    /// </summary>
    public bool HasComponent<T>()
        where T : class
    {
        foreach (var component in Components)
        {
            // TODO: determine if is T or as T is better
            if (component as T != null)
                return true;
        }

        return false;
    }

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

        // Components
        Components = new List<IOrganelleComponent>();

        foreach (var factory in Definition.ComponentFactories)
        {
            var component = factory.Create();

            component.OnAttachToCell();

            Components.Add(component);
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

        // Remove components
        foreach (var component in Components)
        {
            component.OnDetachFromCell();
        }

        Components = null;

        parentMicrobe = null;
    }

    public void Update(float delta)
    {
        foreach (var component in Components)
        {
            component.Update(delta);
        }
    }
}

/// <summary>
///   Custom serializer for PlacedOrganelle and OrganelleTemplate
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
