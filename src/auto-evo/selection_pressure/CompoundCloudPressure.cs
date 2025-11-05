namespace AutoEvo;

using System;
using SharedBase.Archive;

public class CompoundCloudPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_COMPOUND_CLOUD_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly Compound compound;

    private readonly CompoundDefinition compoundDefinition;

    private readonly bool isDayNightCycleEnabled;

    public CompoundCloudPressure(Compound compound, bool isDayNightCycleEnabled, float weight) :
        base(weight, [
            new AddOrganelleAnywhere(organelle => organelle.HasChemoreceptorComponent),
            new ChangeMembraneRigidity(true),
            new UpgradeOrganelle(organelle => organelle.HasChemoreceptorComponent,
                new ChemoreceptorUpgrades(compound, null, Constants.CHEMORECEPTOR_RANGE_DEFAULT,
                    Constants.CHEMORECEPTOR_AMOUNT_DEFAULT, SimulationParameters.GetCompound(compound).Colour)),
            new ChangeMembraneType("single"),
        ])
    {
        compoundDefinition = SimulationParameters.GetCompound(compound);

        if (!compoundDefinition.IsCloud)
            throw new ArgumentException("Given compound to cloud pressure is not of cloud type");

        this.compound = compound;
        this.isDayNightCycleEnabled = isDayNightCycleEnabled;
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.CompoundCloudPressure;

    public static CompoundCloudPressure ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new CompoundCloudPressure((Compound)reader.ReadInt32(), reader.ReadBool(), reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)compound);
        writer.Write(isDayNightCycleEnabled);
        base.WriteToArchive(writer);
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        var score = MathF.Pow(cache.GetSpeedForSpecies(microbeSpecies), 0.6f);

        // Species that are less active during the night get a small penalty here based on their activity
        if (isDayNightCycleEnabled && cache.GetUsesVaryingCompoundsForSpecies(microbeSpecies, patch.Biome))
        {
            var multiplier = species.Behaviour.Activity / Constants.AI_ACTIVITY_TO_BE_FULLY_ACTIVE_DURING_NIGHT;

            // Make the multiplier less extreme
            multiplier *= Constants.AUTO_EVO_NIGHT_SESSILITY_COLLECTING_PENALTY_MULTIPLIER;

            multiplier = Math.Max(multiplier, Constants.AUTO_EVO_MAX_NIGHT_SESSILITY_COLLECTING_PENALTY);

            if (multiplier <= 1)
                score *= multiplier;
        }

        var chemoreceptorScore = cache.GetChemoreceptorCloudScore(microbeSpecies, compoundDefinition, patch.Biome);
        score += chemoreceptorScore;

        return score;
    }

    public override float GetEnergy(Patch patch)
    {
        if (patch.Biome.AverageCompounds.TryGetValue(compound, out var compoundData))
        {
            return compoundData.Density * compoundData.Amount * Constants.AUTO_EVO_COMPOUND_ENERGY_AMOUNT;
        }

        return 0.0f;
    }

    public override LocalizedString GetDescription()
    {
        return new LocalizedString("COMPOUND_FOOD_SOURCE",
            new LocalizedString(compoundDefinition.GetUntranslatedName()));
    }

    public Compound GetUsedCompoundType()
    {
        return compound;
    }

    public override string ToString()
    {
        return $"{Name} ({compoundDefinition.Name})";
    }
}
