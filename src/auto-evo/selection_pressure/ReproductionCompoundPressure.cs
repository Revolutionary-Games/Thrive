namespace AutoEvo;

using System;
using SharedBase.Archive;

public class ReproductionCompoundPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_REPRODUCTION_COMPOUND_USAGE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly Compound compound;

    private readonly CompoundDefinition compoundDefinition;

    private readonly bool isDayNightCycleEnabled;

    public ReproductionCompoundPressure(Compound compound, bool isDayNightCycleEnabled, float weight) :
        base(weight, [
            new AddOrganelleAnywhere(organelle => organelle.HasChemoreceptorComponent),
            AddOrganelleAnywhere.ThatCreateCompound(compound),
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

    // TODO: change this and add to ThriveArchiveObjectType (probably the new saving system?)
    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.CompoundCloudPressure;

    public static ReproductionCompoundPressure ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new ReproductionCompoundPressure(
            (Compound)reader.ReadInt32(), reader.ReadBool(), reader.ReadFloat());

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

        var compoundAmount = 0.0f;

        if (patch.Biome.AverageCompounds.TryGetValue(compound, out var compoundData))
        {
            compoundAmount += compoundData.Density * compoundData.Amount;
        }

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

        // Combine with compound from all chunks
        foreach (var chunk in patch.Biome.Chunks)
        {
            if (chunk.Value.Compounds != null && chunk.Value.Compounds.ContainsKey(compound))
            {
                var chunkChemoreceptorScore = cache.GetChemoreceptorChunkScore(
                    microbeSpecies, chunk.Value, compoundDefinition, patch.Biome);
                var chunkScore = 1.0f;

                // Speed is not too important to chunk microbes,
                // but all else being the same faster is better than slower
                chunkScore += MathF.Pow(cache.GetSpeedForSpecies(microbeSpecies), 0.4f);

                // Diminishing returns on storage
                chunkScore += (MathF.Pow(microbeSpecies.StorageCapacities.Nominal + 1, 0.8f) - 1) / 0.8f;

                // If the species can't engulf, then they are dependent on only eating the runoff compounds
                if (!microbeSpecies.CanEngulf ||
                    cache.GetBaseHexSizeForSpecies(microbeSpecies) < chunk.Value.Size * Constants.ENGULF_SIZE_RATIO_REQ)
                {
                    chunkScore *= Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER;
                }

                chemoreceptorScore += chunkChemoreceptorScore;
                score += chunkScore;

                if (chunk.Value.Compounds?.TryGetValue(compoundDefinition.ID, out var chunkCompoundAmount) != true)
                    throw new ArgumentException("Chunk does not contain compound");

                var ventedCompound = MathF.Pow(chunkCompoundAmount.Amount, Constants.AUTO_EVO_CHUNK_AMOUNT_NERF);

                compoundAmount += ventedCompound;
            }
        }

        // modify score by how much compound is available for collection
        score *= compoundAmount;
        chemoreceptorScore *= compoundAmount;

        // Organelles that produce this compound
        foreach (var organelle in microbeSpecies.Organelles.Organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Outputs.TryGetValue(compoundDefinition, out var producedCompoundAmount))
                {
                    score += producedCompoundAmount * Constants.AUTO_EVO_REPRODUCTION_COMPOUND_PRODUCTION_SCORE;
                }
            }
        }

        // modify score by energy cost and activity
        var activityFraction = microbeSpecies.Behaviour.Activity / Constants.MAX_SPECIES_ACTIVITY;
        var finalScore = (score + chemoreceptorScore) * activityFraction /
            cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome).TotalConsumption;
        finalScore += score * (1 - activityFraction) * Constants.AUTO_EVO_PASSIVE_COMPOUND_COLLECTION_FRACTION /
            cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome).TotalConsumptionStationary;

        // Take into account how much compound the species needs to collect
        finalScore /= species.TotalReproductionCost[compound];

        return finalScore;
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
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
