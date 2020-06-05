using System.Collections.Generic;
using Godot;

/// <summary>
///   Adds a stabby thing to the cell, positioned similarly to the flagellum
/// </summary>
public class PilusComponent : ExternallyPositionedComponent
{
    private List<uint> addedChildShapes = new List<uint>();

    CylinderShape collisionShape;
    uint ownerId;
    float pilusSize = 4.6f;

    protected override void CustomAttach()
    {
        base.CustomAttach();

        GD.Print("Atteched the Pilus");

        // Scale the size down for bacteria
        if (organelle.ParentMicrobe.Species.IsBacteria)
        {
            pilusSize *= 0.5f;
        }

        collisionShape = new CylinderShape();
        collisionShape.Radius = pilusSize / 8.0f;
        collisionShape.Height = pilusSize * organelle.Scale.Length();

        var parentMicrobe = organelle.ParentMicrobe;

        ownerId = parentMicrobe.CreateShapeOwner(collisionShape);
        parentMicrobe.ShapeOwnerAddShape(ownerId, collisionShape);

        parentMicrobe.AddPilus(ownerId);
        addedChildShapes.Add(ownerId);
    }

    protected override void CustomDetach()
    {
        DestroyShape();
    }

    protected override bool NeedsUpdateAnyway()
    {
        return addedChildShapes.Count < 1;
    }

    protected override void OnPositionChanged(Quat rotation, float angle,
        Vector3 membraneCoords)
    {
        organelle.OrganelleGraphics.Transform = new Transform(rotation, membraneCoords);

        Vector3 middle = Hex.AxialToCartesian(new Hex(0, 0));
        Vector3 membranePointDirection = (membraneCoords - middle).Normalized();

        membraneCoords += membranePointDirection * Constants.DEFAULT_HEX_SIZE * 2;

        if (organelle.ParentMicrobe.Species.IsBacteria)
        {
            membraneCoords *= 0.5f;
        }

        var physicsRotation = MathUtils.CreateRotationForPhysicsOrganelle(angle);

        // TODO: Godot doesn't have Cone shape.
        // https://github.com/godotengine/godot-proposals/issues/610
        // So this uses a cylinder for now

        // @pilusShape = organelle.world.GetPhysicalWorld().CreateCone(pilusSize / 10.f,
        //     pilusSize);

        // TODO: find a way to pass the information to the shape /
        // parentMicrobe what is a pilus part of the collision
        // pilusShape.SetCustomTag(PHYSICS_PILUS_TAG);

        var parentMicrobe = organelle.ParentMicrobe;
        var transform = new Transform(physicsRotation, membraneCoords);
        parentMicrobe.ShapeOwnerSetTransform(ownerId, transform);
    }

    private void DestroyShape()
    {
        if (addedChildShapes.Count > 0)
        {
            foreach (var shape in addedChildShapes)
            {
                organelle.ParentMicrobe.RemovePilus(shape);
                organelle.ParentMicrobe.RemoveShapeOwner(shape);
            }

            addedChildShapes.Clear();
        }
    }
}

public class PilusComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new PilusComponent();
    }

    public void Check(string name)
    {
    }
}
