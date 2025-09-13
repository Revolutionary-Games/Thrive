using System;
using Newtonsoft.Json;

[JSONAlwaysDynamicType]
public class CellRemoveActionData : HexRemoveActionData<HexWithData<CellTemplate>, MulticellularSpecies>
{
    [JsonConstructor]
    public CellRemoveActionData(HexWithData<CellTemplate> hex, Hex location, int orientation) : base(hex, location,
        orientation)
    {
    }

    public CellRemoveActionData(HexWithData<CellTemplate> hex) : base(hex, hex.Position,
        hex.Data?.Orientation ?? throw new ArgumentException("Hex with no data"))
    {
    }
}
