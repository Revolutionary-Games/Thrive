namespace AutoEvo;

using SharedBase.Archive;

public class EndosymbiosisPressure : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    public readonly Species Endosymbiont;

    public readonly Species Host;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString =
        new LocalizedString("MICHE_ENDOSYMBIOSIS_SELECTION_PRESSURE");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public EndosymbiosisPressure(Species endosymbiont, Species host, float weight) : base(weight, [
    ])
    {
        Endosymbiont = endosymbiont;
        Host = host;
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.EndosymbiosisPressure;

    public static EndosymbiosisPressure ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new EndosymbiosisPressure(reader.ReadObject<Species>(), reader.ReadObject<Species>(),
            reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Endosymbiont);
        writer.WriteObject(Host);
        base.WriteToArchive(writer);
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        if (species.ID == Endosymbiont.ID)
        {
            return 1.0f;
        }

        return 0;
    }

    public override float GetEnergy(Patch patch)
    {
        if (!patch.SpeciesInPatch.TryGetValue(Host, out long population) || population <= 0)
            return 0;

        return population * Endosymbiont.GetPredationTargetSizeFactor();
    }

    public override string ToString()
    {
        return $"{Name} ({Endosymbiont.FormattedName})";
    }
}
