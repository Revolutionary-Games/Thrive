/// <summary>
///   Callbacks of the cell body plan editor
/// </summary>
[DeserializedCallbackTarget]
public partial class CellBodyPlanEditorComponent
{
    [DeserializedCallbackAllowed]
    private void OnCellAdded(HexWithData<CellTemplate> hexWithData)
    {
        cellDataDirty = true;

        UpdateReproductionOrderList();
    }

    [DeserializedCallbackAllowed]
    private void OnCellRemoved(HexWithData<CellTemplate> hexWithData)
    {
        cellDataDirty = true;

        UpdateReproductionOrderList();
    }

    [DeserializedCallbackAllowed]
    private void DoCellRemoveAction(CellRemoveActionData data)
    {
        editedMicrobeCells.Remove(data.RemovedHex);
    }

    [DeserializedCallbackAllowed]
    private void UndoCellRemoveAction(CellRemoveActionData data)
    {
        editedMicrobeCells.Add(data.RemovedHex);
    }

    [DeserializedCallbackAllowed]
    private void DoCellPlaceAction(CellPlacementActionData data)
    {
        editedMicrobeCells.Add(data.PlacedHex);
    }

    [DeserializedCallbackAllowed]
    private void UndoCellPlaceAction(CellPlacementActionData data)
    {
        editedMicrobeCells.Remove(data.PlacedHex);
    }

    [DeserializedCallbackAllowed]
    private void DoCellMoveAction(CellMoveActionData data)
    {
        data.MovedHex.Position = data.NewLocation;
        data.MovedHex.Data!.Orientation = data.NewRotation;

        if (editedMicrobeCells.Contains(data.MovedHex))
        {
            UpdateAlreadyPlacedVisuals();

            // TODO: notify auto-evo prediction once that is done
        }
        else
        {
            editedMicrobeCells.Add(data.MovedHex);
        }
    }

    [DeserializedCallbackAllowed]
    private void UndoCellMoveAction(CellMoveActionData data)
    {
        data.MovedHex.Position = data.OldLocation;
        data.MovedHex.Data!.Orientation = data.OldRotation;

        UpdateAlreadyPlacedVisuals();
    }
}
