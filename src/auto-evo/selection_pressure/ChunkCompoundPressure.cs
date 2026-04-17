namespace AutoEvo;

using System;
using SharedBase.Archive;

public class ChunkCompoundPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 2;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_CHUNK_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly CompoundDefinition atp = SimulationParameters.GetCompound(Compound.ATP);

    private readonly string chunkType;

    private readonly LocalizedString readableName;

    private readonly CompoundDefinition compound;
    private readonly CompoundDefinition compoundOut;

    private readonly bool isDayNightCycleEnabled;

    public ChunkCompoundPressure(string chunkType, LocalizedString readableName, Compound compound,
        Compound compoundOut, bool isDayNightCycleEnabled, float weight) : base(weight, [
        new RemoveOrganelle(_ => true),
        new AddOrganelleAnywhere(organelle => organelle.HasChemoreceptorComponent),
        new AddOrganelleAnywhere(organelle => organelle.InternalName == "vacuole"),
        AddOrganelleAnywhere.ThatConvertBetweenCompounds(compound, compoundOut),
        AddOrganelleAnywhere.ThatUseCompound(compoundOut),
        new UpgradeOrganelle(organelle => organelle.HasChemoreceptorComponent,
            new ChemoreceptorUpgrades(compound, null, Constants.CHEMORECEPTOR_RANGE_DEFAULT,
                Constants.CHEMORECEPTOR_AMOUNT_DEFAULT, SimulationParameters.GetCompound(compound).Colour)),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Activity, 150.0f),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Activity, -150.0f),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Aggression, 50.0f),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Aggression, -150.0f),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Fear, 150.0f),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Fear, -150.0f),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Focus, 150.0f),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Focus, -150.0f),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Opportunism, 150.0f),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Opportunism, -150.0f),
        new ChangeMembraneType("single"),
        new ChangeMembraneType("double"),
    ])
    {
        this.compound = SimulationParameters.GetCompound(compound);
        this.compoundOut = SimulationParameters.GetCompound(compoundOut);
        this.chunkType = chunkType;
        this.readableName = readableName;
        this.isDayNightCycleEnabled = isDayNightCycleEnabled;
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.ChunkCompoundPressure;

    public static ChunkCompoundPressure ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var chunkType = reader.ReadString();
        var readableName = reader.ReadObject<LocalizedString>();
        var compound = (Compound)reader.ReadInt32();
        var compoundOut = (Compound)reader.ReadInt32();
        bool isDayNightCycleEnabled;

        if (version >= 2)
        {
            isDayNightCycleEnabled = reader.ReadBool();
        }
        else
        {
            isDayNightCycleEnabled = true;
        }

        var instance = new ChunkCompoundPressure(chunkType ?? throw new NullArchiveObjectException(),
            readableName, compound, compoundOut,
            isDayNightCycleEnabled, reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(chunkType);
        writer.WriteObject(readableName);
        writer.Write((int)compound.ID);
        writer.Write((int)compoundOut.ID);
        writer.Write(isDayNightCycleEnabled);
        base.WriteToArchive(writer);
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (!patch.Biome.Chunks.TryGetValue(chunkType, out var chunk))
            throw new ArgumentException("Chunk does not exist in patch");

        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        var score = 1.0f;

        // Speed is not too important to chunk microbes, but all else being the same faster is better than slower
        score += MathF.Pow(cache.GetSpeedForSpecies(microbeSpecies), 0.4f);

        // Diminishing returns on storage
        score += (MathF.Pow(microbeSpecies.StorageCapacities.Nominal + 1, 0.8f) - 1) / 0.8f;

        // Additional bonus from chemoreceptor
        var chemoreceptorScore = cache.GetChemoreceptorChunkScore(microbeSpecies, chunk, compound);

        var activity = microbeSpecies.Behaviour.Activity;

        // Species that are less active during the night get a penalty to their activity
        if (isDayNightCycleEnabled && cache.GetUsesVaryingCompoundsForSpecies(microbeSpecies, patch.Biome))
        {
            var multiplier = activity / Constants.AI_ACTIVITY_TO_BE_FULLY_ACTIVE_DURING_NIGHT;

            multiplier = Math.Max(multiplier, Constants.AUTO_EVO_MAX_NIGHT_SESSILITY_COLLECTING_PENALTY);

            if (multiplier <= 1)
                activity *= multiplier;
        }

        // modify score by activity and focus
        var activityScore = MathF.Pow(activity / Constants.MAX_SPECIES_ACTIVITY, 0.4f);
        var focusScore = 1 + MathF.Pow(microbeSpecies.Behaviour.Focus / Constants.MAX_SPECIES_ACTIVITY, 0.4f)
            * Constants.AUTO_EVO_MAX_FOCUS_CHUNK_BONUS;

        score = (score + chemoreceptorScore) * activityScore * focusScore
            + score * (1 - activityScore * focusScore) * Constants.AUTO_EVO_PASSIVE_COMPOUND_COLLECTION_FRACTION;

        // compound collection is reduced if you are running away from predators instead
        var fearFraction = microbeSpecies.Behaviour.Fear / Constants.MAX_SPECIES_FEAR;

        score *= 1 - fearFraction * Constants.AUTO_EVO_MAX_FEAR_GATHERING_PENALTY;

        // If the species can't engulf, then they are dependent on only eating the runoff compounds
        if (!microbeSpecies.CanEngulf ||
            cache.GetBaseHexSizeForSpecies(microbeSpecies) < chunk.Size * Constants.ENGULF_SIZE_RATIO_REQ)
        {
            score *= Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER;

            // cloud compound collection is reduced if you are chasing prey instead
            var aggressionFraction = microbeSpecies.Behaviour.Aggression / Constants.MAX_SPECIES_AGGRESSION;

            score *= 1 - aggressionFraction * Constants.AUTO_EVO_MAX_AGGRESSION_GATHERING_PENALTY;
        }
        else
        {
            var opportunismFraction = MathF.Pow(
                microbeSpecies.Behaviour.Opportunism / Constants.MAX_SPECIES_ACTIVITY, 0.5f);
            score *= 1 + opportunismFraction * Constants.AUTO_EVO_MAX_OPPORTUNISM_BONUS;
        }

        float compoundATP;
        if (compoundOut != atp)
        {
            var compoundOutGenerated =
                cache.GetCompoundGeneratedFrom(compound, compoundOut, microbeSpecies, patch.Biome);
            compoundATP = cache.GetCompoundConversionScoreForSpecies(compoundOut, atp, microbeSpecies) *
                compoundOutGenerated;
        }
        else
        {
            compoundATP = cache.GetCompoundGeneratedFrom(compound, atp, microbeSpecies, patch.Biome);
        }

        var energyBalance = cache.GetEnergyBalanceForSpecies(microbeSpecies, patch.Biome);

        // Penalize species that don't produce enough ATP to survive from just the compound generated by the chunk
        score *= MathF.Min(compoundATP / energyBalance.TotalConsumption, 1);

        return score;
    }

    public override LocalizedString GetDescription()
    {
        return new LocalizedString("CHUNK_FOOD_SOURCE", readableName);
    }

    public override float GetEnergy(Patch patch)
    {
        if (!patch.Biome.Chunks.TryGetValue(chunkType, out var chunk))
            throw new ArgumentException("Chunk does not exist in patch");

        if (chunk.Compounds?.TryGetValue(compound.ID, out var compoundAmount) != true)
            throw new ArgumentException("Chunk does not contain compound");

        // This computation nerfs big chunks with a large amount,
        // by adding an "accessibility" component to total energy.
        // Since most cells will rely on bigger chunks by exploiting the venting,
        // this technically makes it a less efficient food source than small chunks, despite a larger amount.
        // We thus account for venting also in the total energy from the source,
        // by adding a volume-to-surface radius exponent ratio (e.g. 2/3 for a sphere).
        // This logic doesn't match with the rest of auto-evo (which doesn't account for accessibility).
        // TODO: extend this approach or find another nerf.
        var ventedEnergy = MathF.Pow(compoundAmount.Amount, Constants.AUTO_EVO_CHUNK_AMOUNT_NERF);
        return ventedEnergy * chunk.Density * Constants.AUTO_EVO_CHUNK_ENERGY_AMOUNT;
    }

    public Compound GetUsedCompoundType()
    {
        return compound.ID;
    }

    public override string ToString()
    {
        var chunkName = Localization.Translate("CHUNK_FOOD_SOURCE").FormatSafe(readableName);

        return $"{Name} ({chunkName})";
    }
}
