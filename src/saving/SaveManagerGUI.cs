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
    public NodePath SaveListPath;

    [Export]
    public NodePath SelectedItemCountPath;

    [Export]
    public NodePath TotalSaveCountPath;

    [Export]
    public NodePath TotalSaveSizePath;

    [Export]
    public NodePath LoadButtonPath;

    [Export]
    public NodePath DeleteSelectedButtonPath;

    [Export]
    public NodePath DeleteOldButtonPath;

    [Export]
    public NodePath DeleteSelectedConfirmDialogPath;

    [Export]
    public NodePath DeleteOldConfirmDialogPath;

    [Export]
    public NodePath SaveDirectoryWarningDialogPath;

    private SaveList saveList;
    private Label selectedItemCount;
    private Label totalSaveCount;
    private Label totalSaveSize;
    private Button loadButton;
    private Button deleteSelectedButton;
    private Button deleteOldButton;
    private ConfirmationDialog deleteSelectedConfirmDialog;
    private ConfirmationDialog deleteOldConfirmDialog;
    private AcceptDialog saveDirectoryWarningDialog;

    private List<SaveListItem> selected;
    private bool selectedDirty = true;

    private bool saveCountRefreshed;
    private bool refreshing;

    private int currentAutoSaveCount;
    private int currentQuickSaveCount;

    private Task<(int count, long diskSpace)> getTotalSaveCountTask;
    private Task<(int count, long diskSpace)> getAutoSaveCountTask;
    private Task<(int count, long diskSpace)> getQuickSaveCountTask;

    [Signal]
    public delegate void OnBackPressed();

    public List<SaveListItem> Selected
    {
        get
        {
            if (selectedDirty)
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
        deleteSelectedConfirmDialog = GetNode<ConfirmationDialog>(DeleteSelectedConfirmDialogPath);
        deleteOldConfirmDialog = GetNode<ConfirmationDialog>(DeleteOldConfirmDialogPath);
        saveDirectoryWarningDialog = GetNode<AcceptDialog>(SaveDirectoryWarningDialogPath);

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

        if (!getTotalSaveCountTask.IsCompleted)
            return;

        var info = getTotalSaveCountTask.Result;
        currentAutoSaveCount = getAutoSaveCountTask.Result.count;
        currentQuickSaveCount = getQuickSaveCountTask.Result.count;

        getTotalSaveCountTask.Dispose();
        getAutoSaveCountTask.Dispose();
        getQuickSaveCountTask.Dispose();
        getTotalSaveCountTask = null;
        getAutoSaveCountTask = null;
        getQuickSaveCountTask = null;

        totalSaveCount.Text = info.count.ToString(CultureInfo.CurrentCulture);
        totalSaveSize.Text =
            Math.Round((float)info.diskSpace / Constants.MEBIBYTE, 2).ToString(CultureInfo.CurrentCulture) + " MiB";

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

        getTotalSaveCountTask = new Task<(int count, long diskSpace)>(() => SaveHelper.CountSaves());
        getAutoSaveCountTask = new Task<(int count, long diskSpace)>(() => SaveHelper.CountSaves("auto_save"));
        getQuickSaveCountTask = new Task<(int count, long diskSpace)>(() => SaveHelper.CountSaves("quick_save"));

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
        if (OS.ShellOpen(ProjectSettings.GlobalizePath(Constants.SAVE_FOLDER)) == Error.FileNotFound)
            saveDirectoryWarningDialog.PopupCenteredShrink();
    }

    private void DeleteSelectedButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        deleteSelectedConfirmDialog.GetNode<Label>("DialogText").Text =
            string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("DELETE_SELECTED_SAVE_WARNING"),
                Selected.Count);
        deleteSelectedConfirmDialog.PopupCenteredShrink();
    }

    private void DeleteOldButtonPressed()
    {
        int autoSavesToDeleteCount = (currentAutoSaveCount - 1).Clamp(0, Settings.Instance.MaxAutoSaves);
        int quickSavesToDeleteCount = (currentQuickSaveCount - 1).Clamp(0, Settings.Instance.MaxQuickSaves);

        deleteOldConfirmDialog.GetNode<Label>("DialogText").Text =
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
}
