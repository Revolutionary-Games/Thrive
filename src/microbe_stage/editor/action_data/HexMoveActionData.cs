public abstract class HexMoveActionData<THex> : EditorCombinableActionData
    where THex : class
{
    public THex MovedHex;
    public Hex OldLocation;
    public Hex NewLocation;
    public int OldRotation;
    public int NewRotation;

    protected HexMoveActionData(THex hex, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation)
    {
        MovedHex = hex;
        OldLocation = oldLocation;
        NewLocation = newLocation;
        OldRotation = oldRotation;
        NewRotation = newRotation;
    }

    protected override int CalculateCostInternal()
    {
        if (OldLocation == NewLocation && OldRotation == NewRotation)
            return 0;

        return Constants.ORGANELLE_MOVE_COST;
    }
}
