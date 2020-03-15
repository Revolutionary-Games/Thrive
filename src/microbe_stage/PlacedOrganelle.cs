using System;
using System.Collections.Generic;
using Godot;

public class PlacedOrganelle : Spatial
{
    public float Health = 1.0f;
    public OrganelleDefinition Definition;
    public Hex Position;
    public int Orientation;

    private Microbe parentMicrobe;
    private List<uint> shapes = new List<uint>();

    public void OnAddedToMicrobe(Microbe microbe, Hex position, int rotation)
    {
        microbe.AddChild(this);
        parentMicrobe = microbe;
        microbe.Mass += Definition.Mass;

        var displayScene = GD.Load<PackedScene>(Definition.DisplayScene);
        var x = displayScene.Instance();
        Position = position;
        Orientation = rotation;
        AddChild(x);

        RotateY(rotation * 60);
        Translation = Hex.AxialToCartesian(position);
        Scale = Vector3.One * Constants.DEFAULT_HEX_SIZE;

        foreach (Hex hex in Definition.Hexes)
        {
            var shape = new SphereShape();
            shape.Radius = Constants.DEFAULT_HEX_SIZE / 2.0f;
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
