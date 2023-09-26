using System;
using System.Collections.Generic;
using Components;
using DefaultEcs;
using Godot;

/// <summary>
///   Adds radar capability to a cell
/// </summary>
public class ChemoreceptorComponent : IOrganelleComponent
{
    // Either target compound or species should be null
    private Compound? targetCompound;
    private Species? targetSpecies;
    private float searchRange;
    private float searchAmount;
    private Color lineColour = Colors.White;

    public bool UsesSyncProcess => false;

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
        base.UpdateAsync(delta);

        if (targetCompound != null)
        {
            organelle!.ParentMicrobe!.ReportActiveCompoundChemoreceptor(
                targetCompound, searchRange, searchAmount, lineColour);
        }
        else if (targetSpecies != null)
        {
            organelle!.ParentMicrobe!.ReportActiveSpeciesChemoreceptor(
                targetSpecies, searchRange, searchAmount, lineColour);
        }
    }

    protected override void CustomAttach()
    {
        if (organelle?.OrganelleGraphics == null)
            throw new InvalidOperationException("Chemoreceptor needs parent organelle to have graphics");

        var configuration = organelle.Upgrades?.CustomUpgradeData;

        // Use default values if not configured
        if (configuration == null)
        {
            SetDefaultConfiguration();
            return;
        }

        SetConfiguration((ChemoreceptorUpgrades)configuration);
    }

    public void UpdateAsync(ref OrganelleContainer organelleContainer, in Entity microbeEntity, float delta)
    {
        organelleContainer.ActiveCompoundDetections ??=
            new HashSet<(Compound Compound, float Range, float MinAmount, Color Colour)>();

        organelleContainer.ActiveCompoundDetections.Add((targetCompound!, searchRange, searchAmount, lineColour));
    }

    public void UpdateSync(in Entity microbeEntity, float delta)
    {
        throw new NotSupportedException();
    }

    private void SetConfiguration(ChemoreceptorUpgrades configuration)
    {
        targetCompound = configuration.TargetCompound;
        targetSpecies = configuration.TargetSpecies;
        searchRange = configuration.SearchRange;
        searchAmount = configuration.SearchAmount;
        lineColour = configuration.LineColour;
    }

    private void SetDefaultConfiguration()
    {
        targetCompound = SimulationParameters.Instance.GetCompound(Constants.CHEMORECEPTOR_DEFAULT_COMPOUND_NAME);
        targetSpecies = null;
        searchRange = Constants.CHEMORECEPTOR_RANGE_DEFAULT;
        searchAmount = Constants.CHEMORECEPTOR_AMOUNT_DEFAULT;
        lineColour = Colors.White;
    }
}

public class ChemoreceptorComponentFactory : IOrganelleComponentFactory
{
    public IOrganelleComponent Create()
    {
        return new ChemoreceptorComponent();
    }

    public void Check(string name)
    {
    }
}

[JSONDynamicTypeAllowed]
public class ChemoreceptorUpgrades : IComponentSpecificUpgrades
{
    public ChemoreceptorUpgrades(Compound? targetCompound, Species? targetSpecies,
        float searchRange, float searchAmount, Color lineColour)
    {
        TargetCompound = targetCompound;
        TargetSpecies = targetSpecies;
        SearchRange = searchRange;
        SearchAmount = searchAmount;
        LineColour = lineColour;
    }

    public Compound? TargetCompound { get; set; }
    public Species? TargetSpecies { get; set; }
    public float SearchRange { get; set; }
    public float SearchAmount { get; set; }
    public Color LineColour { get; set; }

    public bool Equals(IComponentSpecificUpgrades other)
    {
        if (other is not ChemoreceptorUpgrades otherChemoreceptor)
            return false;

        return TargetCompound == otherChemoreceptor.TargetCompound
            && TargetSpecies?.ID == otherChemoreceptor.TargetSpecies?.ID
            && SearchRange == otherChemoreceptor.SearchRange
            && SearchAmount == otherChemoreceptor.SearchAmount
            && LineColour == otherChemoreceptor.LineColour;
    }

    public object Clone()
    {
        return new ChemoreceptorUpgrades(TargetCompound, TargetSpecies, SearchRange, SearchAmount, LineColour);
    }
}
