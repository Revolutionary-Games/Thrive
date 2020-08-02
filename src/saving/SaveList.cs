using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

/// <summary>
///   A widget containing a list of saves
/// </summary>
public class SaveList : ScrollContainer
{
    [Export]
    public bool AutoRefreshOnFirstVisible = true;

    [Export]
    public bool SelectableItems;

    [Export]
    public bool LoadableItems = true;

    [Export]
    public NodePath LoadingItemPath;

    [Export]
    public NodePath SavesListPath;

    [Export]
    public NodePath DeleteConfirmDialogPath;

    private Control loadingItem;
    private BoxContainer savesList;
    private ConfirmationDialog deleteConfirmDialog;

    private PackedScene listItemScene;

    private bool refreshing;
    private bool refreshedAtLeastOnce;

    private Task<List<string>> readSavesList;

    private string saveToBeDeleted;

    [Signal]
    public delegate void OnSelectedChanged();

    public override void _Ready()
    {
        loadingItem = GetNode<Control>(LoadingItemPath);
        savesList = GetNode<BoxContainer>(SavesListPath);
        deleteConfirmDialog = GetNode<ConfirmationDialog>(DeleteConfirmDialogPath);

        listItemScene = GD.Load<PackedScene>("res://src/saving/SaveListItem.tscn");
    }

    public override void _Process(float delta)
    {
        if (AutoRefreshOnFirstVisible && !refreshedAtLeastOnce && IsVisibleInTree())
        {
            Refresh();
            return;
        }

        if (!refreshing)
            return;

        if (!readSavesList.IsCompleted)
            return;

        var saves = readSavesList.Result;
        readSavesList.Dispose();
        readSavesList = null;

        foreach (var save in saves)
        {
            var item = (SaveListItem)listItemScene.Instance();
            item.Selectable = SelectableItems;
            item.Loadable = LoadableItems;

            if (SelectableItems)
                item.Connect(nameof(SaveListItem.OnSelectedChanged), this, nameof(OnSubItemSelectedChanged));

            item.Connect(nameof(SaveListItem.OnDeleted), this, nameof(OnDeletePressed), new Array() { save });

            item.SaveName = save;
            savesList.AddChild(item);
        }

        loadingItem.Visible = false;
        refreshing = false;
    }

    public IEnumerable<SaveListItem> GetSelectedItems()
    {
        foreach (SaveListItem child in savesList.GetChildren())
        {
            if (child.Selectable && child.Selected)
                yield return child;
        }
    }

    public void Refresh()
    {
        if (refreshing)
            return;

        refreshing = true;
        refreshedAtLeastOnce = true;

        foreach (var child in savesList.GetChildren())
        {
            ((Node)child).QueueFree();
        }

        loadingItem.Visible = true;
        readSavesList = new Task<List<string>>(() => SaveManager.CreateListOfSaves());
        TaskExecutor.Instance.AddTask(readSavesList);
    }

    private void OnSubItemSelectedChanged()
    {
        EmitSignal(nameof(OnSelectedChanged));
    }

    private void OnDeletePressed(string saveName)
    {
        saveToBeDeleted = saveName;
        deleteConfirmDialog.DialogText =
            $"Deleting this save cannot be undone, are you sure you want to permanently delete {saveName}?";
        deleteConfirmDialog.PopupCenteredMinsize();
    }

    private void OnConfirmDelete()
    {
        GD.Print("Deleting save: ", saveToBeDeleted);
        SaveHelper.DeleteSave(saveToBeDeleted);
        saveToBeDeleted = null;

        Refresh();
    }

    
}
