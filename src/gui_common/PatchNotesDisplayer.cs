using System.Linq;
using Godot;

/// <summary>
///   Handles displaying the patch notes from the previous version to the current one
/// </summary>
public partial class PatchNotesDisplayer : VBoxContainer
{
    [Export]
    public bool InsideDialogStyle;

    [Export]
    public NodePath? PatchNotesPath;

    [Export]
    public NodePath TitlePath = null!;

    [Export]
    public NodePath NewVersionsCountLabelPath = null!;

    [Export]
    public NodePath ViewAllButtonPath = null!;

    [Export]
    public NodePath ViewAllButtonOutsideScrollPath = null!;

#pragma warning disable CA2213
    private PatchNotesList patchNotes = null!;

    private Label title = null!;
    private Label newVersionsCountLabel = null!;

    private Button viewAllButton = null!;
    private Button viewAllButtonOutsideScroll = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        patchNotes = GetNode<PatchNotesList>(PatchNotesPath);
        title = GetNode<Label>(TitlePath);
        newVersionsCountLabel = GetNode<Label>(NewVersionsCountLabelPath);
        viewAllButton = GetNode<Button>(ViewAllButtonPath);
        viewAllButtonOutsideScroll = GetNode<Button>(ViewAllButtonOutsideScrollPath);

        patchNotes.Visible = false;
        newVersionsCountLabel.Visible = false;

        if (InsideDialogStyle)
        {
            title.Visible = false;
            patchNotes.StyleWithBackground = false;

            viewAllButton.Visible = false;
            viewAllButtonOutsideScroll.Visible = true;
        }
    }

    public bool ShowIfNewPatchNotesExist()
    {
        var lastPlayed = LastPlayedVersion.LastPlayed;
        if (lastPlayed == null)
        {
            // If no known last played version, this is the firs time Thrive is played and there's no need to show
            // patch notes
            return false;
        }

        var currentVersion = Constants.Version;

        // If we aren't playing a newer version then also skip showing
        if (VersionUtils.Compare(currentVersion, lastPlayed) <= 0)
            return false;

        // Find the oldest patch notes we should show, and count how many versions there's been
        int newVersions = 0;
        string? oldestToShow = null;

        foreach (var entry in SimulationParameters.Instance.GetPatchNotes())
        {
            // Skip older than last played
            if (VersionUtils.Compare(lastPlayed, entry.Key) >= 0)
                continue;

            // Skip versions that might for some reason be higher than the current one
            if (VersionUtils.Compare(currentVersion, entry.Key) < 0)
                continue;

            ++newVersions;

            if (oldestToShow == null || VersionUtils.Compare(entry.Key, oldestToShow) < 0)
            {
                oldestToShow = entry.Key;
            }
        }

        if (newVersions < 1)
        {
            // This should only happen in development builds as patch notes for the upcoming release is not added yet
            GD.Print("No newer patch notes found, even though there should be some");
            return false;
        }

        // Show the right notes
        patchNotes.ShowAll = false;
        patchNotes.FilterNewestVersion = currentVersion;
        patchNotes.FilterOldestVersion = oldestToShow;

        // And then make it visible to refresh its data
        patchNotes.Visible = true;

        // Setup text to show how many new versions there are
        if (newVersions == 1)
        {
            newVersionsCountLabel.Text = Localization.Translate("PATCH_NOTES_LAST_PLAYED_INFO")
                .FormatSafe(lastPlayed);
        }
        else
        {
            newVersionsCountLabel.Text = Localization.Translate("PATCH_NOTES_LAST_PLAYED_INFO_PLURAL")
                .FormatSafe(lastPlayed, newVersions);
        }

        newVersionsCountLabel.Visible = true;

        return true;
    }

    public void ShowLatest()
    {
        var latestVersion = SimulationParameters.Instance.GetPatchNotes().Last();

        patchNotes.ShowAll = false;
        patchNotes.FilterNewestVersion = latestVersion.Key;
        patchNotes.FilterOldestVersion = latestVersion.Key;
        patchNotes.Visible = true;

        newVersionsCountLabel.Visible = false;

        viewAllButton.Disabled = false;
        viewAllButtonOutsideScroll.Disabled = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PatchNotesPath != null)
            {
                PatchNotesPath.Dispose();
                TitlePath.Dispose();
                NewVersionsCountLabelPath.Dispose();
                ViewAllButtonPath.Dispose();
                ViewAllButtonOutsideScrollPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnViewAllPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        viewAllButton.Disabled = true;
        viewAllButtonOutsideScroll.Disabled = true;

        patchNotes.ShowAll = true;
        patchNotes.Visible = true;
    }
}
