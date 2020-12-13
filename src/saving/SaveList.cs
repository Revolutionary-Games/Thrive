using System.Collections.Generic;
using System.Globalization;
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
    public NodePath LoadNewerSaveDialogPath;

    [Export]
    public NodePath LoadOlderSaveDialogPath;

    [Export]
    public NodePath LoadInvalidSaveDialogPath;

    [Export]
    public NodePath LoadIncompatibleDialogPath;

    private Control loadingItem;
    private BoxContainer savesList;
    private ConfirmationDialog deleteConfirmDialog;
    private ConfirmationDialog loadNewerConfirmDialog;
    private ConfirmationDialog loadOlderConfirmDialog;
    private ConfirmationDialog loadInvalidConfirmDialog;
    private AcceptDialog loadIncompatibleDialog;

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
        loadOlderConfirmDialog = GetNode<ConfirmationDialog>(LoadOlderSaveDialogPath);
        loadNewerConfirmDialog = GetNode<ConfirmationDialog>(LoadNewerSaveDialogPath);
        loadInvalidConfirmDialog = GetNode<ConfirmationDialog>(LoadInvalidSaveDialogPath);
        loadIncompatibleDialog = GetNode<AcceptDialog>(LoadIncompatibleDialogPath);

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
            item.Connect(nameof(SaveListItem.OnBrokenSaveLoaded), this, nameof(OnInvalidLoaded), new Array { save });
            item.Connect(nameof(SaveListItem.OnKnownIncompatibleLoaded), this, nameof(OnKnownIncompatibleLoaded));

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

        // Deleting this save cannot be undone, are you sure you want to permanently delete {0}?
        deleteConfirmDialog.DialogText = string.Format(CultureInfo.CurrentCulture,
            TranslationServer.Translate("SAVE_DELETE_WARNING"),
            saveName);
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
        loadOlderConfirmDialog.PopupCenteredMinsize();
    }

    private void OnNewSaveLoaded(string saveName)
    {
        saveToBeLoaded = saveName;
        loadNewerConfirmDialog.PopupCenteredMinsize();
    }

    private void OnInvalidLoaded(string saveName)
    {
        saveToBeLoaded = saveName;
        loadInvalidConfirmDialog.PopupCenteredMinsize();
    }

    private void OnKnownIncompatibleLoaded()
    {
        loadIncompatibleDialog.PopupCenteredMinsize();
    }

    private void OnConfirmLoadOlder()
    {
        GD.PrintErr("The user requested to load an older save.");
        OnConfirmSaveLoad();
    }

    private void OnConfirmLoadNewer()
    {
        GD.PrintErr("The user requested to load a newer save.");
        OnConfirmSaveLoad();
    }

    private void OnConfirmLoadInvalid()
    {
        GD.PrintErr("The user requested to load an invalid save.");
        OnConfirmSaveLoad();
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
