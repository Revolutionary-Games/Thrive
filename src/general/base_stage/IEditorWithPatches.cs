/// <summary>
///   Access to overall editor state for patch map editor components
/// </summary>
public interface IEditorWithPatches : IEditor
{
    /// <summary>
    ///   Current patch of the player (either where they were before the editor or where they want to move to)
    /// </summary>
    public Patch CurrentPatch { get; }

    /// <summary>
    ///   The target patch to move to after editing. Null if no move is intended.
    /// </summary>
    public Patch? TargetPatch { get; }

    public void OnCurrentPatchUpdated(Patch patch);
}
