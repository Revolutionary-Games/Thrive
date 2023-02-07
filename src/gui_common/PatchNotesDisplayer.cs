using Godot;

/// <summary>
///   Handles displaying the patch notes from the previous version to the current one
/// </summary>
public class PatchNotesDisplayer : VBoxContainer
{
    [Export]
    public NodePath? PatchNotesPath;

    [Export]
    public NodePath NewVersionsCountLabelPath = null!;

    [Export]
    public NodePath ViewAllButtonPath = null!;

#pragma warning disable CA2213
    private PatchNotesList patchNotes = null!;

    private Label newVersionsCountLabel = null!;

    private Button viewAllButton = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        patchNotes = GetNode<PatchNotesList>(PatchNotesPath);
        newVersionsCountLabel = GetNode<Label>(NewVersionsCountLabelPath);
        viewAllButton = GetNode<Button>(ViewAllButtonPath);

        patchNotes.Visible = false;
        newVersionsCountLabel.Visible = false;
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
            newVersionsCountLabel.Text = TranslationServer.Translate("PATCH_NOTES_LAST_PLAYED_INFO")
                .FormatSafe(lastPlayed);
        }
        else
        {
            newVersionsCountLabel.Text = TranslationServer.Translate("PATCH_NOTES_LAST_PLAYED_INFO_PLURAL")
                .FormatSafe(lastPlayed, newVersions);
        }

        newVersionsCountLabel.Visible = true;

        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PatchNotesPath != null)
            {
                PatchNotesPath.Dispose();
                NewVersionsCountLabelPath.Dispose();
                ViewAllButtonPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnViewAllPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        viewAllButton.Disabled = true;

        patchNotes.ShowAll = true;
        patchNotes.Visible = true;
    }
}
