using Godot;
using System;
using System.Collections.Generic;

public class PlacedOrganelle : Spatial
{
    public float Health = 1.0f;
    public OrganelleDefinition Definition;
    public Hex Position;
    public int Orientation;

    private Microbe ParentMicrobe;
    private List<uint> Shapes = new List<uint>();

    public void OnAddedToMicrobe(Microbe microbe, Hex position, int rotation) {
        microbe.AddChild(this);
        ParentMicrobe = microbe;

        var DisplayScene = GD.Load<PackedScene>(Definition.DisplayScene);
        var x = DisplayScene.Instance();
        AddChild(x);

        RotateY(rotation * 60);
        Translation = Hex.AxialToCartesian(position);
        Scale = Vector3.One * Constants.DEFAULT_HEX_SIZE;

        foreach(Hex hex in Definition.Hexes) {
            var shape = new SphereShape();
            shape.Radius = Constants.DEFAULT_HEX_SIZE / 2.0f;
            var ownerId = microbe.CreateShapeOwner(shape);
            microbe.ShapeOwnerAddShape(ownerId, shape);
            Vector3 shapePosition = Hex.AxialToCartesian(Hex.RotateAxialNTimes(hex, rotation) + position);
            var transform = new Transform(Quat.Identity, shapePosition);
            microbe.ShapeOwnerSetTransform(ownerId, transform);
            Shapes.Add(ownerId);
        }
    }

    public void OnRemovedFromMicrobe() {

    }
}
