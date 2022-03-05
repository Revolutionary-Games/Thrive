/// <summary>
///   Access to overall editor state for patch map editor components
/// </summary>
public interface IEditorWithPatches : IEditor
{
    public Patch CurrentPatch { get; }

    public void OnCurrentPatchUpdated(Patch patch);
}
