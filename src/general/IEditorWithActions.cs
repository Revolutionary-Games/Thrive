public interface IEditorWithActions : IEditor
{
    public new int MutationPoints { get; set; }

    public void Undo();
    public void Redo();
}
