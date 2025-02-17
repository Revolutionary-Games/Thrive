public interface IEditorWithActions : IEditor
{
    public new double MutationPoints { get; set; }

    public void Undo();
    public void Redo();
}
