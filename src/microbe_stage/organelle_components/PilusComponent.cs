using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Adds a stabby thing to the cell, positioned similarly to the flagellum
/// </summary>
public class PilusComponent : ExternallyPositionedComponent
{
    private const string PILUS_INJECTISOME_UPGRADE_NAME = "injectisome";

    private List<uint> addedChildShapes = new();

    private Microbe? currentShapesParent;

    public override void OnShapeParentChanged(Microbe newShapeParent, Vector3 offset)
    {
        if (currentShapesParent == null)
            throw new InvalidOperationException("Pilus not attached to a microbe yet");

        // Check if the pilus exists
        if (NeedsUpdateAnyway())
        {
            // Send the organelle positions to the membrane then update the pilus
            currentShapesParent.SendOrganellePositionsToMembrane();
            UpdateAsync(0);
            UpdateSync();

            if (newShapeParent.Colony != null)
                OnShapeParentChanged(newShapeParent, offset);
        }
        else
        {
            // Firstly the rotation relative to the master.
            var position = organelle!.RotatedPositionInsideColony(lastCalculatedPosition);

            // Then the position
            position += offset;
            Vector3 middle = offset;
            Vector3 membranePointDirection = (position - middle).Normalized();
            position += membranePointDirection * Constants.DEFAULT_HEX_SIZE * 2;

            // Pilus rotation
            var angle = GetAngle(middle - position);
            var rotation = MathUtils.CreateRotationForPhysicsOrganelle(angle);
            var transform = new Transform(rotation, position);

            throw new NotImplementedException();

            // // New ownerId
            // var ownerId = currentShapesParent.CreateNewOwnerId(newShapeParent, transform, addedChildShapes[0]);
            // newShapeParent.AddPilus(ownerId);
            //
            // // Destroy the old shape owner
            // DestroyShape();
            // addedChildShapes.Add(ownerId);
        }

        currentShapesParent = newShapeParent;
    }

    protected override void CustomAttach()
    {
        if (organelle?.OrganelleGraphics == null)
            throw new InvalidOperationException("Pilus needs parent organelle to have graphics");

        currentShapesParent = organelle!.ParentMicrobe;
    }

    protected override void CustomDetach()
    {
        DestroyShape();
        currentShapesParent = null;
    }

    protected override bool NeedsUpdateAnyway()
    {
        return addedChildShapes.Count < 1;
    }

    protected override void OnPositionChanged(Quat rotation, float angle,
        Vector3 membraneCoords)
    {
        organelle!.OrganelleGraphics!.Transform = new Transform(rotation, membraneCoords);

        Vector3 middle = Hex.AxialToCartesian(new Hex(0, 0));
        Vector3 membranePointDirection = (membraneCoords - middle).Normalized();

        membraneCoords += membranePointDirection * Constants.DEFAULT_HEX_SIZE * 2;

        if (organelle.ParentMicrobe!.CellTypeProperties.IsBacteria)
        {
            membraneCoords *= 0.5f;
        }

        var physicsRotation = MathUtils.CreateRotationForPhysicsOrganelle(angle);
        var parentMicrobe = currentShapesParent!;

        if (parentMicrobe.Colony != null && !NeedsUpdateAnyway())
        {
            // Get the real position of the pilus while in the colony
            membraneCoords = organelle.RotatedPositionInsideColony(membraneCoords);
            membraneCoords += parentMicrobe.GetOffsetRelativeToMaster();
        }

        var transform = new Transform(physicsRotation, membraneCoords);
        if (NeedsUpdateAnyway())
            CreateShape(parentMicrobe);

        throw new NotImplementedException();

        // parentMicrobe.ShapeOwnerSetTransform(addedChildShapes[0], transform);
    }

    private void CreateShape(Microbe parent)
    {
        float pilusSize = 4.6f;

        // Scale the size down for bacteria
        if (organelle!.ParentMicrobe!.CellTypeProperties.IsBacteria)
        {
            pilusSize *= 0.5f;
        }

        // Turns out cones are really hated by physics engines, so we'll need to permanently make do with a cylinder
        var shape = new CylinderShape();
        shape.Radius = pilusSize / 10.0f;
        shape.Height = pilusSize;

        throw new NotImplementedException();

        // var ownerId = parent.CreateShapeOwner(shape);
        // parent.ShapeOwnerAddShape(ownerId, shape);
        // parent.AddPilus(ownerId);
        // addedChildShapes.Add(ownerId);
    }

    private void DestroyShape()
    {
        if (addedChildShapes.Count > 0)
        {
            foreach (var shape in addedChildShapes)
            {
                currentShapesParent!.RemovePilus(shape);

                throw new NotImplementedException();

                // currentShapesParent.RemoveShapeOwner(shape);
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
