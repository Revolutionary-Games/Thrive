public interface IReadOnlyOrganelleTemplate : IReadOnlyPositionedOrganelle
{
    public Compound GetActiveTargetCompound()
    {
        if (Definition.HasChemoreceptorComponent)
        {
            if (Upgrades?.CustomUpgradeData is not ChemoreceptorUpgrades chemoreceptorData)
            {
                return Constants.CHEMORECEPTOR_DEFAULT_COMPOUND;
            }

            return chemoreceptorData.TargetCompound;
        }

        // No other organelles are known to set up their active target compounds
        return Compound.Invalid;
    }

    public Enzyme? GetActiveTargetEnzyme(string internalName)
    {
        if (Definition.HasLysosomeComponent)
        {
            return LysosomeComponent.HasActiveEnzyme(Upgrades?.CustomUpgradeData as LysosomeUpgrades, internalName);
        }

        // No other organelles are known to set up their active enzymes
        return null;
    }

    public float GetActiveToxicity()
    {
        if (Upgrades?.CustomUpgradeData is ToxinUpgrades toxinData)
        {
            return toxinData.Toxicity;
        }

        return Constants.DEFAULT_TOXICITY;
    }

    public ToxinType GetActiveToxin()
    {
        if (Upgrades?.CustomUpgradeData is ToxinUpgrades toxinData)
        {
            return toxinData.BaseType;
        }

        return ToxinType.Cytotoxin;
    }

    public Species? GetActiveTargetSpecies()
    {
        if (Definition.HasChemoreceptorComponent &&
            Upgrades?.CustomUpgradeData is ChemoreceptorUpgrades chemoreceptorData)
        {
            return chemoreceptorData.TargetSpecies;
        }

        // No other organelles are known to set up their active target species
        return null;
    }

    public OrganelleTemplate Clone();
}
