using System;
using Godot;

public class LysosomeComponent : IOrganelleComponent
{
    private PlacedOrganelle? organelle;

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
        this.organelle = organelle;

        var configuration = organelle.Upgrades?.CustomUpgradeData;

        // Use default values if not configured
        if (configuration == null)
        {
            SetDefaultConfiguration();
            return;
        }

        SetConfiguration((LysosomeUpgrades)configuration);
    }

    public void OnDetachFromCell(PlacedOrganelle organelle)
    {
    }

    public void UpdateAsync(float delta)
    {
        // TODO: Animate lysosomes sticking onto phagosomes (if possible)
    }

    public void UpdateSync()
    {
    }

    public void OnShapeParentChanged(Microbe newShapeParent, Vector3 offset)
    {
    }

    private void SetDefaultConfiguration()
    {
        ClearEnzymes();
        var lipase = SimulationParameters.Instance.GetEnzyme("lipase");
        organelle!.Definition.Enzymes![lipase.InternalName] = 1;
    }

    private void SetConfiguration(LysosomeUpgrades configuration)
    {
        ClearEnzymes();
        organelle!.Definition.Enzymes![configuration.Enzyme.InternalName] = 1;
    }

    private void ClearEnzymes()
    {
        foreach (var enzyme in SimulationParameters.Instance.GetDigestiveEnzymes())
        {
            organelle!.Definition.Enzymes![enzyme.InternalName] = 0;
        }
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

    public object Clone()
    {
        return new LysosomeUpgrades(Enzyme);
    }
}