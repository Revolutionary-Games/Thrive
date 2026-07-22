namespace AutoEvo;

using System;
using System.Collections.Generic;
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
            new UpgradeOrganelle(organelle => organelle.HasChemoreceptorComponent,
                new ChemoreceptorUpgrades(compound, null, Constants.CHEMORECEPTOR_RANGE_DEFAULT,
                    Constants.CHEMORECEPTOR_AMOUNT_DEFAULT, SimulationParameters.GetCompound(compound).Colour)),
            new AddCellWithOrganelle(organelle => organelle.HasChemoreceptorComponent),
            AddCellWithOrganelle.ThatCreateCompound(compound),
        ])
    {
        compoundDefinition = SimulationParameters.GetCompound(compound);

        if (!compoundDefinition.IsCloud)
            throw new ArgumentException("Given compound to reproduction compound pressure is not of cloud type");

        this.compound = compound;
        this.isDayNightCycleEnabled = isDayNightCycleEnabled;
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.ReproductionCompoundPressure;

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
        float speed;
        float chemoreceptorScore;
        float nominalStorageCapacity;
        bool usesVaryingCompounds;

        var microbeBaseHexSize = 0.0f;
        var microbeCanEngulf = false;

        List<TweakedProcess> activeProcessList;

        var activity = species.Behaviour.Activity;

        if (species is MicrobeSpecies microbeSpecies)
        {
            speed = cache.GetSpeedForSpecies(microbeSpecies);
            nominalStorageCapacity = microbeSpecies.StorageCapacities.Nominal;
            usesVaryingCompounds = cache.GetUsesVaryingCompoundsForSpecies(microbeSpecies, patch.Biome);
            chemoreceptorScore = cache.GetChemoreceptorCloudScore(microbeSpecies, compoundDefinition, patch.Biome);
            activeProcessList = cache.GetActiveProcessList(microbeSpecies);
            microbeBaseHexSize = cache.GetBaseHexSizeForSpecies(microbeSpecies);
            microbeCanEngulf = microbeSpecies.CanEngulf;
        }
        else if (species is MulticellularSpecies multicellularSpecies)
        {
            speed = cache.GetSpeedForSpecies(multicellularSpecies);
            nominalStorageCapacity = multicellularSpecies.StorageCapacities.Nominal;
            usesVaryingCompounds = cache.GetUsesVaryingCompoundsForSpecies(multicellularSpecies, patch.Biome);
            chemoreceptorScore = cache.GetChemoreceptorCloudScore(multicellularSpecies, compoundDefinition,
                patch.Biome);
            activeProcessList = cache.GetActiveProcessList(multicellularSpecies);
        }
        else
        {
            return 0;
        }

        // Let the miche function even at a compound level of 0
        var compoundAmount = 1.0f;

        var mildingModifier = Constants.AUTO_EVO_REPRODUCTION_COMPOUND_COST_WEAKENING_MODIFIER;

        if (patch.Biome.AverageCompounds.TryGetValue(compound, out var compoundData))
        {
            compoundAmount += compoundData.Density * compoundData.Amount;
        }

        var score = MathF.Pow(speed, 0.6f);

        // Diminishing returns on storage
        var capacitiesScore = (MathF.Pow(nominalStorageCapacity + 1, 0.8f) - 1) * 1.25f;
        score += capacitiesScore;

        // cloud compound collection is reduced if you are chasing prey or running away from predators instead
        var aggressionPenaltyMultiplier = 1 -
            species.Behaviour.Aggression / Constants.MAX_SPECIES_AGGRESSION *
            Constants.AUTO_EVO_MAX_AGGRESSION_GATHERING_PENALTY;
        var fearPenaltyMultiplier = 1 - species.Behaviour.Fear / Constants.MAX_SPECIES_FEAR *
            Constants.AUTO_EVO_MAX_FEAR_GATHERING_PENALTY;

        score *= aggressionPenaltyMultiplier * fearPenaltyMultiplier;
        chemoreceptorScore *= aggressionPenaltyMultiplier * fearPenaltyMultiplier;

        // modify score by how much compound is available for collection
        score *= compoundAmount;
        chemoreceptorScore *= compoundAmount;

        // Precompute some scores to only resolve once.
        var speedScore = MathF.Pow(speed, 0.4f);

        var opportunismFraction = MathF.Pow(species.Behaviour.Opportunism / Constants.MAX_SPECIES_ACTIVITY, 0.5f);

        // Combine with compound amounts and scores from all chunks
        foreach (var chunk in patch.Biome.Chunks.Values)
        {
            var canEngulfChunk = false;
            if (chunk.Compounds != null && chunk.Compounds.ContainsKey(compound))
            {
                var chunkChemoreceptorScore = 0.0f;
                if (species is MicrobeSpecies microbe)
                {
                    chunkChemoreceptorScore = cache.GetChemoreceptorChunkScore(microbe, chunk, compoundDefinition);

                    if (microbeCanEngulf && microbeBaseHexSize > chunk.Size * Constants.ENGULF_SIZE_RATIO_REQ)
                        canEngulfChunk = true;
                }

                if (species is MulticellularSpecies multicellularSpecies)
                {
                    chunkChemoreceptorScore = cache.GetChemoreceptorChunkScore(multicellularSpecies, chunk,
                        compoundDefinition);

                    foreach (var cellType in multicellularSpecies.CellTypes)
                    {
                        if (canEngulfChunk)
                            break;

                        if (cellType.MembraneType.CanEngulf &&
                            cache.GetBaseHexSizeForCellType(cellType) > chunk.Size * Constants.ENGULF_SIZE_RATIO_REQ)
                        {
                            foreach (var hex in multicellularSpecies.EditorCells)
                            {
                                var cell = hex.Data;
                                if (cell != null && cell.CellType == cellType)
                                {
                                    canEngulfChunk = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                var chunkScore = 1.0f;

                // Speed is not too important to chunk microbes,
                // but all else being the same faster is better than slower
                chunkScore += speedScore;

                // Diminishing returns on storage
                chunkScore += capacitiesScore;

                // compound collection is reduced if you are running away from predators instead
                chunkScore *= fearPenaltyMultiplier;

                // If the species can't engulf, then they are dependent on only eating the runoff compounds
                if (!canEngulfChunk)
                {
                    chunkScore *= Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER;
                    chunkChemoreceptorScore *= Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER;

                    // cloud compound collection is reduced if you are chasing prey instead
                    chunkScore *= aggressionPenaltyMultiplier;
                    chunkChemoreceptorScore *= aggressionPenaltyMultiplier;
                }
                else
                {
                    score *= 1 + opportunismFraction * Constants.AUTO_EVO_MAX_OPPORTUNISM_BONUS;
                }

                if (!chunk.Compounds.TryGetValue(compoundDefinition.ID, out var chunkCompoundAmount))
                    throw new ArgumentException("Chunk does not contain compound");

                var ventedCompound = MathF.Pow(chunkCompoundAmount.Amount, Constants.AUTO_EVO_CHUNK_AMOUNT_NERF);

                // modify score by how much compound is available for collection
                chemoreceptorScore += chunkChemoreceptorScore * ventedCompound;
                score += chunkScore * ventedCompound;
            }
        }

        var finalScore = 0.1f;

        // Species that are less active during the night get a penalty to their activity
        if (isDayNightCycleEnabled && usesVaryingCompounds)
        {
            var multiplier = activity / Constants.AI_ACTIVITY_TO_BE_FULLY_ACTIVE_DURING_NIGHT;

            multiplier = Math.Max(multiplier, Constants.AUTO_EVO_MAX_NIGHT_SESSILITY_COLLECTING_PENALTY);

            if (multiplier <= 1)
                activity *= multiplier;
        }

        // modify score by activity and focus
        var activityScore = MathF.Pow(activity / Constants.MAX_SPECIES_ACTIVITY, 0.4f);
        var focusScore = MathF.Pow(species.Behaviour.Focus / Constants.MAX_SPECIES_ACTIVITY, 0.4f);

        finalScore += (score + chemoreceptorScore) * activityScore * focusScore;
        finalScore += score * (1 - activityScore * focusScore) *
            Constants.AUTO_EVO_PASSIVE_COMPOUND_COLLECTION_FRACTION;

        // Score from organelles that produce this compound
        foreach (var process in activeProcessList)
        {
            if (process.Process.Outputs.TryGetValue(compoundDefinition, out var producedCompoundAmount))
            {
                finalScore += producedCompoundAmount * Constants.AUTO_EVO_REPRODUCTION_COMPOUND_PRODUCTION_SCORE;
            }
        }

        // Take into account how much compound the species needs to collect
        finalScore /= species.TotalReproductionCost[compound] * mildingModifier;

        return finalScore;
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }

    public override LocalizedString GetDescription()
    {
        return new LocalizedString("COMPOUND_REPRODUCTION_SOURCE",
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
