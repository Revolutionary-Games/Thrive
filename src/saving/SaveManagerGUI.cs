using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Shows a GUI to the user that lists the existing saves and allows doing things with them like loading and deleting
/// </summary>
public partial class SaveManagerGUI : Control
{
#pragma warning disable CA2213
    [Export]
    private SaveList saveList = null!;

    [Export]
    private Label selectedItemCount = null!;

    [Export]
    private Label totalSaveCount = null!;

    [Export]
    private Label totalSaveSize = null!;

    [Export]
    private Button loadButton = null!;

    [Export]
    private Button deleteSelectedButton = null!;

    [Export]
    private Button deleteOldButton = null!;

    [Export]
    private CustomConfirmationDialog deleteSelectedConfirmDialog = null!;

    [Export]
    private CustomConfirmationDialog deleteOldConfirmDialog = null!;

    [Export]
    private CustomConfirmationDialog saveDirectoryWarningDialog = null!;

    [Export]
    private CustomConfirmationDialog errorSaveDeletionFailed = null!;
#pragma warning restore CA2213

    private List<SaveListItem>? selected;
    private bool selectedDirty = true;

    private bool saveCountRefreshed;
    private bool refreshing;

    private int currentAutoSaveCount;
    private int currentQuickSaveCount;
    private int currentBackupCount;

    private Task<(int Count, ulong DiskSpace)>? getTotalSaveCountTask;
    private Task<(int Count, ulong DiskSpace)>? getAutoSaveCountTask;
    private Task<(int Count, ulong DiskSpace)>? getQuickSaveCountTask;
    private Task<(int Count, ulong DiskSpace)>? getBackupCountTask;

    [Signal]
    public delegate void OnBackPressedEventHandler();

    public List<SaveListItem> Selected
    {
        get
        {
            if (selectedDirty || selected == null)
            {
                selected = saveList.GetSelectedItems().ToList();
                selectedDirty = false;
            }

            return selected;
        }
    }

    public override void _Ready()
    {
        saveList.Connect(SaveList.SignalName.OnItemsChanged, new Callable(this, nameof(RefreshSaveCounts)));
    }

    public override void _Process(double delta)
    {
        if (!saveCountRefreshed && IsVisibleInTree())
        {
            RefreshSaveCounts();
            return;
        }

        if (!refreshing)
            return;

        if (!getTotalSaveCountTask!.IsCompleted)
            return;

        var info = getTotalSaveCountTask.Result;
        currentAutoSaveCount = getAutoSaveCountTask!.Result.Count;
        currentQuickSaveCount = getQuickSaveCountTask!.Result.Count;
        currentBackupCount = getBackupCountTask!.Result.Count;

        getTotalSaveCountTask.Dispose();
        getAutoSaveCountTask.Dispose();
        getQuickSaveCountTask.Dispose();
        getBackupCountTask.Dispose();
        getTotalSaveCountTask = null;
        getAutoSaveCountTask = null;
        getQuickSaveCountTask = null;
        getBackupCountTask = null;

        totalSaveCount.Text = info.Count.ToString(CultureInfo.CurrentCulture);
        totalSaveSize.Text = Localization.Translate("MIB_VALUE")
            .FormatSafe(Math.Round((float)info.DiskSpace / Constants.MEBIBYTE, 2));

        UpdateSelectedCount();
        UpdateButtonsStatus();

        refreshing = false;
    }

    private void OnSelectedChanged()
    {
        selectedDirty = true;

        UpdateSelectedCount();
        UpdateButtonsStatus();
    }

    private void UpdateSelectedCount()
    {
        selectedItemCount.Text = Selected.Count.ToString(CultureInfo.CurrentCulture);
    }

    private void RefreshList()
    {
        saveList.Refresh();
    }

