public class CellMoveActionData : HexMoveActionData<HexWithData<CellTemplate>, EarlyMulticellularSpecies>
{
    public CellMoveActionData(HexWithData<CellTemplate> organelle, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation) : base(organelle, oldLocation, newLocation, oldRotation, newRotation)
    {
    }

    protected override CombinableActionData CreateDerivedMoveAction(HexWithData<CellTemplate> hex, Hex oldLocation,
        Hex newLocation, int oldRotation, int newRotation)
    {
        return new CellMoveActionData(hex, oldLocation, newLocation, oldRotation, newRotation);
    }

    protected override CombinableActionData CreateDerivedPlacementAction(
        HexPlacementActionData<HexWithData<CellTemplate>, EarlyMulticellularSpecies> data)
    {
        var placementActionData = (CellPlacementActionData)data;

        return new CellPlacementActionData(placementActionData.PlacedHex, NewLocation, NewRotation);
    }
}
