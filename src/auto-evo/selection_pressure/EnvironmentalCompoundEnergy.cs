namespace AutoEvo;

using System;
using SharedBase.Archive;

public class EnvironmentalCompoundEnergy : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_ENVIRONMENTAL_COMPOUND_ENERGY");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly CompoundDefinition atp = SimulationParameters.GetCompound(Compound.ATP);

    private readonly CompoundDefinition compound;

    private readonly float energyMultiplier;

    public EnvironmentalCompoundEnergy(Compound compound, float energyMultiplier,
        float weight) :
        base(weight, [
            AddOrganelleAnywhere.ThatUseCompound(compound),
        ])
    {
        this.compound = SimulationParameters.GetCompound(compound);

        if (this.compound.IsCloud)
            throw new ArgumentException("Given compound to environmental pressure is a cloud type");

        this.energyMultiplier = energyMultiplier;
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.EnvironmentalCompoundEnergy;

    public static EnvironmentalCompoundEnergy ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new EnvironmentalCompoundEnergy((Compound)reader.ReadInt32(), reader.ReadFloat(),
            reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)compound.ID);
        writer.Write(energyMultiplier);
        base.WriteToArchive(writer);
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        return 1;
    }

    public override float GetEnergy(Patch patch)
    {
        return patch.Biome.AverageCompounds[compound.ID].Ambient * energyMultiplier;
    }

    public override LocalizedString GetDescription()
    {
        return new LocalizedString("DISSOLVED_COMPOUND_FOOD_SOURCE_ENERGY",
            new LocalizedString(compound.GetUntranslatedName()));
    }

    public Compound GetUsedCompoundType()
    {
        return compound.ID;
    }

    public override string ToString()
    {
        return $"{Name} ({compound.Name})";
    }
}