    private void RefreshSaveCounts()
    {
        if (refreshing)
            return;

        selectedDirty = true;
        saveCountRefreshed = true;
        refreshing = true;

        getTotalSaveCountTask = new Task<(int Count, ulong DiskSpace)>(() => SaveHelper.CountSaves());
        getAutoSaveCountTask =
            new Task<(int Count, ulong DiskSpace)>(() => SaveHelper.CountSaves(Constants.AutoSaveRegex));
        getQuickSaveCountTask =
            new Task<(int Count, ulong DiskSpace)>(() => SaveHelper.CountSaves(Constants.QuickSaveRegex));
        getBackupCountTask =
            new Task<(int Count, ulong DiskSpace)>(() => SaveHelper.CountSaves(Constants.BackupRegex));

        TaskExecutor.Instance.AddTask(getTotalSaveCountTask, false);
        TaskExecutor.Instance.AddTask(getAutoSaveCountTask, false);
        TaskExecutor.Instance.AddTask(getQuickSaveCountTask, false);
        TaskExecutor.Instance.AddTask(getBackupCountTask);
    }

    private void UpdateButtonsStatus()
    {
        loadButton.Disabled = Selected.Count != 1;
        deleteSelectedButton.Disabled = Selected.Count == 0;
        deleteOldButton.Disabled = currentAutoSaveCount < 2 &&
            currentQuickSaveCount < 2 &&
            currentBackupCount < 1;
    }

    private void LoadFirstSelectedSave()
    {
        if (Selected.Count < 1)
            return;

        Selected[0].LoadThisSave();
    }

    private void LoadFirstSelectedSaveButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        LoadFirstSelectedSave();
    }

    private void RefreshButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        RefreshList();
    }

    private void OpenSaveDirectoryPressed()
    {
        if (!FolderHelpers.OpenFolder(Constants.SAVE_FOLDER))
            saveDirectoryWarningDialog.PopupCenteredShrink();
    }

    private void DeleteSelectedButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        deleteSelectedConfirmDialog.DialogText =
            Localization.Translate("DELETE_SELECTED_SAVE_WARNING").FormatSafe(Selected.Count);
        deleteSelectedConfirmDialog.PopupCenteredShrink();
    }

    private void DeleteOldButtonPressed()
    {
        int autoSavesToDeleteCount = Math.Max(currentAutoSaveCount - 1, 0);
        int quickSavesToDeleteCount = Math.Max(currentQuickSaveCount - 1, 0);
        int oldBackupsToDeleteCount = Math.Max(currentBackupCount, 0);

        deleteOldConfirmDialog.DialogText = Localization.Translate("DELETE_ALL_OLD_SAVE_WARNING_2").FormatSafe(
            autoSavesToDeleteCount, quickSavesToDeleteCount, oldBackupsToDeleteCount);
        deleteOldConfirmDialog.PopupCenteredShrink();
    }

    private void OnConfirmDeleteSelected()
    {
        GUICommon.Instance.PlayButtonPressSound();

        GD.Print("Deleting save(s): ", string.Join(", ", Selected.Select(s => s.SaveName).ToList()));

        try
        {
            Selected.ForEach(s => SaveHelper.DeleteSave(s.SaveName));
        }
        catch (IOException e)
        {
            errorSaveDeletionFailed.PopupCenteredShrink();
            GD.Print("Failed to delete save: ", e.Message);
        }

        deleteSelectedButton.Disabled = true;
        selected = null;

        RefreshList();
    }

    private void OnConfirmDeleteOld()
    {
        string message = string.Join(", ", SaveHelper.CleanUpOldSavesOfType(Constants.AutoSaveRegex));

        if (message.Length > 0)
            message += ", ";

        message += string.Join(", ", SaveHelper.CleanUpOldSavesOfType(Constants.QuickSaveRegex));

        if (message.Length > 0)
            message += ", ";

        message += string.Join(", ", SaveHelper.CleanUpOldSavesOfType(Constants.BackupRegex, true));

        GD.Print("Deleted save(s): ", message);

        RefreshList();
    }

    private void OnBackButton()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OnBackPressed);
    }

    private void OnSaveListItemConfirmed(SaveListItem item)
    {
        item.LoadThisSave();
    }
}
