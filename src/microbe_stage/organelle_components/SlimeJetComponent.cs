using System;
using Godot;

/// <summary>
///   Slime-powered jet for adding bursts of speed
/// </summary>
public class SlimeJetComponent : ExternallyPositionedComponent
{
    public Vector3 GetDirection()
    {
        Vector3 organellePosition = Hex.AxialToCartesian(organelle!.Position);
        Vector3 middle = Hex.AxialToCartesian(new Hex(0, 0));
        var delta = middle - organellePosition;
        if (delta == Vector3.Zero)
            delta = DefaultVisualPos;
        return delta.Normalized();
    }

    protected override bool NeedsUpdateAnyway()
    {
        // The basis of the transform represents the rotation, as long as the rotation is not modified,
        // the organelle needs to be updated.
        // TODO: Calculated rotations should never equal the identity,
        // it should be kept an eye on if it does. The engine for some reason doesnt update THIS basis
        // unless checked with some condition (if or return)
        // SEE: https://github.com/Revolutionary-Games/Thrive/issues/2906
        return organelle!.OrganelleGraphics!.Transform.basis == Transform.Identity.basis;
    }

    protected override void OnPositionChanged(Quat rotation, float angle,
        Vector3 membraneCoords)
    {
        organelle!.OrganelleGraphics!.Transform = new Transform(rotation, membraneCoords);
    }
}

public class SlimeJetComponentFactory : IOrganelleComponentFactory
{
    public float Momentum;

    public IOrganelleComponent Create()
    {
        return new SlimeJetComponent();
    }

    public void Check(string name)
    {
    }
}
