namespace AutoEvo;

public class MetabolicStabilityPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("METABOLIC_STABILITY_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident
    private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");
    private readonly Patch patch;
    private readonly float weight;

    public MetabolicStabilityPressure(Patch patch, float weight) : base(weight,
        [
            AddOrganelleAnywhere.ThatCreateCompound(ATP),
            new AddOrganelleAnywhere(_ => true),
        ],
        0)
    {
        this.patch = patch;
        this.weight = weight;
    }

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        if (species.BaseSpeed == 0)
        {
            return 0.0f;
        }

        return ScoreByCell(species, cache);
    }

    public override string ToString()
    {
        return Name.ToString();
    }

    private float ScoreByCell(MicrobeSpecies species, SimulationCache cache)
    {
        var energyBalance = cache.GetEnergyBalanceForSpecies(species, patch.Biome);

        if (energyBalance.FinalBalance > 0)
        {
            return 1.0f * weight;
        }

        if (energyBalance.FinalBalanceStationary >= 0)
        {
            return 0.5f * weight;
        }

        return 0.0f;
    }
}
