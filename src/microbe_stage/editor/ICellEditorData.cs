/// <summary>
///   Data needed by the cell editor to function / apply the modifications
/// </summary>
public interface ICellEditorData : IHexEditor, IEditorWithPatches, IEditorWithActions
{
    public ICellProperties? EditedCellProperties { get; }

    public bool OrganellePlacedThisSession(OrganelleTemplate organelle);
}
