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

    private SaveList saveList;
    private Label selectedItemCount;
    private Label totalSaveCount;
    private Label totalSaveSize;
    private Button loadButton;
    private Button deleteSelectedButton;
    private ConfirmationDialog deleteSelectedConfirmDialog;

    // ReSharper disable once NotAccessedField.Local
    private Button deleteOldButton;

    private List<SaveListItem> selected;
    private bool selectedDirty = true;

    private bool saveCountRefreshed;
    private bool refreshing;

    private Task<(int count, long diskSpace)> getSaveCountTask;

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

        saveList.Connect(nameof(SaveList.OnItemsChanged), this, nameof(RefreshList));
    }

    public override void _Process(float delta)
    {
        if (!saveCountRefreshed && IsVisibleInTree())
        {
            RefreshSaveCount();
            return;
        }

        if (!refreshing)
            return;

        if (!getSaveCountTask.IsCompleted)
            return;

        var info = getSaveCountTask.Result;
        getSaveCountTask.Dispose();
        getSaveCountTask = null;

        totalSaveCount.Text = info.count.ToString(CultureInfo.CurrentCulture);
        totalSaveSize.Text =
            Math.Round((float)info.diskSpace / Constants.MEBIBYTE, 2).ToString(CultureInfo.CurrentCulture) + " MiB";

        UpdateSelectedCount();

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
        selectedDirty = true;

        saveList.Refresh();
        RefreshSaveCount();
    }

    private void RefreshSaveCount()
    {
        if (refreshing)
            return;

        saveCountRefreshed = true;
        refreshing = true;

        getSaveCountTask = new Task<(int count, long diskSpace)>(() => SaveHelper.CountSaves());
        TaskExecutor.Instance.AddTask(getSaveCountTask);
    }

    private void UpdateButtonsStatus()
    {
        loadButton.Disabled = Selected.Count != 1;
        deleteSelectedButton.Disabled = Selected.Count == 0;
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

    private void DeleteSelectedButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        deleteSelectedConfirmDialog.DialogText =
            "Deleting the selected save(s) cannot be undone, are you sure you want to permanently delete " +
            $"{Selected.Count} save(s)?";
        deleteSelectedConfirmDialog.PopupCenteredMinsize();
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

    private void OnBackButton()
    {
        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(OnBackPressed));
    }
}
