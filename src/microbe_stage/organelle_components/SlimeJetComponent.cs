using System;
using Godot;

/// <summary>
///   Slime-powered jet for adding bursts of speed
/// </summary>
public class SlimeJetComponent : ExternallyPositionedComponent
{
    private bool active;

    private AnimationPlayer? animation;

    private Compound mucilage = null!;

    /// <summary>
    ///   Whether this jet is currently secreting slime and animating
    /// </summary>
    public bool Active
    {
        get => active;
        set
        {
            active = value;

            if (animation != null)
            {
                // Play the animation if active, and vice versa
                animation!.PlaybackSpeed = active ? 1.0f : 0.0f;
            }
        }
    }

    public override void UpdateAsync(float delta)
    {
        // Visual positioning code
        base.UpdateAsync(delta);

        // Movement force
        var microbe = organelle!.ParentMicrobe!;

        if (microbe.PhagocytosisStep != PhagocytosisPhase.None)
            return;

        var movement = CalculateMovementForce(microbe, delta);

        if (movement != Vector3.Zero)
            microbe.AddMovementForce(movement);
    }

    /// <summary>
    ///   Determines the movement impulse imparted by this jet by ejecting some mucilage
    /// </summary>
    public Vector3 CalculateMovementForce(Microbe microbe, float delta)
    {
        if (!Active)
            return Vector3.Zero;

        var currentCellRotation = microbe.GlobalTransform.basis.Quat();
        var direction = GetDirection();

        // Eject mucilage at the maximum rate in a direction opposite to this organelle's rotation
        var slimeSecreted = microbe.EjectCompound(
            mucilage, Constants.COMPOUNDS_TO_VENT_PER_SECOND * delta, -direction, 2);

        // Scale total added force by the amount ejected
        return Constants.MUCILAGE_JET_FACTOR * slimeSecreted *
            currentCellRotation.Xform(direction) / microbe.MassFromOrganelles;
    }

    public Vector3 GetDirection()
    {
        Vector3 organellePosition = Hex.AxialToCartesian(organelle!.Position);
        Vector3 middle = Hex.AxialToCartesian(new Hex(0, 0));
        var delta = middle - organellePosition;
        if (delta == Vector3.Zero)
            delta = DefaultVisualPos;
        return delta.Normalized();
    }

    protected override void CustomAttach()
    {
        mucilage = SimulationParameters.Instance.GetCompound("mucilage");

        if (organelle?.OrganelleGraphics == null)
            throw new InvalidOperationException("Slime jet needs parent organelle to have graphics");

        animation = organelle.OrganelleAnimation;

        if (animation == null)
        {
            GD.PrintErr("MovementComponent's organelle has no animation player set");
            return;
        }

        animation.GetAnimation(animation.CurrentAnimation).Loop = true;
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
    public IOrganelleComponent Create()
    {
        return new SlimeJetComponent();
    }

    public void Check(string name)
    {
    }
}
