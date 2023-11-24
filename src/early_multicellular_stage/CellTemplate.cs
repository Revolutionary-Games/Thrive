using System;
using Godot;
using Newtonsoft.Json;

[JsonObject(IsReference = true)]
public class CellTemplate : IPositionedCell, ICloneable, IActionHex
{
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

    public void RepositionToOrigin()
    {
        CellType.RepositionToOrigin();
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
        CellPropertiesHelpers.SetupWorldEntities(this, worldSimulation);
    }

    public bool StateHasStabilized(IWorldSimulation worldSimulation)
    {
        return MicrobeSpecies.StateHasStabilizedImpl(worldSimulation);
    }

    public float CalculatePhotographDistance(IWorldSimulation worldSimulation)
    {
        return CellPropertiesHelpers.CalculatePhotographDistance(this, worldSimulation);
    }

    public object Clone()
    {
        return new CellTemplate(CellType)
        {
            Position = Position,
            Orientation = Orientation,
        };
    }
}
