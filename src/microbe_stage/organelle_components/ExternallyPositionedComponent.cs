using Godot;

/// <summary>
///   Base class for organelle components that position their model on a membrane edge
/// </summary>
public abstract class ExternallyPositionedComponent : IOrganelleComponent
{
    /// <summary>
    ///   The default visual position if the organelle is on the microbe's center
    ///   TODO: this should be made organelle type specific, chemoreceptors and pilus should point backward (in Godot
    ///   coordinates to point forwards by default, and flagella should keep this current default value)
    /// </summary>
    protected static readonly Vector3 DefaultVisualPos = Vector3.Forward;

    protected PlacedOrganelle? organelle;

    /// <summary>
    ///   Needed to calculate final pos on update
    /// </summary>
    protected Vector3 organellePos;

    /// <summary>
    ///   Last calculated position, Used to not have to recreate the physics all the time
    /// </summary>
    protected Vector3 lastCalculatedPosition = Vector3.Zero;

    /// <summary>
    ///   To support splitting processing to the async and sync phases this stores the async phase result before
    ///   applying this to the Godot objects
    /// </summary>
    protected Vector3? calculatedNewPosition;

    protected float calculatedNewAngle;

    private bool skippedAsyncProcess;

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

    /// <summary>
    ///   Positions the external organelle the right way
    /// </summary>
    /// <param name="delta">Time since last frame</param>
    /// <remarks>
    ///   <para>
    ///     TODO: in profiling this is quite a hot spot so this should be optimized for when this needs to run
    ///   </para>
    /// </remarks>
    public virtual void UpdateAsync(float delta)
    {
        // TODO: it would be nicer if this were notified when the membrane changes to not recheck this constantly

        var membrane = organelle!.ParentMicrobe!.Membrane;

        // Skip updating if membrane is not ready yet for us to read it and do it in the sync update instead
        if (membrane.Dirty)
        {
            skippedAsyncProcess = true;
            return;
        }

        CheckPositioningWithMembrane();
    }

    public virtual void UpdateSync()
    {
        if (skippedAsyncProcess)
        {
            CheckPositioningWithMembrane();
            skippedAsyncProcess = false;
        }

        if (calculatedNewPosition == null)
            return;

        var rotation = MathUtils.CreateRotationForExternal(calculatedNewAngle);

        OnPositionChanged(rotation, calculatedNewAngle, calculatedNewPosition.Value);

        calculatedNewPosition = null;
    }

    public virtual void OnShapeParentChanged(Microbe newShapeParent, Vector3 offset)
    {
    }

    /// <summary>
    ///   Gets the angle of rotation of an externally placed organelle
    /// </summary>
    /// <param name="delta">The difference between the cell middle and the external organelle position</param>
    protected float GetAngle(Vector3 delta)
    {
        float angle = Mathf.Atan2(-delta.z, delta.x);
        if (angle < 0)
        {
            angle += 2 * Mathf.Pi;
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
        Vector3 membraneCoords);

    private void CheckPositioningWithMembrane()
    {
        var membrane = organelle!.ParentMicrobe!.Membrane;

        Vector3 middle = Hex.AxialToCartesian(new Hex(0, 0));
        var relativeOrganellePosition = middle - organellePos;

        if (relativeOrganellePosition == Vector3.Zero)
            relativeOrganellePosition = DefaultVisualPos;

        Vector3 exit = middle - relativeOrganellePosition;
        var membraneCoords = membrane.GetVectorTowardsNearestPointOfMembrane(exit.x,
            exit.z);

        if (!membraneCoords.Equals(lastCalculatedPosition) || NeedsUpdateAnyway())
        {
            calculatedNewAngle = GetAngle(relativeOrganellePosition);
            calculatedNewPosition = membraneCoords;
            lastCalculatedPosition = membraneCoords;
        }
    }
}
