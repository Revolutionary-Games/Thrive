using System;
using Newtonsoft.Json;

public class CellTemplate : IPositionedCell, ICloneable
{
    private int orientation;

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
    public OrganelleLayout<OrganelleTemplate> Organelles => CellType.Organelles;

    public object Clone()
    {
        return new CellTemplate(CellType)
        {
            Position = Position,
            Orientation = Orientation,
        };
    }
}
