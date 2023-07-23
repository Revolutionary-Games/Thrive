using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Shows a GUI to the user that lists the existing saves and allows doing things with them like loading and deleting
/// </summary>
public class SaveManagerGUI : Control
{
    [Export]
    public NodePath? SaveListPath;

    [Export]
    public NodePath SelectedItemCountPath = null!;

    [Export]
    public NodePath TotalSaveCountPath = null!;

    [Export]
    public NodePath TotalSaveSizePath = null!;

    [Export]
    public NodePath LoadButtonPath = null!;

    [Export]
    public NodePath DeleteSelectedButtonPath = null!;

    [Export]
    public NodePath DeleteOldButtonPath = null!;

    [Export]
    public NodePath DeleteSelectedConfirmDialogPath = null!;

    [Export]
    public NodePath DeleteOldConfirmDialogPath = null!;

    [Export]
    public NodePath SaveDirectoryWarningDialogPath = null!;

    [Export]
    public NodePath ErrorSaveDeletionFailedPath = null!;

#pragma warning disable CA2213
    private SaveList saveList = null!;
    private Label selectedItemCount = null!;
    private Label totalSaveCount = null!;
    private Label totalSaveSize = null!;
    private Button loadButton = null!;
    private Button deleteSelectedButton = null!;
    private Button deleteOldButton = null!;
    private CustomConfirmationDialog deleteSelectedConfirmDialog = null!;
    private CustomConfirmationDialog deleteOldConfirmDialog = null!;
    private CustomConfirmationDialog saveDirectoryWarningDialog = null!;
    private ErrorDialog errorSaveDeletionFailed = null!;
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
    public delegate void OnBackPressed();

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
        saveList = GetNode<SaveList>(SaveListPath);
        selectedItemCount = GetNode<Label>(SelectedItemCountPath);
        totalSaveCount = GetNode<Label>(TotalSaveCountPath);
        totalSaveSize = GetNode<Label>(TotalSaveSizePath);
        loadButton = GetNode<Button>(LoadButtonPath);
        deleteSelectedButton = GetNode<Button>(DeleteSelectedButtonPath);
        deleteOldButton = GetNode<Button>(DeleteOldButtonPath);
        deleteSelectedConfirmDialog = GetNode<CustomConfirmationDialog>(DeleteSelectedConfirmDialogPath);
        deleteOldConfirmDialog = GetNode<CustomConfirmationDialog>(DeleteOldConfirmDialogPath);
        saveDirectoryWarningDialog = GetNode<CustomConfirmationDialog>(SaveDirectoryWarningDialogPath);
        errorSaveDeletionFailed = GetNode<ErrorDialog>(ErrorSaveDeletionFailedPath);

        saveList.Connect(nameof(SaveList.OnItemsChanged), this, nameof(RefreshSaveCounts));
    }

    public override void _Process(float delta)
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
        totalSaveSize.Text = TranslationServer.Translate("MIB_VALUE")
            .FormatSafe(Math.Round((float)info.DiskSpace / Constants.MEBIBYTE, 2));

        UpdateSelectedCount();
        UpdateButtonsStatus();

        refreshing = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (SaveListPath != null)
            {
                SaveListPath.Dispose();
                SelectedItemCountPath.Dispose();
                TotalSaveCountPath.Dispose();
                TotalSaveSizePath.Dispose();
                LoadButtonPath.Dispose();
                DeleteSelectedButtonPath.Dispose();
                DeleteOldButtonPath.Dispose();
                DeleteSelectedConfirmDialogPath.Dispose();
                DeleteOldConfirmDialogPath.Dispose();
                SaveDirectoryWarningDialogPath.Dispose();
                ErrorSaveDeletionFailedPath.Dispose();
            }
        }

        base.Dispose(disposing);
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

        TaskExecutor.Instance.AddTask(getTotalSaveCountTask);
        TaskExecutor.Instance.AddTask(getAutoSaveCountTask);
        TaskExecutor.Instance.AddTask(getQuickSaveCountTask);
        TaskExecutor.Instance.AddTask(getBackupCountTask);
    }

    private void UpdateButtonsStatus()
    {
        loadButton.Disabled = Selected.Count != 1;
        deleteSelectedButton.Disabled = Selected.Count == 0;
        deleteOldButton.Disabled = (currentAutoSaveCount < 2) &&
            (currentQuickSaveCount < 2) &&
            (currentBackupCount < 1);
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
            TranslationServer.Translate("DELETE_SELECTED_SAVE_WARNING").FormatSafe(Selected.Count);
        deleteSelectedConfirmDialog.PopupCenteredShrink();
    }

    private void DeleteOldButtonPressed()
    {
        int autoSavesToDeleteCount = Math.Max(currentAutoSaveCount - 1, 0);
        int quickSavesToDeleteCount = Math.Max(currentQuickSaveCount - 1, 0);
        int oldBackupsToDeleteCount = Math.Max(currentBackupCount, 0);

        deleteOldConfirmDialog.DialogText = TranslationServer.Translate("DELETE_ALL_OLD_SAVE_WARNING_2").FormatSafe(
            autoSavesToDeleteCount, quickSavesToDeleteCount, oldBackupsToDeleteCount);
        deleteOldConfirmDialog.PopupCenteredShrink();
    }

    private void OnConfirmDeleteSelected()
    {
        GUICommon.Instance.PlayButtonPressSound();

        GD.Print("Deleting save(s): ", string.Join(", ", Selected.Select(item => item.SaveName).ToList()));
        try
        {
            Selected.ForEach(item => SaveHelper.DeleteSave(item.SaveName));
        }
        catch (SaveHelper.FailedToDeleteSaveException e)
        {
            errorSaveDeletionFailed.ShowError(
                "Failed to delete save.", e.Message, e.StackTrace);
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
        EmitSignal(nameof(OnBackPressed));
    }

    private void OnSaveListItemConfirmed(SaveListItem item)
    {
        item.LoadThisSave();
    }
}
