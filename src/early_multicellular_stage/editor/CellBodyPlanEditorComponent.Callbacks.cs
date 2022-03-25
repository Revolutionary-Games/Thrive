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
}
