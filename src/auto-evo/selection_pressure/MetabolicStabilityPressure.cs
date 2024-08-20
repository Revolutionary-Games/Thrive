namespace AutoEvo;

public class MetabolicStabilityPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_METABOLIC_STABILITY_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public MetabolicStabilityPressure(float weight) : base(weight, [
        AddOrganelleAnywhere.ThatCreateCompound("atp"),
        RemoveOrganelle.ThatUseCompound("atp"),
    ])
    {
    }

    public override LocalizedString Name => NameString;

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        if (cache.GetSpeedForSpecies(microbeSpecies) == 0)
        {
            return 0.0f;
        }

        var energyBalance = cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome);

        if (energyBalance.FinalBalance > 0)
        {
            return 1.0f;
        }

        if (energyBalance.FinalBalanceStationary > 0)
        {
            // Punish microbes that can't move continuously severely
            return 0.25f;
        }

        return 0.0f;
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
