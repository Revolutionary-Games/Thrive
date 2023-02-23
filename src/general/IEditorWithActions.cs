public interface IEditorWithActions : IEditor
{
    public new float MutationPoints { get; set; }

    public void Undo();
    public void Redo();
}
