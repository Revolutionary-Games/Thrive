using System.Collections.Generic;
using Godot;

/// <summary>
///   Adds a stabby thing to the cell, positioned similarly to the flagellum
/// </summary>
public class PilusComponent : ExternallyPositionedComponent
{
    private List<uint> addedChildShapes = new List<uint>();

    private Microbe currentShapesParent;

    public override void OnShapeParentChanged(Microbe newShapeParent, Vector3 offset)
    {
        if (NeedsUpdateAnyway())
        {
            // Send the organelle positions to the membrane then update their positions
            currentShapesParent.SendOrganellePositionsToMembrane();
            Update(0);
        }
        else
        {
            // Firstly the rotation relative to the master.
            var position = organelle.RotationInsideColony(lastCalculatedPosition);
            position += offset;
            Vector3 middle = offset;
            Vector3 membranePointDirection = (position - middle).Normalized();
            position += membranePointDirection * Constants.DEFAULT_HEX_SIZE * 2;

            // Pilus rotation
            var angle = GetAngle(middle - position);
            var rotation = MathUtils.CreateRotationForPhysicsOrganelle(angle);
            var transform = new Transform(rotation, position);

            // New ownerId
            var ownerId = organelle.NewOwnerId(currentShapesParent, newShapeParent, transform, addedChildShapes[0]);
            newShapeParent.AddPilus(ownerId);

            // Destroy the old shape owner
            DestroyShape();
            addedChildShapes.Add(ownerId);
        }
        currentShapesParent = newShapeParent;
    }

    protected override void CustomAttach()
    {
        currentShapesParent = organelle.ParentMicrobe;
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
        GD.Print(membraneCoords);
        organelle.OrganelleGraphics.Transform = new Transform(rotation, membraneCoords);

        Vector3 middle = Hex.AxialToCartesian(new Hex(0, 0));
        Vector3 membranePointDirection = (membraneCoords - middle).Normalized();

        membraneCoords += membranePointDirection * Constants.DEFAULT_HEX_SIZE * 2;

        if (organelle.ParentMicrobe.Species.IsBacteria)
        {
            membraneCoords *= 0.5f;
        }

        var physicsRotation = MathUtils.CreateRotationForPhysicsOrganelle(angle);
        
        var parentMicrobe = currentShapesParent;
        
        if (parentMicrobe.Colony != null && !NeedsUpdateAnyway())
        {
            // Get the real position of the pilus while in the colony
            membraneCoords = organelle.RotationInsideColony(membraneCoords);
            membraneCoords += parentMicrobe.GetOffsetRelativeToMaster();
        }

        var transform = new Transform(physicsRotation, membraneCoords);
        if(NeedsUpdateAnyway())
            CreateShape(transform, parentMicrobe);
        parentMicrobe.ShapeOwnerSetTransform(addedChildShapes[0], transform);
        // TODO: find a way to pass the information to the shape /
        // parentMicrobe what is a pilus part of the collision
        // pilusShape.SetCustomTag(PHYSICS_PILUS_TAG);

    }
    private void CreateShape(Transform transform, Microbe parent)
    {
        float pilusSize = 4.6f;

        // Scale the size down for bacteria
        if (organelle.ParentMicrobe.Species.IsBacteria)
        {
            pilusSize *= 0.5f;
        }

        // Create the shape
        // TODO: Godot doesn't have Cone shape.
        // https://github.com/godotengine/godot-proposals/issues/610
        // So this uses a cylinder for now

        // @pilusShape = organelle.world.GetPhysicalWorld().CreateCone(pilusSize / 10.f,
        //     pilusSize);
        var shape = new CylinderShape();
        shape.Radius = pilusSize / 10.0f;
        shape.Height = pilusSize;

        var ownerId = parent.CreateShapeOwner(shape);
        parent.ShapeOwnerAddShape(ownerId, shape);
        parent.AddPilus(ownerId);
        addedChildShapes.Add(ownerId);
    }
    private void DestroyShape()
    {
        if (addedChildShapes.Count > 0)
        {
            foreach (var shape in addedChildShapes)
            {
                currentShapesParent.RemovePilus(shape);
                currentShapesParent.RemoveShapeOwner(shape);
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
