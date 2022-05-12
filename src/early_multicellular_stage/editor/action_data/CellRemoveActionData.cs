using System;
using Newtonsoft.Json;

[JSONAlwaysDynamicType]
public class CellRemoveActionData : RemoveHexActionData<HexWithData<CellTemplate>>
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



    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        throw new System.NotImplementedException();
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        throw new System.NotImplementedException();
    }


}
