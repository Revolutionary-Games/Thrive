public interface IEditorWithActions : IEditor
{
    public new int MutationPoints { get; set; }

    /// <summary>
    ///   Changes the number of mutation points left. Should only be called by editor actions
    /// </summary>
    public void ChangeMutationPoints(int change);

    public void Undo();
    public void Redo();
}
