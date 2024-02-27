using Components;
using DefaultEcs;
using Godot;

/// <summary>
///   Slime-powered jet for adding bursts of speed
/// </summary>
public class SlimeJetComponent : IOrganelleComponent
{
    private const string SlimeJetAnimationName = "SlimeJet";

    private bool animationActive;
    private bool animationDirty = true;

    private PlacedOrganelle parentOrganelle = null!;

    private Vector3 organellePosition;
    private Vector3 queuedForce = Vector3.Zero;

    public bool UsesSyncProcess => animationDirty;

    /// <summary>
    ///   Whether this jet is currently secreting slime (and animating)
    /// </summary>
    public bool Active
    {
        get => animationActive;
        set
        {
            if (animationActive == value)
                return;

            animationActive = value;
            animationDirty = true;
        }
    }

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
        // See comment in MovementComponent.OnAttachToCell
        parentOrganelle = organelle;

        organellePosition = Hex.AxialToCartesian(organelle.Position);
    }

    public void UpdateAsync(ref OrganelleContainer organelleContainer, in Entity microbeEntity,
        IWorldSimulation worldSimulation, float delta)
    {
        // All of the logic for this ended up in MicrobeEmissionSystem and MicrobeMovementSystem, just the animation
        // applying is here anymore...
    }

    public void UpdateSync(in Entity microbeEntity, float delta)
    {
        if (parentOrganelle.OrganelleAnimation == null)
            return;

        // Start the animation if it should play and otherwise just wait for the animation to stop
        if (!animationActive)
        {
            animationDirty = false;
            return;
        }

        if (!parentOrganelle.OrganelleAnimation.IsPlaying())
            parentOrganelle.OrganelleAnimation.Play(SlimeJetAnimationName);

        // animationDirty is not set false here as otherwise we won't know when the playing stops and we need to
        // start the animation again to keep playing if the jet is active for long
    }

    public void AddQueuedForce(in Entity entity, float slimeAmount)
    {
        if (!Active)
        {
            GD.PrintErr("Non-active slime jet attempt to add force");
            return;
        }

        queuedForce += CalculateMovementForce(entity, slimeAmount);
    }

    public void ConsumeMovementForce(out Vector3 force)
    {
        force = queuedForce;

        queuedForce.X = 0;
        queuedForce.Y = 0;
        queuedForce.Z = 0;
    }

    public Vector3 GetDirection()
    {
        Vector3 middle = Hex.AxialToCartesian(new Hex(0, 0));
        var delta = middle - organellePosition;
        if (delta == Vector3.Zero)
            delta = CellPropertiesHelpers.DefaultVisualPos;
        return delta.Normalized();
    }

    /// <summary>
    ///   Determines the movement impulse imparted by this jet by ejecting some mucilage
    /// </summary>
    private Vector3 CalculateMovementForce(in Entity entity, float slimeAmount)
    {
        if (!Active)
            return Vector3.Zero;

        // Scale total added force by the amount ejected
        // TODO: this used to be divided by "microbe.MassFromOrganelles" make sure this force still makes sense (and
        // considering the new physics engine)
        float force = Constants.MUCILAGE_JET_FACTOR * slimeAmount;

        var direction = GetDirection();

        // Take rotation in colony into account
        // TODO: verify this math actually ends up correct considering the rotating of the movement vector in the
        // microbe movement system
        if (entity.Has<AttachedToEntity>())
        {
            var extraRotation = entity.Get<AttachedToEntity>().RelativeRotation;

            return extraRotation * direction * force;
        }

        return direction * force;
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
