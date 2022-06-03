/// <summary>
///   Callbacks for the metaball body editor
/// </summary>
[DeserializedCallbackTarget]
public partial class MetaballBodyEditorComponent
{
    [DeserializedCallbackAllowed]
    private void OnMetaballAdded(MulticellularMetaball metaball)
    {
        metaballDisplayDataDirty = true;
    }

    [DeserializedCallbackAllowed]
    private void OnMetaballRemoved(MulticellularMetaball metaball)
    {
        metaballDisplayDataDirty = true;
    }

    [DeserializedCallbackAllowed]
    private void DoCellRemoveAction(MetaballRemoveActionData<MulticellularMetaball> data)
    {
        editedMetaballs.Remove(data.RemovedMetaball);
    }

    [DeserializedCallbackAllowed]
    private void UndoCellRemoveAction(MetaballRemoveActionData<MulticellularMetaball> data)
    {
        editedMetaballs.Add(data.RemovedMetaball);
    }

    [DeserializedCallbackAllowed]
    private void DoCellPlaceAction(MetaballPlacementActionData<MulticellularMetaball> data)
    {
        editedMetaballs.Add(data.PlacedMetaball);
    }

    [DeserializedCallbackAllowed]
    private void UndoCellPlaceAction(MetaballPlacementActionData<MulticellularMetaball> data)
    {
        editedMetaballs.Remove(data.PlacedMetaball);
    }

    [DeserializedCallbackAllowed]
    private void DoCellMoveAction(MetaballMoveActionData<MulticellularMetaball> data)
    {
        data.MovedMetaball.Position = data.NewPosition;
        data.MovedMetaball.Parent = data.NewParent;

        if (editedMetaballs.Contains(data.MovedMetaball))
        {
            UpdateAlreadyPlacedVisuals();

            // TODO: notify auto-evo prediction once that is done
        }
        else
        {
            editedMetaballs.Add(data.MovedMetaball);
        }
    }

    [DeserializedCallbackAllowed]
    private void UndoCellMoveAction(MetaballMoveActionData<MulticellularMetaball> data)
    {
        data.MovedMetaball.Position = data.OldPosition;
        data.MovedMetaball.Parent = data.OldParent;

        UpdateAlreadyPlacedVisuals();
    }
}
