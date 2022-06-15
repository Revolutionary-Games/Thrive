using Godot;

public class LysosomeComponent : IOrganelleComponent
{
    private PlacedOrganelle? organelle;

    private Enzyme lipase = null!;

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
        this.organelle = organelle;

        var configuration = organelle.Upgrades?.CustomUpgradeData;

        lipase = SimulationParameters.Instance.GetEnzyme("lipase");

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
        SetEnzyme(lipase);
    }

    private void SetConfiguration(LysosomeUpgrades configuration)
    {
        ClearEnzymes();
        SetEnzyme(configuration.Enzyme);
    }

    private void ClearEnzymes()
    {
        foreach (var enzyme in SimulationParameters.Instance.GetDigestiveEnzymes())
            SetEnzyme(enzyme, 0);
    }

    private void SetEnzyme(Enzyme enzyme, int quantity = 1)
    {
        organelle!.Definition.Enzymes![enzyme.InternalName] = quantity;
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
