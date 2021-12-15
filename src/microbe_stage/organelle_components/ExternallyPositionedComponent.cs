using Godot;

/// <summary>
///   Base class for organelle components that position their model on a membrane edge
/// </summary>
public abstract class ExternallyPositionedComponent : IOrganelleComponent
{
    /// <summary>
    ///   The default visual position if the organelle is on the microbe's center
    /// </summary>
    protected static readonly Vector2 DefaultVisualPos = Vector2.Up;

    protected PlacedOrganelle organelle;

    /// <summary>
    ///   Needed to calculate final pos on update
    /// </summary>
    protected Vector2 organellePos;

    /// <summary>
    ///   Last calculated position, Used to not have to recreate the physics all the time
    /// </summary>
    protected Vector2 lastCalculatedPosition = Vector2.Zero;

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
        this.organelle = organelle;
        organellePos = Hex.AxialToCartesian(organelle.Position);

        CustomAttach();
    }

    public void OnDetachFromCell(PlacedOrganelle organelle)
    {
        CustomDetach();

        this.organelle = null;
    }

    public virtual void Update(float elapsed)
    {
        // TODO: it would be nicer if this were notified when the
        // membrane changes to not recheck this constantly

        Vector2 middle = Hex.AxialToCartesian(new Hex(0, 0));
        var relativeOrganellePosition = middle - organellePos;

        if (relativeOrganellePosition == Vector2.Zero)
            relativeOrganellePosition = DefaultVisualPos;

        Vector2 exit = middle - relativeOrganellePosition;

        var membraneCoords = organelle.ParentMicrobe.Membrane.GetVectorTowardsNearestPointOfMembrane(exit.x,
            exit.y);

        if (!membraneCoords.Equals(lastCalculatedPosition) || NeedsUpdateAnyway())
        {
            float angle = GetAngle(relativeOrganellePosition);

            var rotation = MathUtils.CreateRotationForExternal(angle);

            OnPositionChanged(rotation, angle, membraneCoords);

            lastCalculatedPosition = membraneCoords;
        }
    }

    public virtual void OnShapeParentChanged(Microbe newShapeParent, Vector2 offset)
    {
    }

    /// <summary>
    ///  Gets the angle of rotation of an externally placed organelle
    /// </summary>
    /// <param name="delta"> the difference between the cell middle and the external organelle position</param>
    protected float GetAngle(Vector2 delta)
    {
        float angle = Mathf.Atan2(-delta.y, delta.x);
        if (angle < 0)
        {
            angle = angle + (2 * Mathf.Pi);
        }

        angle = (angle * 180 / Mathf.Pi - 90) % 360;
        return angle;
    }

    protected virtual void CustomAttach()
    {
    }

    protected virtual void CustomDetach()
    {
    }

    protected virtual bool NeedsUpdateAnyway()
    {
        return false;
    }

    protected abstract void OnPositionChanged(Quat rotation, float angle,
        Vector2 membraneCoords);
}
