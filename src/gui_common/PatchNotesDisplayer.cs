using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Handles displaying the patch notes from the previous version to the current one
/// </summary>
public partial class PatchNotesDisplayer : VBoxContainer
{
    [Export]
    public bool InsideDialogStyle;

    private const int VersionsPerPage = 5;

    private readonly List<string> allVersions = [];

#pragma warning disable CA2213
    [Export]
    private PatchNotesList patchNotes = null!;

    [Export]
    private Label title = null!;

    [Export]
    private Label newVersionsCountLabel = null!;

    [Export]
    private Label recentBoundaryLabel = null!;

    [Export]
    private ScrollContainer scrollContainer = null!;

    [Export]
    private Button newerButton = null!;

    [Export]
    private Button olderButton = null!;

#pragma warning restore CA2213

    private int currentVersionIndex;

    /// <summary>
    ///   Count of versions newer than the player's last-played version. Acts as a hard page boundary in main-menu
    ///   mode so the first page only shows the recent set (capped by <see cref="VersionsPerPage"/>). Zero when
    ///   not in main-menu mode (e.g. options-menu "show latest").
    /// </summary>
    private int recentVersionsCount;

    public override void _Ready()
    {
        if (InsideDialogStyle)
        {
            title.Visible = false;
            patchNotes.StyleWithBackground = false;
        }

        newVersionsCountLabel.Visible = false;
        recentBoundaryLabel.Visible = false;

        RebuildVersionList();
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

        // Count how many versions have been released since the player last played
        int newVersions = 0;

        foreach (var entry in SimulationParameters.Instance.GetPatchNotes())
        {
            // Skip older than last played
            if (VersionUtils.Compare(lastPlayed, entry.Key) >= 0)
                continue;

            // Skip versions that might for some reason be higher than the current one
            if (VersionUtils.Compare(currentVersion, entry.Key) < 0)
                continue;

            ++newVersions;
        }

        if (newVersions < 1)
        {
            // This should only happen in development builds as patch notes for the upcoming release is not added yet
            GD.Print("No newer patch notes found, even though there should be some");
            return false;
        }

        recentVersionsCount = newVersions;
        currentVersionIndex = 0;
        ShowVersionAtCurrentIndex();

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
        if (allVersions.Count > 0)
        {
            recentVersionsCount = 0;
            currentVersionIndex = 0;
            ShowVersionAtCurrentIndex();
        }
    }

    private void RebuildVersionList()
    {
        allVersions.Clear();

        foreach (var entry in SimulationParameters.Instance.GetPatchNotes())
        {
            allVersions.Add(entry.Key);
        }

        // Newest first for navigation
        allVersions.Reverse();
    }

    private void ShowVersionAtCurrentIndex()
    {
        if (allVersions.Count == 0)
            return;

        var pageEndExclusive = GetPageEnd(currentVersionIndex);

        patchNotes.FilterNewestVersion = allVersions[currentVersionIndex];
        patchNotes.FilterOldestVersion = allVersions[pageEndExclusive - 1];

        scrollContainer.ScrollVertical = 0;

        newerButton.Disabled = currentVersionIndex == 0;
        olderButton.Disabled = pageEndExclusive >= allVersions.Count;
        recentBoundaryLabel.Visible = recentVersionsCount > 0 && pageEndExclusive == recentVersionsCount;
    }

    /// <summary>
    ///   Returns the exclusive end index of the page that starts at <paramref name="start"/>. Clamped to the recent
    ///   set boundary so that boundary always falls on a page break.
    /// </summary>
    private int GetPageEnd(int start)
    {
        var end = Math.Min(start + VersionsPerPage, allVersions.Count);

        if (recentVersionsCount > 0 && start < recentVersionsCount && recentVersionsCount < end)
            end = recentVersionsCount;

        return end;
    }

    private int GetPrevPageStart(int currentStart)
    {
        // Walk forward from 0; the previous page is the one whose end matches currentStart.
        var start = 0;
        while (true)
        {
            var end = GetPageEnd(start);
            if (end >= currentStart)
                return start;

            start = end;
        }
    }

    private void OnOlderButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        var pageEnd = GetPageEnd(currentVersionIndex);
        if (pageEnd < allVersions.Count)
        {
            currentVersionIndex = pageEnd;
            ShowVersionAtCurrentIndex();
        }
    }

    private void OnNewerButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (currentVersionIndex > 0)
        {
            currentVersionIndex = GetPrevPageStart(currentVersionIndex);
            ShowVersionAtCurrentIndex();
        }
    }
}
