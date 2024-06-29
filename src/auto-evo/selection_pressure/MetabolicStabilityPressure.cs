namespace AutoEvo;

using System;

public class MetabolicStabilityPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    public static readonly LocalizedString Name = new LocalizedString("METABOLIC_STABILITY_PRESSURE");

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

    public override float Score(MicrobeSpecies species, SimulationCache cache)
    {
        if (cache.GetBaseSpeedForSpecies(species) == 0)
        {
            return 0.0f;
        }

        var energyBalance = cache.GetEnergyBalanceForSpecies(species, patch.Biome);

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

    public override float GetEnergy()
    {
        return 0;
    }

    public override IFormattable GetDescription()
    {
        // This shouldn't be called on 0 energy pressures
        return Name;
    }

    public override string ToString()
    {
        return Name.ToString();
    }
}
