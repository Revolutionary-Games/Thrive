namespace AutoEvo;

public class MetabolicStabilityPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("MICHE_METABOLIC_STABILITY_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident
    private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");
    private readonly Patch patch;

    public MetabolicStabilityPressure(Patch patch, float weight) : base(weight, [
        AddOrganelleAnywhere.ThatCreateCompound(ATP),
        RemoveOrganelle.ThatUseCompound(ATP),
    ])
    {
        this.patch = patch;
    }

    public override float Score(Species species, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        if (cache.GetSpeedForSpecies(microbeSpecies) == 0)
        {
            return 0.0f;
        }

        var energyBalance = cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome);

        if (energyBalance.ConservativeFinalBalance > 0)
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

    public override float GetEnergy()
    {
        return 0;
    }

    public override LocalizedString GetDescription()
    {
        return Name;
    }

    public override string ToString()
    {
        return Name.ToString();
    }
}
