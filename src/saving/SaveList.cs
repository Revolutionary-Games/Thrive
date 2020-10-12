using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

/// <summary>
///   A widget containing a list of saves
/// </summary>
public class SaveList : ScrollContainer
{
    [Export]
    public bool AutoRefreshOnFirstVisible = true;

    [Export]
    public bool AutoRefreshOnBecomingVisible = true;

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

    [Export]
    public NodePath LoadConfirmDialogPath;

    private Control loadingItem;
    private BoxContainer savesList;
    private ConfirmationDialog deleteConfirmDialog;
    private ConfirmationDialog loadConfirmDialog;

    private PackedScene listItemScene;

    private bool refreshing;
    private bool refreshedAtLeastOnce;

    private Task<List<string>> readSavesList;

    private string saveToBeDeleted;
    private string saveToBeLoaded;

    private bool wasVisible;

    [Signal]
    public delegate void OnSelectedChanged();

    [Signal]
    public delegate void OnItemsChanged();

    public override void _Ready()
    {
        loadingItem = GetNode<Control>(LoadingItemPath);
        savesList = GetNode<BoxContainer>(SavesListPath);
        deleteConfirmDialog = GetNode<ConfirmationDialog>(DeleteConfirmDialogPath);
        loadConfirmDialog = GetNode<ConfirmationDialog>(LoadConfirmDialogPath);

        listItemScene = GD.Load<PackedScene>("res://src/saving/SaveListItem.tscn");
    }

    public override void _Process(float delta)
    {
        bool isCurrentlyVisible = IsVisibleInTree();

        if (isCurrentlyVisible && ((AutoRefreshOnFirstVisible && !refreshedAtLeastOnce) ||
            (AutoRefreshOnBecomingVisible && !wasVisible)))
        {
            Refresh();
            wasVisible = true;
            return;
        }

        if (!isCurrentlyVisible)
            wasVisible = false;

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

            item.Connect(nameof(SaveListItem.OnDeleted), this, nameof(OnDeletePressed), new Array { save });

            item.Connect(nameof(SaveListItem.OnOldSaveLoaded), this, nameof(OnOldSaveLoaded), new Array { save });
            item.Connect(nameof(SaveListItem.OnNewSaveLoaded), this, nameof(OnNewSaveLoaded), new Array { save });
            item.Connect(nameof(SaveListItem.OnBrokenSaveLoaded), this, nameof(OnBrokenLoaded), new Array { save });

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
        readSavesList = new Task<List<string>>(() => SaveHelper.CreateListOfSaves());
        TaskExecutor.Instance.AddTask(readSavesList);
    }

    private void OnSubItemSelectedChanged()
    {
        EmitSignal(nameof(OnSelectedChanged));
    }

    private void OnDeletePressed(string saveName)
    {
        GUICommon.Instance.PlayButtonPressSound();

        saveToBeDeleted = saveName;
        deleteConfirmDialog.DialogText =
            $"Deleting this save cannot be undone, are you sure you want to permanently delete {saveName}?";
        deleteConfirmDialog.PopupCenteredMinsize();
    }

    private void OnConfirmDelete()
    {
        GUICommon.Instance.PlayButtonPressSound();

        GD.Print("Deleting save: ", saveToBeDeleted);
        SaveHelper.DeleteSave(saveToBeDeleted);
        saveToBeDeleted = null;

        Refresh();
        EmitSignal(nameof(OnItemsChanged));
    }

    private void OnOldSaveLoaded(string saveName)
    {
        saveToBeLoaded = saveName;

        loadConfirmDialog.DialogText = "This save is from an old version of Thrive and may be incompatible.\n";
        loadConfirmDialog.DialogText += "As Thrive is currently early in development ";
        loadConfirmDialog.DialogText += "save compatibility is not a priority.\n";
        loadConfirmDialog.DialogText += "You may report any issues you encounter, ";
        loadConfirmDialog.DialogText += "but they aren't the highest priority right now.\n";
        loadConfirmDialog.DialogText += "Do you want to try loading the save anyway?";
        loadConfirmDialog.PopupCenteredMinsize();
    }

    private void OnNewSaveLoaded(string saveName)
    {
        saveToBeLoaded = saveName;

        loadConfirmDialog.DialogText = "This save is from a newer version of Thrive and very likely incompatible.\n";
        loadConfirmDialog.DialogText += "Do you want to try loading the save anyway?";
        loadConfirmDialog.PopupCenteredMinsize();
    }

    private void OnBrokenLoaded(string saveName)
    {
        saveToBeLoaded = saveName;

        loadConfirmDialog.DialogText = "Thrive could not load this file.\n";
        loadConfirmDialog.DialogText += "It seems like the save is broken.\n";
        loadConfirmDialog.DialogText += "Do you want to try loading the save anyway?";
        loadConfirmDialog.PopupCenteredMinsize();
    }

    private void OnConfirmSaveLoad()
    {
        GUICommon.Instance.PlayButtonPressSound();

        TransitionManager.Instance.AddScreenFade(Fade.FadeType.FadeIn, 0.3f, true);
        TransitionManager.Instance.StartTransitions(this, nameof(LoadSave));
    }

    private void LoadSave()
    {
        SaveHelper.LoadSave(saveToBeLoaded);
    }
}
