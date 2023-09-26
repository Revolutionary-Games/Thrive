using System;
using Newtonsoft.Json;

[JSONAlwaysDynamicType]
public class CellRemoveActionData : HexRemoveActionData<HexWithData<CellTemplate>, EarlyMulticellularSpecies>
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

    protected override CombinableActionData CreateDerivedMoveAction(
        HexPlacementActionData<HexWithData<CellTemplate>, EarlyMulticellularSpecies> data)
    {
        return new CellMoveActionData(data.PlacedHex, Location, data.Location,
            Orientation, data.Orientation);
    }

    protected override CombinableActionData CreateDerivedRemoveAction(HexMoveActionData<HexWithData<CellTemplate>,
        EarlyMulticellularSpecies> data)
    {
        return new CellRemoveActionData(RemovedHex, data.OldLocation, data.OldRotation);
    }
}
