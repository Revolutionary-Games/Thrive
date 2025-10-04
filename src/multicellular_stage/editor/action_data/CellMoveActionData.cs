public class CellMoveActionData : HexMoveActionData<HexWithData<CellTemplate>, MulticellularSpecies>
{
    public CellMoveActionData(HexWithData<CellTemplate> organelle, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation) : base(organelle, oldLocation, newLocation, oldRotation, newRotation)
    {
    }
}
