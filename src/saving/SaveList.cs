using System.Collections.Generic;
using System.Diagnostics;
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
    public NodePath LoadInvalidSaveOkPath;

    [Export]
    public NodePath LoadInvalidSaveCancelPath;

    [Export]
    public NodePath LoadInvalidSaveOpenLogPath;

    private Control loadingItem;
    private BoxContainer savesList;
    private ConfirmationDialog deleteConfirmDialog;
    private ConfirmationDialog loadNewerConfirmDialog;
    private ConfirmationDialog loadOlderConfirmDialog;
    private WindowDialog loadInvalidConfirmDialog;

    private Button loadInvalidConfirmOk;
    private Button loadInvalidConfirmCancel;
    private Button loadInvalidConfirmOpenLog;

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
        loadInvalidConfirmDialog = GetNode<WindowDialog>(LoadInvalidSaveDialogPath);

        loadInvalidConfirmOk = GetNode<Button>(LoadInvalidSaveOkPath);
        loadInvalidConfirmCancel = GetNode<Button>(LoadInvalidSaveCancelPath);
        loadInvalidConfirmOpenLog = GetNode<Button>(LoadInvalidSaveOpenLogPath);

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

            loadInvalidConfirmOk.Connect("pressed", this, nameof(LoadSave));
            loadInvalidConfirmOpenLog.Connect("pressed", this, nameof(OnInvalidOpenLog), new Array { save });
            loadInvalidConfirmCancel.Connect("pressed", this, nameof(OnInvalidCancel));

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

    private void OnInvalidOpenLog(string saveName)
    {
        var path = ProjectSettings.GlobalizePath($"user://logs/{saveName}.error.log.txt");
        Process.Start(path);
    }

    private void OnInvalidCancel()
    {
        loadInvalidConfirmDialog.Hide();
    }

    private void OnConfirmLoadOlder()
    {
        Save.LogErrorToFile(saveToBeLoaded, "OLD", null);
        OnConfirmSaveLoad();
    }

    private void OnConfirmLoadNewer()
    {
        Save.LogErrorToFile(saveToBeLoaded, "NEW", null);
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
