/// <summary>
///   Editor with data to fill in for the editor report screen
/// </summary>
public interface IEditorReportData : IEditor
{
    public Patch CurrentPatch { get; }

    public Patch? SelectedPatch { get; }

    public void SendAutoEvoResultsToReportComponent();
}
