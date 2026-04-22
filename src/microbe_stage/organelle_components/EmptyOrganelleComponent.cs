using System;
using Arch.Core;
using Components;

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

    public void UpdateAsync(ref OrganelleContainer organelleContainer, ref SpecializationFactor specializationFactor,
        in Entity microbeEntity,
        IWorldSimulation worldSimulation, float energyCostMultiplier, float delta)
    {
    }

    public void UpdateSync(in Entity microbeEntity, float delta)
    {
        throw new NotSupportedException();
    }
}
