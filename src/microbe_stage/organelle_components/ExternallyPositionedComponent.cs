﻿using Godot;

/// <summary>
///   Base class for organelle components that position their model on a membrane edge
/// </summary>
public abstract class ExternallyPositionedComponent : IOrganelleComponent
{
    protected PlacedOrganelle organelle;

    /// <summary>
    ///   Needed to calculate final pos on update
    /// </summary>
    protected Vector3 organellePos;

    /// <summary>
    ///   Last calculated position, Used to not have to recreate the physics all the time
    /// </summary>
    protected Vector3 lastCalculatedPos = new Vector3(0, 0, 0);

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

        Vector3 middle = Hex.AxialToCartesian(new Hex(0, 0));
        var delta = middle - organellePos;
        Vector3 exit = middle - delta;
        var membraneCoords = organelle.ParentMicrobe.Membrane.GetExternalOrganelle(exit.x,
            exit.z);

        if (!membraneCoords.Equals(lastCalculatedPos) || NeedsUpdateAnyway())
        {
            float angle = Mathf.Atan2(-delta.z, delta.x);
            if (angle < 0)
            {
                angle = angle + (2 * Mathf.Pi);
            }

            angle = (angle * 180 / Mathf.Pi - 90) % 360;

            var rotation = MathUtils.CreateRotationForExternal(angle);

            OnPositionChanged(rotation, angle, membraneCoords);

            lastCalculatedPos = membraneCoords;
        }
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
}
