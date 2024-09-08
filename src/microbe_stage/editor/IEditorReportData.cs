/// <summary>
///   Editor with data to fill in for the editor report screen
/// </summary>
public interface IEditorReportData : IEditor
{
    /// <summary>
    ///   Returns the patch the player is currently in
    /// </summary>
    public Patch CurrentPatch { get; }

    /// <summary>
    ///   Returns the patch the player wants to move after editing. If the player doesn't want to move, returns
    ///   the same as <see cref="CurrentPatch"/>
    /// </summary>
    public Patch? TargetPatch { get; }

    /// <summary>
    ///   Returns the patch the player selected in the patch map
    /// </summary>
    public Patch? SelectedPatch { get; }

    public void SendAutoEvoResultsToReportComponent();
}
