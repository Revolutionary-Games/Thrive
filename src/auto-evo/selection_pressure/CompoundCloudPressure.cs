namespace AutoEvo;

using System;
using SharedBase.Archive;

public class CompoundCloudPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 2;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_COMPOUND_CLOUD_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly CompoundDefinition atp = SimulationParameters.GetCompound(Compound.ATP);

    private readonly Compound compound;
    private readonly CompoundDefinition compoundOut;

    private readonly CompoundDefinition compoundDefinition;

    private readonly bool isDayNightCycleEnabled;

    public CompoundCloudPressure(Compound compound, Compound compoundOut, bool isDayNightCycleEnabled, float weight) :
        base(weight, [
            RemoveOrganelle.ThatCreateCompound(Compound.Glucose),
            AddOrganelleAnywhere.ThatUseCompound(compound),
            new AddOrganelleAnywhere(organelle => organelle.HasChemoreceptorComponent),
            new UpgradeOrganelle(organelle => organelle.HasChemoreceptorComponent,
                new ChemoreceptorUpgrades(compound, null, Constants.CHEMORECEPTOR_RANGE_DEFAULT,
                    Constants.CHEMORECEPTOR_AMOUNT_DEFAULT, SimulationParameters.GetCompound(compound).Colour)),
            new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Activity, 150.0f),
            new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Aggression, -50.0f),
            new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Fear, -50.0f),
        ])
    {
        compoundDefinition = SimulationParameters.GetCompound(compound);

        if (!compoundDefinition.IsCloud)
            throw new ArgumentException("Given compound to cloud pressure is not of cloud type");

        this.compound = compound;
        this.compoundOut = SimulationParameters.GetCompound(compoundOut);
        this.isDayNightCycleEnabled = isDayNightCycleEnabled;
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.CompoundCloudPressure;

    public static CompoundCloudPressure ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        Compound compoundOut;

        if (version >= 2)
        {
            compoundOut = (Compound)reader.ReadInt32();
        }
        else
        {
            compoundOut = Compound.ATP;
        }

        var instance = new CompoundCloudPressure((Compound)reader.ReadInt32(), compoundOut, reader.ReadBool(),
            reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)compound);
        writer.Write((int)compoundOut.ID);
        writer.Write(isDayNightCycleEnabled);
        base.WriteToArchive(writer);
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        var score = MathF.Pow(cache.GetSpeedForSpecies(microbeSpecies), 0.6f);

        // Diminishing returns on storage
        score += (MathF.Pow(microbeSpecies.StorageCapacities.Nominal + 1, 0.8f) - 1) / 0.8f;

        var chemoreceptorScore = cache.GetChemoreceptorCloudScore(microbeSpecies, compoundDefinition, patch.Biome);

        var activity = microbeSpecies.Behaviour.Activity;

        // Species that are less active during the night get a penalty to their activity
        if (isDayNightCycleEnabled && cache.GetUsesVaryingCompoundsForSpecies(microbeSpecies, patch.Biome))
        {
            var multiplier = activity / Constants.AI_ACTIVITY_TO_BE_FULLY_ACTIVE_DURING_NIGHT;

            multiplier = Math.Max(multiplier, Constants.AUTO_EVO_MAX_NIGHT_SESSILITY_COLLECTING_PENALTY);

            if (multiplier <= 1)
                activity *= multiplier;
        }

        // modify score by activity
        var activityFraction = activity / Constants.MAX_SPECIES_ACTIVITY;

        score = (score + chemoreceptorScore) * activityFraction
            + score * (1 - activityFraction) * Constants.AUTO_EVO_PASSIVE_COMPOUND_COLLECTION_FRACTION;

        // cloud compound collection is reduced if you are chasing prey or running away from predators instead
        var aggressionFraction = microbeSpecies.Behaviour.Aggression / Constants.MAX_SPECIES_AGGRESSION;
        var fearFraction = microbeSpecies.Behaviour.Fear / Constants.MAX_SPECIES_FEAR;

        score *= (1 - aggressionFraction * Constants.AUTO_EVO_MAX_AGGRESSION_GATHERING_PENALTY)
            * (1 - fearFraction * Constants.AUTO_EVO_MAX_FEAR_GATHERING_PENALTY);

        float compoundATP;
        if (compoundOut != atp)
        {
            var compoundOutGenerated =
                cache.GetCompoundGeneratedFrom(compoundDefinition, compoundOut, microbeSpecies, patch.Biome);
            compoundATP = cache.GetCompoundConversionScoreForSpecies(compoundOut, atp, microbeSpecies) *
                compoundOutGenerated;
        }
        else
        {
            compoundATP = cache.GetCompoundGeneratedFrom(compoundDefinition, atp, microbeSpecies, patch.Biome);
        }

        var energyBalance = cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome);

        // Penalize species that don't produce enough ATP to survive from just the compound in this cloud
        score *= MathF.Min(compoundATP / energyBalance.TotalConsumption, 1);

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
