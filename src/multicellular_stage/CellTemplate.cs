using System;
using Godot;
using Newtonsoft.Json;
using SharedBase.Archive;

[JsonObject(IsReference = true)]
public class CellTemplate : IPositionedCell, ICloneable, IActionHex, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    private int orientation;

    [JsonConstructor]
    public CellTemplate(CellType cellType, Hex position, int orientation)
    {
        CellType = cellType;
        Position = position;
        Orientation = orientation;
    }

    public CellTemplate(CellType cellType)
    {
        CellType = cellType;
    }

    public Hex Position { get; set; }

    public int Orientation
    {
        get => orientation;

        // We normalize rotations here as it isn't normalized later for cell templates
        set => orientation = value % 6;
    }

    [JsonProperty]
    public CellType CellType { get; private set; }

    [JsonIgnore]
    public MembraneType MembraneType { get => CellType.MembraneType; set => CellType.MembraneType = value; }

    [JsonIgnore]
    public float MembraneRigidity { get => CellType.MembraneRigidity; set => CellType.MembraneRigidity = value; }

    [JsonIgnore]
    public Color Colour { get => CellType.Colour; set => CellType.Colour = value; }

    [JsonIgnore]
    public bool IsBacteria { get => CellType.IsBacteria; set => CellType.IsBacteria = value; }

    [JsonIgnore]
    public float BaseRotationSpeed { get => CellType.BaseRotationSpeed; set => CellType.BaseRotationSpeed = value; }

    [JsonIgnore]
    public bool CanEngulf => CellType.CanEngulf;

    [JsonIgnore]
    public string FormattedName => CellType.TypeName;

    [JsonIgnore]
    public OrganelleLayout<OrganelleTemplate> Organelles => CellType.Organelles;

    [JsonIgnore]
    public ISimulationPhotographable.SimulationType SimulationToPhotograph =>
        ISimulationPhotographable.SimulationType.MicrobeGraphics;

    [JsonIgnore]
    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    [JsonIgnore]
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.CellTemplate;

    [JsonIgnore]
    public bool CanBeReferencedInArchive => true;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.CellTemplate)
            throw new NotSupportedException();

        writer.WriteObject((CellTemplate)obj);
    }

    public static CellTemplate ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new CellTemplate(reader.ReadObject<CellType>(), reader.ReadHex(), reader.ReadInt32());
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(CellType);
        writer.Write(Position);
        writer.Write(Orientation);
    }

    public bool RepositionToOrigin()
    {
        return CellType.RepositionToOrigin();
    }

    public void UpdateNameIfValid(string newName)
    {
        CellType.UpdateNameIfValid(newName);
    }

    public bool MatchesDefinition(IActionHex other)
    {
        return CellType == ((CellTemplate)other).CellType;
    }

    public void SetupWorldEntities(IWorldSimulation worldSimulation)
    {
        GeneralCellPropertiesHelpers.SetupWorldEntities(this, worldSimulation);
    }

    public bool StateHasStabilized(IWorldSimulation worldSimulation)
    {
        return MicrobeSpecies.StateHasStabilizedImpl(worldSimulation);
    }

    public Vector3 CalculatePhotographDistance(IWorldSimulation worldSimulation)
    {
        return GeneralCellPropertiesHelpers.CalculatePhotographDistance(worldSimulation);
    }

    public object Clone()
    {
        return new CellTemplate(CellType)
        {
            Position = Position,
            Orientation = Orientation,
        };
    }

    public ulong GetVisualHashCode()
    {
        return CellType.GetVisualHashCode() ^ (ulong)Orientation * 347 ^ (ulong)Position.GetHashCode() * 317;
    }
}
