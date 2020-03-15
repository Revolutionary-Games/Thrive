using System;
using System.Collections.Generic;
using Godot;

public class PlacedOrganelle : Spatial
{
    public OrganelleDefinition Definition;
    public Hex Position;

    /// <summary>
    ///   This is now the number of times to rotate. This used to be the angle in degrees
    /// </summary>
    public int Orientation;

    private Microbe parentMicrobe;
    private List<uint> shapes = new List<uint>();

    public void OnAddedToMicrobe(Microbe microbe, Hex position, int rotation)
    {
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
        RotateY(rotation * 60);
        Translation = Hex.AxialToCartesian(position);
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
                Hex.RotateAxialNTimes(hex, rotation) + position);
            var transform = new Transform(Quat.Identity, shapePosition);
            microbe.ShapeOwnerSetTransform(ownerId, transform);

            shapes.Add(ownerId);
        }
    }

    public void OnRemovedFromMicrobe()
    {
    }
}
