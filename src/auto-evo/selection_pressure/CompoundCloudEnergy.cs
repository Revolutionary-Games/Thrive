namespace AutoEvo;

using System;
using SharedBase.Archive;

public class CompoundCloudEnergy : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_COMPOUND_CLOUD_ENERGY");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly Compound compound;

    private readonly CompoundDefinition compoundDefinition;

    public CompoundCloudEnergy(Compound compound, float weight) :
        base(weight, [
            new AddOrganelleAnywhere(organelle => organelle.HasChemoreceptorComponent),
            new UpgradeOrganelle(organelle => organelle.HasChemoreceptorComponent,
                new ChemoreceptorUpgrades(compound, null, Constants.CHEMORECEPTOR_RANGE_DEFAULT,
                    Constants.CHEMORECEPTOR_AMOUNT_DEFAULT, SimulationParameters.GetCompound(compound).Colour)),
        ])
    {
        compoundDefinition = SimulationParameters.GetCompound(compound);

        if (!compoundDefinition.IsCloud)
            throw new ArgumentException("Given compound to cloud pressure is not of cloud type");

        this.compound = compound;
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.CompoundCloudEnergy;

    public static CompoundCloudEnergy ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new CompoundCloudEnergy((Compound)reader.ReadInt32(), reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)compound);
        base.WriteToArchive(writer);
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        return 1;
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
        return new LocalizedString("COMPOUND_FOOD_SOURCE_ENERGY",
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
