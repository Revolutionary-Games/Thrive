namespace AutoEvo;

using System;
using Godot;
using SharedBase.Archive;

public class MetabolicStabilityPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_METABOLIC_STABILITY_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public MetabolicStabilityPressure(float weight) : base(weight, [
        RemoveOrganelle.ThatUseCompound(Compound.ATP),
        new UpgradeOrganelle(organelle => organelle.HasMovementComponent, new FlagellumUpgrades(-1.0f)),
        new ChangeBehaviorScore(ChangeBehaviorScore.BehaviorAttribute.Activity, -150.0f),
    ])
    {
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.MetabolicStabilityPressure;

    public static MetabolicStabilityPressure ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new MetabolicStabilityPressure(reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (cache.GetSpeedForSpecies(species) == 0)
        {
            return 0.0f;
        }

        if (species is MicrobeSpecies)
        {
            var energyBalance = cache.GetEnergyBalanceForSpecies(species, patch.Biome);

            if (energyBalance.FinalBalance > 0)
            {
                return 1.0f;
            }

            if (energyBalance.FinalBalanceStationary > 0)
            {
                // Only punish non-sessile species for not being able to move continuously
                return 1.0f - species.Behaviour.Activity / Constants.MAX_SPECIES_ACTIVITY;
            }

            return 0.0f;
        }

        if (species is MulticellularSpecies multicellularSpecies)
        {
            // for metabolic stability in Multicellular species, we care for individual cells instead of the whole
            // species, because ATP is per-cell.
            // We take cell types instead of individual cells because it's faster, matches what the player gets warnings
            // for, and makes it easier to place new cells in hexes where they might have less adjacency.
            foreach (var cellType in multicellularSpecies.CellTypes)
            {
                var energyBalance = cache.GetEnergyBalanceForCellType(cellType, multicellularSpecies, patch.Biome);

                if (energyBalance.FinalBalance < 0)
                {
                    if (energyBalance.FinalBalanceStationary > 0)
                    {
                        // Only punish non-sessile species for not being able to move continuously
                        return 1.0f - species.Behaviour.Activity / Constants.MAX_SPECIES_ACTIVITY;
                    }

                    return 0.0f;
                }
            }

            return 1.0f;
        }

        GD.Print(species.ReadableName);
        throw new ArgumentException("Wrong type of Species passed to Microbe/Multicellular Species miche tree");
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }
}
