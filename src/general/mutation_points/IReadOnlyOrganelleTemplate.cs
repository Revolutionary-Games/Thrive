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

    public OrganelleTemplate Clone();
}
