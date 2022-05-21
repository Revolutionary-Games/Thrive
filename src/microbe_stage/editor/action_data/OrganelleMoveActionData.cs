public class OrganelleMoveActionData : HexMoveActionData<OrganelleTemplate>
{
    public OrganelleMoveActionData(OrganelleTemplate organelle, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation) : base(organelle, oldLocation, newLocation, oldRotation, newRotation)
    {
    }

    protected override CombinableActionData CreateDerivedMoveAction(OrganelleTemplate hex, Hex oldLocation,
        Hex newLocation, int oldRotation, int newRotation)
    {
        return new OrganelleMoveActionData(hex, oldLocation, newLocation, oldRotation, newRotation);
    }

    protected override CombinableActionData CreateDerivedPlacementAction(HexPlacementActionData<OrganelleTemplate> data)
    {
        var placementActionData = (OrganellePlacementActionData)data;

        return new OrganellePlacementActionData(placementActionData.PlacedHex, NewLocation, NewRotation)
        {
            ReplacedCytoplasm = placementActionData.ReplacedCytoplasm,
        };
    }
}
