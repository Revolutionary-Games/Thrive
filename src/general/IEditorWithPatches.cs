public interface IEditorWithPatches : IEditor
{
    public Patch CurrentPatch { get; }

    /// <summary>
    ///   Returns true when the player is allowed to move to the specified patch
    /// </summary>
    /// <returns>True if the patch move requested is valid. False otherwise</returns>
    bool IsPatchMoveValid(Patch? patch);

    void SetPlayerPatch(Patch? patch);
}
