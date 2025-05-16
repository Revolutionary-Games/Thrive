namespace AutoEvo;

using Newtonsoft.Json;

[JSONDynamicTypeAllowed]
public class MetabolicStabilityPressure : SelectionPressure
{
    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_METABOLIC_STABILITY_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public MetabolicStabilityPressure(float weight) : base(weight, [
        AddOrganelleAnywhere.ThatCreateCompound(Compound.ATP),
        RemoveOrganelle.ThatUseCompound(Compound.ATP),
        new UpgradeOrganelle(organelle => organelle.HasMovementComponent, new FlagellumUpgrades(-1.0f)),
    ])
    {
    }

    [JsonIgnore]
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
    if (microbeSpecies.Behaviour.Activity > 0)
        {
        // Non-sessile species are punished for not moving continuously
        return 0.25f;
        }

    // Sessile species are rewarded if they can maintain energy while stationary
    return 1.0f;
    }

        }

        return 0.0f;
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
