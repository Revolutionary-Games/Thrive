/// <summary>
///   Editor with data to fill in for the editor report screen
/// </summary>
public interface IEditorReportData : IEditor
{
    /// <summary>
    ///   Returns the patch the player is currently in. If the player wants to move, returns the same
    ///   as <see cref="TargetPatch"/>
    /// </summary>
    public Patch CurrentPatch { get; }

    /// <summary>
    ///   Returns the patch the player wants to move after editing
    /// </summary>
    public Patch? TargetPatch { get; }

    /// <summary>
    ///   Returns the patch the player selected in the patch map (for displaying in the GUI)
    /// </summary>
    public Patch? SelectedPatch { get; }

    public void SendAutoEvoResultsToReportComponent();
}
