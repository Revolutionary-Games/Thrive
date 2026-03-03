namespace AutoEvo;

using SharedBase.Archive;

public class PredationEnergy : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    public readonly Species Prey;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_PREDATION_ENERGY");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    public PredationEnergy(Species prey, float weight) :
        base(weight, [])
    {
        Prey = prey;
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.PredationEnergy;

    public static PredationEnergy ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new PredationEnergy(reader.ReadObject<Species>(), reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Prey);
        base.WriteToArchive(writer);
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        return 1;
    }

    public override float GetEnergy(Patch patch)
    {
        if (!patch.SpeciesInPatch.TryGetValue(Prey, out long population) || population <= 0)
            return 0;

        return population * Prey.GetPredationTargetSizeFactor() * Constants.AUTO_EVO_PREDATION_ENERGY_MULTIPLIER;
    }

    public override LocalizedString GetDescription()
    {
        return new LocalizedString("PREDATION_FOOD_SOURCE_ENERGY", Prey.FormattedNameBbCode);
    }

    public override string ToString()
    {
        return $"{Name} ({Prey.FormattedName})";
    }
}
