namespace AutoEvo;

using System;
using Newtonsoft.Json;
using SharedBase.Archive;

public class CompoundConversionEfficiencyPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    public readonly CompoundDefinition FromCompound;

    public readonly CompoundDefinition ToCompound;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_COMPOUND_EFFICIENCY_PRESSURE");

    private readonly CompoundDefinition atp = SimulationParameters.GetCompound(Compound.ATP);

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly bool usedForSurvival;

    public CompoundConversionEfficiencyPressure(Compound compound, Compound outCompound,
        bool usedForSurvival, float weight) :
        base(weight, [
            RemoveOrganelle.ThatCreateCompound(outCompound),
            AddOrganelleAnywhere.ThatConvertBetweenCompounds(compound, outCompound),
        ])
    {
        FromCompound = SimulationParameters.GetCompound(compound);
        ToCompound = SimulationParameters.GetCompound(outCompound);
        this.usedForSurvival = usedForSurvival;
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.CompoundConversionEfficiencyPressure;

    public static CompoundConversionEfficiencyPressure ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new CompoundConversionEfficiencyPressure((Compound)reader.ReadInt32(),
            (Compound)reader.ReadInt32(), reader.ReadBool(), reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write((int)FromCompound.ID);
        writer.Write((int)ToCompound.ID);
        writer.Write(usedForSurvival);
        base.WriteToArchive(writer);
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species is not MicrobeSpecies microbeSpecies)
            return 0;

        return cache.GetCompoundConversionScoreForSpecies(FromCompound, ToCompound, microbeSpecies, patch.Biome);
    }

    public override float GetEnergy(Patch patch)
    {
        return 0;
    }

    public override string ToString()
    {
        return $"{Name} ({FromCompound.Name})";
    }
}
