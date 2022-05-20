using Newtonsoft.Json;

[JSONAlwaysDynamicType]
public class OrganelleRemoveActionData : HexRemoveActionData<OrganelleTemplate>
{
    /// <summary>
    ///   Used for replacing Cytoplasm. If true this action is free.
    /// </summary>
    public bool GotReplaced;

    [JsonConstructor]
    public OrganelleRemoveActionData(OrganelleTemplate organelle, Hex location, int orientation) : base(organelle,
        location,
        orientation)
    {
    }

    public OrganelleRemoveActionData(OrganelleTemplate organelle) : base(organelle, organelle.Position,
        organelle.Orientation)
    {
    }

    protected override int CalculateCostInternal()
    {
        return GotReplaced ? 0 : base.CalculateCostInternal();
    }

    protected override CombinableActionData CreateDerivedMoveAction(HexPlacementActionData<OrganelleTemplate> data)
    {
        return new OrganelleMoveActionData(data.PlacedHex, Location, data.Location,
            Orientation, data.Orientation);
    }

    protected override CombinableActionData CreateDerivedRemoveAction(HexMoveActionData<OrganelleTemplate> data)
    {
        return new OrganelleRemoveActionData(RemovedHex, data.OldLocation, data.OldRotation);
    }
}
