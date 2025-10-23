using System.Collections.Generic;
using SharedBase.Archive;
using Xoshiro.PRNG64;

/// <summary>
///   Makes sure nitrogen is between a defined safe limit and attempts to correct things if not (as pure processes
///   don't result in nitrogen balance)
/// </summary>
public class NitrogenControlEffect : IWorldEffect
{
    public const ushort SERIALIZATION_VERSION = 1;

    // ReSharper disable once CollectionNeverUpdated.Local
    /// <summary>
    ///   This doesn't add any clouds with sizes, so this is just a permanently empty dictionary
    /// </summary>
    private readonly Dictionary<Compound, float> cloudSizesDummy = new();

    private readonly XoShiRo256starstar random = new();

    private readonly GameWorld targetWorld;

    public NitrogenControlEffect(GameWorld targetWorld)
    {
        this.targetWorld = targetWorld;
    }

    private NitrogenControlEffect(GameWorld targetWorld, XoShiRo256starstar random)
    {
        this.targetWorld = targetWorld;
        this.random = random;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.NitrogenControlEffect;
    public bool CanBeReferencedInArchive => false;

    public static NitrogenControlEffect ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new NitrogenControlEffect(reader.ReadObject<GameWorld>(), reader.ReadObject<XoShiRo256starstar>());
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(targetWorld);
        writer.WriteAnyRegisteredValueAsObject(random);
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        HandleNitrogenLevels();
    }

    private void HandleNitrogenLevels()
    {
        // TODO: having like a world specific configuration for the limits would be pretty nice
        float maxLevel = Constants.MAX_NITROGEN_LEVEL;
        float minLevel = Constants.SOFT_MIN_NITROGEN_LEVEL;

        var nitrogenModification = new Dictionary<Compound, float>();

        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            // Add the min level if missing entirely (shouldn't happen unless the biomes.json file is wrong)
            if (!patch.Biome.TryGetCompound(Compound.Nitrogen, CompoundAmountType.Biome, out var amount))
            {
                nitrogenModification[Compound.Nitrogen] = minLevel;

                patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, nitrogenModification, cloudSizesDummy);
                continue;
            }

            // Adjust nitrogen amount if it is outside the allowed limits
            if (amount.Ambient > maxLevel)
            {
                var excess = amount.Ambient - maxLevel;

                // Lower a bit below the ceiling so that it is not as easy to tell what the ceiling is
                nitrogenModification[Compound.Nitrogen] = -excess - random.NextFloat() * 0.07f;
            }
            else if (amount.Ambient < minLevel)
            {
                var halfAmount = (minLevel - amount.Ambient) * 0.5f;

                // Add a bit of randomness to not look like a "clipped" result
                nitrogenModification[Compound.Nitrogen] = halfAmount + halfAmount * random.NextFloat();
            }
            else
            {
                continue;
            }

            patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, nitrogenModification, cloudSizesDummy);
        }
    }
}
