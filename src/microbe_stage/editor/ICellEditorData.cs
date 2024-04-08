/// <summary>
///   Data needed by the cell editor to function / apply the modifications
/// </summary>
public interface ICellEditorData : IHexEditor, IEditorWithPatches, IEditorWithActions
{
    public ICellDefinition? EditedCellProperties { get; }
}
