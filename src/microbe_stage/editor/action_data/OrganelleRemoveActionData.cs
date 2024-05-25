using Newtonsoft.Json;

[JSONAlwaysDynamicType]
public class OrganelleRemoveActionData : HexRemoveActionData<OrganelleTemplate, CellType>
{
    /// <summary>
    ///   Used for replacing Cytoplasm. If true this action is free.
    /// </summary>
    public bool GotReplaced;

    [JsonConstructor]
    public OrganelleRemoveActionData(OrganelleTemplate organelle, Hex location, int orientation) : base(organelle,
        location, orientation)
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

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        // Endosymbionts can be deleted for free after placing (not that it is very useful, but it should be free)
        if (other is EndosymbiontPlaceActionData endosymbiontPlaceActionData)
        {
            if (RemovedHex == endosymbiontPlaceActionData.PlacedOrganelle)
            {
                return ActionInterferenceMode.CancelsOut;
            }
        }

        var baseResult = base.GetInterferenceModeWithGuaranteed(other);

        if (baseResult != ActionInterferenceMode.NoInterference)
            return baseResult;

        if (other is OrganelleUpgradeActionData upgradeActionData)
        {
            // This replaces (refunds) the MP for an upgrade done to this organelle
            if (ReferenceEquals(upgradeActionData.UpgradedOrganelle, RemovedHex))
                return ActionInterferenceMode.ReplacesOther;
        }

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CreateDerivedMoveAction(
        HexPlacementActionData<OrganelleTemplate, CellType> data)
    {
        return new OrganelleMoveActionData(data.PlacedHex, Location, data.Location,
            Orientation, data.Orientation);
    }

    protected override CombinableActionData CreateDerivedRemoveAction(
        HexMoveActionData<OrganelleTemplate, CellType> data)
    {
        return new OrganelleRemoveActionData(RemovedHex, data.OldLocation, data.OldRotation)
            { GotReplaced = GotReplaced };
    }
}
