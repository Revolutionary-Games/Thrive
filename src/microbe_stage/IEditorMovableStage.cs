/// <summary>
///   Used to trigger move to editor for supported stages
/// </summary>
public interface IEditorMovableStage
{
    public bool MovingToEditor { get; }
    public bool IsDisposed { get; }

    public void MoveToEditor();
}
