using System;
using Newtonsoft.Json;

[JSONAlwaysDynamicType]
public class CellPlacementActionData : HexPlacementActionData<HexWithData<CellTemplate>>
{
    [JsonConstructor]
    public CellPlacementActionData(HexWithData<CellTemplate> hex, Hex location, int orientation) : base(hex, location,
        orientation)
    {
    }

    public CellPlacementActionData(HexWithData<CellTemplate> hex) : base(hex, hex.Position,
        hex.Data?.Orientation ?? throw new ArgumentException("Hex with no data"))
    {
    }

    protected override int CalculateCostInternal()
    {
        return PlacedHex.Data?.CellType.MPCost ?? throw new InvalidOperationException("Hex with no data");
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        throw new System.NotImplementedException();
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        throw new System.NotImplementedException();
    }
}
