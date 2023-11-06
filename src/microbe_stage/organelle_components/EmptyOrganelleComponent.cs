using System;
using Components;
using DefaultEcs;

/// <summary>
///   An organelle component that doesn't do anything. Used to allow organelle components that store data in their
///   object instances to exist.
/// </summary>
public abstract class EmptyOrganelleComponent : IOrganelleComponent
{
    public bool UsesSyncProcess => false;

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
    }

    public void UpdateAsync(ref OrganelleContainer organelleContainer, in Entity microbeEntity, float delta)
    {
    }

    public void UpdateSync(in Entity microbeEntity, float delta)
    {
        throw new NotSupportedException();
    }
}
