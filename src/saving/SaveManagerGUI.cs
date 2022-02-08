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
    public NodePath SaveListPath = null!;

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

    private List<SaveListItem>? selected;
    private bool selectedDirty = true;

    private bool saveCountRefreshed;
    private bool refreshing;

    private int currentAutoSaveCount;
    private int currentQuickSaveCount;

    private Task<(int Count, ulong DiskSpace)>? getTotalSaveCountTask;
    private Task<(int Count, ulong DiskSpace)>? getAutoSaveCountTask;
    private Task<(int Count, ulong DiskSpace)>? getQuickSaveCountTask;

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

        getTotalSaveCountTask.Dispose();
        getAutoSaveCountTask.Dispose();
        getQuickSaveCountTask.Dispose();
        getTotalSaveCountTask = null;
        getAutoSaveCountTask = null;
        getQuickSaveCountTask = null;

        totalSaveCount.Text = info.Count.ToString(CultureInfo.CurrentCulture);
        totalSaveSize.Text =
            Math.Round((float)info.DiskSpace / Constants.MEBIBYTE, 2).ToString(CultureInfo.CurrentCulture) + " MiB";

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
        getAutoSaveCountTask = new Task<(int Count, ulong DiskSpace)>(() => SaveHelper.CountSaves("auto_save"));
        getQuickSaveCountTask = new Task<(int Count, ulong DiskSpace)>(() => SaveHelper.CountSaves("quick_save"));

        TaskExecutor.Instance.AddTask(getTotalSaveCountTask);
        TaskExecutor.Instance.AddTask(getAutoSaveCountTask);
        TaskExecutor.Instance.AddTask(getQuickSaveCountTask);
    }

    private void UpdateButtonsStatus()
    {
        loadButton.Disabled = Selected.Count != 1;
        deleteSelectedButton.Disabled = Selected.Count == 0;
        deleteOldButton.Disabled = (currentAutoSaveCount <= 1) && (currentQuickSaveCount <= 1);
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
            string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("DELETE_SELECTED_SAVE_WARNING"),
                Selected.Count);
        deleteSelectedConfirmDialog.PopupCenteredShrink();
    }

    private void DeleteOldButtonPressed()
    {
        int autoSavesToDeleteCount = (currentAutoSaveCount - 1).Clamp(0, Settings.Instance.MaxAutoSaves);
        int quickSavesToDeleteCount = (currentQuickSaveCount - 1).Clamp(0, Settings.Instance.MaxQuickSaves);

        deleteOldConfirmDialog.DialogText =
            string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("DELETE_ALL_OLD_SAVE_WARNING"),
                autoSavesToDeleteCount, quickSavesToDeleteCount);
        deleteOldConfirmDialog.PopupCenteredShrink();
    }

    private void OnConfirmDeleteSelected()
    {
        GUICommon.Instance.PlayButtonPressSound();

        GD.Print("Deleting save(s): ", string.Join(", ", Selected.Select(item => item.SaveName).ToList()));

        Selected.ForEach(item => SaveHelper.DeleteSave(item.SaveName));
        deleteSelectedButton.Disabled = true;
        selected = null;

        RefreshList();
    }

    private void OnConfirmDeleteOld()
    {
        string message = string.Join(", ", SaveHelper.CleanUpOldSavesOfType("auto_save"));

        if (message.Length > 0)
            message += ", ";

        message += string.Join(", ", SaveHelper.CleanUpOldSavesOfType("quick_save"));

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
