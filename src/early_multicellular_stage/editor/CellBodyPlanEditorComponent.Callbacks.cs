/// <summary>
///   Callbacks of the cell body plan editor
/// </summary>
[DeserializedCallbackTarget]
public partial class CellBodyPlanEditorComponent
{
    [DeserializedCallbackAllowed]
    private void OnCellAdded(HexWithData<CellTemplate> hexWithData)
    {
        organelleDataDirty = true;
    }

    [DeserializedCallbackAllowed]
    private void OnCellRemoved(HexWithData<CellTemplate> hexWithData)
    {
        organelleDataDirty = true;
    }

    [DeserializedCallbackAllowed]
    private void DoCellRemoveAction(CellRemoveActionData data)
    {
        editedMicrobeCells.Remove(data.AddedHex);
    }

    [DeserializedCallbackAllowed]
    private void UndoCellRemoveAction(CellRemoveActionData data)
    {
        editedMicrobeCells.Add(data.AddedHex);
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

}
