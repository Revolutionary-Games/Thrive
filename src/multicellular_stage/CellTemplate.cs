using System;
using Godot;
using SharedBase.Archive;

public class CellTemplate : IPositionedCell, ICloneable, IReadOnlyHexWithData<IReadOnlyCellTemplate>,
    IPlayerReadableName, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    private CellType modifiableCellType;

    public CellTemplate(CellType cellType, Hex position, int orientation)
    {
        modifiableCellType = cellType;
        Position = position;
        Orientation = orientation;
    }

    public Hex Position { get; set; }

    public int Orientation
    {
        get;

        // We normalize rotations here as it isn't normalized later for cell templates
        set => field = value % 6;
    }

    public string ReadableName => modifiableCellType.FormattedName;

    public string ReadableExactIdentifier => Localization.Translate("ITEM_AT_2D_COORDINATES")
        .FormatSafe(ReadableName, Position.Q, Position.R);

    public virtual CellType ModifiableCellType
    {
        get => modifiableCellType;
        protected set => modifiableCellType = value;
    }

    public virtual IReadOnlyCellTypeDefinition CellType => ModifiableCellType;

    public MembraneType MembraneType
    {
        get => ModifiableCellType.MembraneType;
        set => ModifiableCellType.MembraneType = value;
    }

    public float MembraneRigidity
    {
        get => ModifiableCellType.MembraneRigidity;
        set => ModifiableCellType.MembraneRigidity = value;
    }

    public Color Colour { get => ModifiableCellType.Colour; set => ModifiableCellType.Colour = value; }

    public bool IsBacteria { get => ModifiableCellType.IsBacteria; set => ModifiableCellType.IsBacteria = value; }

    public float BaseRotationSpeed
    {
        get => ModifiableCellType.BaseRotationSpeed;
        set => ModifiableCellType.BaseRotationSpeed = value;
    }

    public bool CanEngulf => ModifiableCellType.CanEngulf;

    public string FormattedName => ModifiableCellType.CellTypeName;

    public IReadOnlyOrganelleLayout<IReadOnlyOrganelleTemplate> Organelles => ModifiableCellType.Organelles;
    public OrganelleLayout<OrganelleTemplate> ModifiableOrganelles => ModifiableCellType.ModifiableOrganelles;

    public ISimulationPhotographable.SimulationType SimulationToPhotograph =>
        ISimulationPhotographable.SimulationType.MicrobeGraphics;

    // Readonly interface compatibility
    public IReadOnlyCellTemplate Data => this;

    // Saving
    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.CellTemplate;

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
        writer.WriteObject(ModifiableCellType);
        writer.Write(Position);
        writer.Write(Orientation);
    }

    public bool RepositionToOrigin()
    {
        return ModifiableCellType.RepositionToOrigin();
    }

    public void UpdateNameIfValid(string newName)
    {
        ModifiableCellType.UpdateNameIfValid(newName);
    }

    public bool MatchesDefinition(IActionHex other)
    {
        return ModifiableCellType == ((CellTemplate)other).ModifiableCellType;
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
        return new CellTemplate(ModifiableCellType, Position, Orientation);
    }

    public ulong GetVisualHashCode()
    {
        return ModifiableCellType.GetVisualHashCode() ^ (ulong)Orientation * 347 ^ (ulong)Position.GetHashCode() * 317;
    }

    public override string ToString()
    {
        return $"Cell ({CellType.CellTypeName}) at {Position}";
    }
}
