using System;
using System.Collections.Generic;
using Components;
using DefaultEcs;

/// <summary>
///   Adds extra digestion enzymes to an organelle
/// </summary>
public class LysosomeComponent : IOrganelleComponent
{
    public bool UsesSyncProcess { get; set; }

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
        var configuration = organelle.Upgrades?.CustomUpgradeData;

        var enzyme = configuration is LysosomeUpgrades upgrades ?
            upgrades.Enzyme :
            SimulationParameters.Instance.GetEnzyme("lipase");

        // TODO: avoid allocating memory like this for each lysosome component
        // Could most likely refactor the PlacedOrganelle.GetEnzymes to take in the container.AvailableEnzymes
        // dictionary and write updated values to that
        organelle.OverriddenEnzymes = new Dictionary<Enzyme, int>
        {
            { enzyme, 1 },
        };
    }

    public void UpdateAsync(ref OrganelleContainer organelleContainer, in Entity microbeEntity,
        IWorldSimulation worldSimulation, float delta)
    {
        // TODO: Animate lysosomes sticking onto phagosomes (if possible). This probably should happen in the
        // engulfing system (this at least can't happen here as Godot data update needs to happen in sync update)
    }

    public void UpdateSync(in Entity microbeEntity, float delta)
    {
        throw new NotSupportedException();
    }
}

public class LysosomeComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new LysosomeComponent();
    }

    public void Check(string name)
    {
    }
}

[JSONDynamicTypeAllowed]
public class LysosomeUpgrades : IComponentSpecificUpgrades
{
    public LysosomeUpgrades(Enzyme enzyme)
    {
        Enzyme = enzyme;
    }

    public Enzyme Enzyme { get; set; }

    public bool Equals(IComponentSpecificUpgrades? other)
    {
        if (other is not LysosomeUpgrades otherLysosome)
            return false;

        return Enzyme.InternalName.Equals(otherLysosome.Enzyme.InternalName);
    }

    public object Clone()
    {
        return new LysosomeUpgrades(Enzyme);
    }
}
