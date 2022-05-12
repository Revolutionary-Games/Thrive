using Newtonsoft.Json;

[JSONAlwaysDynamicType]
public abstract class RemoveHexActionData<THex> : EditorCombinableActionData
    where THex : class
{
    public THex AddedHex;
    public Hex Location;
    public int Orientation;

    protected RemoveHexActionData(THex organelle, Hex location, int orientation)
    {
        AddedHex = organelle;
        Location = location;
        Orientation = orientation;
    }

    protected override int CalculateCostInternal()
    {
        return Constants.ORGANELLE_REMOVE_COST;
    }
}
