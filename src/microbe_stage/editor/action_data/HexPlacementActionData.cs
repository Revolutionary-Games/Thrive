[JSONAlwaysDynamicType]
public abstract class HexPlacementActionData<THex> : EditorCombinableActionData
    where THex : class
{
    public THex PlacedHex;
    public Hex Location;
    public int Orientation;

    protected HexPlacementActionData(THex hex, Hex location, int orientation)
    {
        PlacedHex = hex;
        Location = location;
        Orientation = orientation;
    }
}
