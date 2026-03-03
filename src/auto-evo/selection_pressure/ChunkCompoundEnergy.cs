namespace AutoEvo;

using System;
using SharedBase.Archive;

public class ChunkCompoundEnergy : SelectionPressure
{
    public const ushort SERIALIZATION_VERSION = 1;

    // Needed for translation extraction
    // ReSharper disable ArrangeObjectCreationWhenTypeEvident
    private static readonly LocalizedString NameString = new LocalizedString("MICHE_CHUNK_ENERGY");

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    private readonly string chunkType;

    private readonly LocalizedString readableName;

    private readonly CompoundDefinition compound;

    public ChunkCompoundEnergy(string chunkType, LocalizedString readableName, Compound compound,
        float weight) : base(weight, [])
    {
        this.compound = SimulationParameters.GetCompound(compound);
        this.chunkType = chunkType;
        this.readableName = readableName;
    }

    public override LocalizedString Name => NameString;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.ChunkCompoundEnergy;

    public static ChunkCompoundEnergy ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new ChunkCompoundEnergy(reader.ReadString() ?? throw new NullArchiveObjectException(),
            reader.ReadObject<LocalizedString>(), (Compound)reader.ReadInt32(), reader.ReadFloat());

        instance.ReadBasePropertiesFromArchive(reader, 1);
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(chunkType);
        writer.WriteObject(readableName);
        writer.Write((int)compound.ID);
        base.WriteToArchive(writer);
    }

    public override float Score(Species species, Patch patch, SimulationCache cache)
    {
        return 1;
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
        var chunkName = Localization.Translate("CHUNK_FOOD_SOURCE_ENERGY").FormatSafe(readableName);

        return $"{Name} ({chunkName})";
    }
}
