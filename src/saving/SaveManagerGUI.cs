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

    private SaveList saveList;
    private Label selectedItemCount;
    private Label totalSaveCount;
    private Label totalSaveSize;
    private Button loadButton;
    private Button deleteSelectedButton;
    private Button deleteOldButton;

    private List<SaveListItem> selected;
    private bool selectedDirty = true;

    private bool saveCountRefreshed = false;
    private bool refreshing = false;

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
        totalSaveSize.Text = Math.Round((float)info.diskSpace / Constants.MEBIBYTE, 2).
            ToString(CultureInfo.CurrentCulture) + " MiB";

        refreshing = false;
    }

    private void OnSelectedChanged()
    {
        selectedDirty = true;

        UpdateSelectedCount();
        UpdateLoadButtonStatus();
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

        getSaveCountTask = new Task<(int count, long diskSpace)>(() => SaveManager.CountSaves());
        TaskExecutor.Instance.AddTask(getSaveCountTask);
    }

    private void UpdateLoadButtonStatus()
    {
        loadButton.Disabled = Selected.Count != 1;
    }

    private void LoadFirstSelectedSave()
    {
        if (Selected.Count < 1)
            return;

        SaveHelper.LoadSave(Selected[0].SaveName);
    }

    private void OnBackButton()
    {
        EmitSignal(nameof(OnBackPressed));
    }

    private void OnVisibilityChanged()
    {
        RefreshList();
    }
}
