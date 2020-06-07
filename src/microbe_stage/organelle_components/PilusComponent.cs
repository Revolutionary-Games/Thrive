using System.Collections.Generic;
using Godot;

/// <summary>
///   Adds a stabby thing to the cell, positioned similarly to the flagellum
/// </summary>
public class PilusComponent : ExternallyPositionedComponent
{
    private uint ownerId;
    private bool ownerSet = false;

    protected override void CustomDetach()
    {
        DestroyShape();
    }

    protected override bool NeedsUpdateAnyway()
    {
        return ownerSet && organelle.ParentMicrobe.ShapeOwnerGetShapeCount(ownerId) < 1;
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

        float pilusSize = 4.6f;

        // Scale the size down for bacteria
        if (organelle.ParentMicrobe.Species.IsBacteria)
        {
            pilusSize *= 0.5f;
        }

        var physicsRotation = MathUtils.CreateRotationForPhysicsOrganelle(angle);

        // Need to remove the old copy first
        DestroyShape();

        // TODO: Godot doesn't have Cone shape.
        // https://github.com/godotengine/godot-proposals/issues/610
        // So this uses a cylinder for now

        // @pilusShape = organelle.world.GetPhysicalWorld().CreateCone(pilusSize / 10.f,
        //     pilusSize);

        var shape = new CylinderShape();
        shape.Radius = pilusSize / 10.0f;
        shape.Height = pilusSize;

        var parentMicrobe = organelle.ParentMicrobe;

        if(!ownerSet)
        {
            ownerId = parentMicrobe.CreateShapeOwner(parentMicrobe);
            ownerSet = true;
        }

        parentMicrobe.ShapeOwnerAddShape(ownerId, shape);

        // TODO: find a way to pass the information to the shape /
        // parentMicrobe what is a pilus part of the collision
        // pilusShape.SetCustomTag(PHYSICS_PILUS_TAG);

        var transform = new Transform(physicsRotation, membraneCoords);
        parentMicrobe.ShapeOwnerSetTransform(ownerId, transform);

        parentMicrobe.AddPilus(ownerId);
    }

    private void DestroyShape()
    {
        var parentMicrobe = organelle.ParentMicrobe;

        if(parentMicrobe.GetShapeOwners().Count > 0)
        {
            var shapeCount = parentMicrobe.ShapeOwnerGetShapeCount(ownerId);
            for (int i = 0; i < shapeCount; ++i)
            {
                parentMicrobe.ShapeOwnerRemoveShape(ownerId, i);
            }
        }

        parentMicrobe.RemoveShapeOwner(ownerId);
        parentMicrobe.RemovePilus(ownerId);
        ownerSet = false;
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
