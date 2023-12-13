using Components;
using DefaultEcs;
using Godot;

/// <summary>
///   Slime-powered jet for adding bursts of speed
/// </summary>
public class SlimeJetComponent : IOrganelleComponent
{
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

        // Play the animation if active, and vice versa
        parentOrganelle.OrganelleAnimation.PlaybackSpeed = animationActive ? 1.0f : 0.0f;
        animationDirty = false;
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

        queuedForce.x = 0;
        queuedForce.y = 0;
        queuedForce.z = 0;
    }

    public Vector3 GetDirection()
    {
        Vector3 middle = Hex.AxialToCartesian(new Hex(0, 0));
        var delta = middle - organellePosition;
        if (delta == Vector3.Zero)
            delta = Components.CellPropertiesHelpers.DefaultVisualPos;
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

            return extraRotation.Xform(direction) * force;
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
