using SharedBase.Archive;

/// <summary>
///   Customised difficulty setting
/// </summary>
public class CustomDifficulty : IDifficulty
{
    public const ushort SERIALIZATION_VERSION = 3;

    private bool applyGrowthOverride;
    private bool growthLimitOverride;

    private bool limitGrowthRate;

    public float MPMultiplier { get; set; }
    public float AIMutationMultiplier { get; set; }
    public float CompoundDensity { get; set; }
    public float PlayerDeathPopulationPenalty { get; set; }
    public float GlucoseDecay { get; set; }
    public float OsmoregulationMultiplier { get; set; }

    /// <summary>
    ///   The default value is picked here to preserve the old behaviour when loading older saves.
    /// </summary>
    public float PlayerAutoEvoStrength { get; set; } = 0.2f;

    /// <summary>
    ///   This defaults to 0 to keep this off in old saves
    /// </summary>
    public float PlayerSpeciesAIPopulationStrength { get; set; }

    public bool FreeGlucoseCloud { get; set; }

    public ReproductionCompoundHandling ReproductionCompounds { get; set; } =
        ReproductionCompoundHandling.TopUpOnPatchChange;

    public bool SwitchSpeciesOnExtinction { get; set; }

    public bool LimitGrowthRate
    {
        get
        {
            if (applyGrowthOverride)
                return growthLimitOverride;

            return limitGrowthRate;
        }
        set => limitGrowthRate = value;
    }

    public FogOfWarMode FogOfWarMode { get; set; }

    public bool InstantKillProtection { get; set; }

    public bool OrganelleUnlocksEnabled { get; set; }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.CustomDifficulty;
    public bool CanBeReferencedInArchive => false;

    public static CustomDifficulty ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new CustomDifficulty
        {
            MPMultiplier = reader.ReadFloat(),
            AIMutationMultiplier = reader.ReadFloat(),
            CompoundDensity = reader.ReadFloat(),
            PlayerDeathPopulationPenalty = reader.ReadFloat(),
            GlucoseDecay = reader.ReadFloat(),
            OsmoregulationMultiplier = reader.ReadFloat(),
            PlayerAutoEvoStrength = reader.ReadFloat(),
            PlayerSpeciesAIPopulationStrength = reader.ReadFloat(),
            FreeGlucoseCloud = reader.ReadBool(),
            ReproductionCompounds = (ReproductionCompoundHandling)reader.ReadInt32(),
            SwitchSpeciesOnExtinction = reader.ReadBool(),
        };

        if (version > 1)
        {
            instance.FogOfWarMode = (FogOfWarMode)reader.ReadInt32();
            instance.OrganelleUnlocksEnabled = reader.ReadBool();
            instance.limitGrowthRate = reader.ReadBool();
        }

        if (version > 2)
        {
            instance.InstantKillProtection = reader.ReadBool();
        }
        else
        {
            instance.InstantKillProtection = true;
        }

        return instance;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(MPMultiplier);
        writer.Write(AIMutationMultiplier);
        writer.Write(CompoundDensity);
        writer.Write(PlayerDeathPopulationPenalty);
        writer.Write(GlucoseDecay);
        writer.Write(OsmoregulationMultiplier);
        writer.Write(PlayerAutoEvoStrength);
        writer.Write(PlayerSpeciesAIPopulationStrength);
        writer.Write(FreeGlucoseCloud);
        writer.Write((int)ReproductionCompounds);
        writer.Write(SwitchSpeciesOnExtinction);

        // Version 2 fields that were forgotten in the first version (and caused a map reveal bug)
        writer.Write((int)FogOfWarMode);
        writer.Write(OrganelleUnlocksEnabled);
        writer.Write(limitGrowthRate);

        // Version 3 fields that were added after
        writer.Write(InstantKillProtection);
    }

    public void SetGrowthRateLimitCheatOverride(bool newLimitSetting)
    {
        applyGrowthOverride = true;
        growthLimitOverride = newLimitSetting;
    }

    public void ClearGrowthRateLimitOverride()
    {
        applyGrowthOverride = false;
    }
}
