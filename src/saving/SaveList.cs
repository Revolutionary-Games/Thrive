using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Array = Godot.Collections.Array;

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
    public NodePath? LoadingItemPath;

    [Export]
    public NodePath NoSavesItemPath = null!;

    [Export]
    public NodePath SavesListPath = null!;

    [Export]
    public NodePath DeleteConfirmDialogPath = null!;

    [Export]
    public NodePath LoadNewerSaveDialogPath = null!;

    [Export]
    public NodePath LoadOlderSaveDialogPath = null!;

    [Export]
    public NodePath LoadInvalidSaveDialogPath = null!;

    [Export]
    public NodePath LoadIncompatibleDialogPath = null!;

    [Export]
    public NodePath UpgradeSaveDialogPath = null!;

    [Export]
    public NodePath UpgradeFailedDialogPath = null!;

    [Export]
    public NodePath LoadIncompatiblePrototypeDialogPath = null!;

    [Export]
    public NodePath ErrorSaveDeletionFailedPath = null!;

#pragma warning disable CA2213
    private Control loadingItem = null!;
    private Control noSavesItem = null!;
    private BoxContainer savesList = null!;
    private CustomConfirmationDialog deleteConfirmDialog = null!;
    private CustomConfirmationDialog loadNewerConfirmDialog = null!;
    private CustomConfirmationDialog loadOlderConfirmDialog = null!;
    private CustomConfirmationDialog loadInvalidConfirmDialog = null!;
    private CustomConfirmationDialog loadIncompatibleDialog = null!;
    private CustomConfirmationDialog upgradeSaveDialog = null!;
    private CustomConfirmationDialog loadIncompatiblePrototypeDialog = null!;
    private ErrorDialog upgradeFailedDialog = null!;
    private CustomConfirmationDialog errorSaveDeletionFailed = null!;
    private PackedScene listItemScene = null!;
#pragma warning restore CA2213

    private bool refreshing;
    private bool refreshedAtLeastOnce;

    private Task<List<string>>? readSavesList;

    private string? saveToBeDeleted;
    private string? saveToBeLoaded;
    private bool suppressSaveUpgradeClose;

    private bool wasVisible;
    private bool incompatibleIfNotUpgraded;

    [Signal]
    public delegate void OnSelectedChanged();

    [Signal]
    public delegate void OnItemsChanged();

    [Signal]
    public delegate void OnConfirmed(SaveListItem item);

    [Signal]
    public delegate void OnSaveLoaded(string saveName);

    public override void _Ready()
    {
        loadingItem = GetNode<Control>(LoadingItemPath);
        noSavesItem = GetNode<Control>(NoSavesItemPath);
        savesList = GetNode<BoxContainer>(SavesListPath);
        deleteConfirmDialog = GetNode<CustomConfirmationDialog>(DeleteConfirmDialogPath);
        loadOlderConfirmDialog = GetNode<CustomConfirmationDialog>(LoadOlderSaveDialogPath);
        loadNewerConfirmDialog = GetNode<CustomConfirmationDialog>(LoadNewerSaveDialogPath);
        loadInvalidConfirmDialog = GetNode<CustomConfirmationDialog>(LoadInvalidSaveDialogPath);
        loadIncompatibleDialog = GetNode<CustomConfirmationDialog>(LoadIncompatibleDialogPath);
        upgradeSaveDialog = GetNode<CustomConfirmationDialog>(UpgradeSaveDialogPath);
        upgradeFailedDialog = GetNode<ErrorDialog>(UpgradeFailedDialogPath);
        loadIncompatiblePrototypeDialog = GetNode<CustomConfirmationDialog>(LoadIncompatiblePrototypeDialogPath);
        errorSaveDeletionFailed = GetNode<CustomConfirmationDialog>(ErrorSaveDeletionFailedPath);

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

        if (!readSavesList!.IsCompleted)
            return;

        var saves = readSavesList.Result;
        readSavesList.Dispose();
        readSavesList = null;

        if (saves.Count > 0)
        {
            noSavesItem.Visible = false;

            foreach (var save in saves)
            {
                var item = (SaveListItem)listItemScene.Instance();
                item.Selectable = SelectableItems;
                item.Loadable = LoadableItems;

                if (SelectableItems)
                    item.Connect(nameof(SaveListItem.OnSelectedChanged), this, nameof(OnSubItemSelectedChanged));

                item.Connect(nameof(SaveListItem.OnDoubleClicked), this, nameof(OnItemDoubleClicked),
                    new Array { item });

                item.Connect(nameof(SaveListItem.OnDeleted), this, nameof(OnDeletePressed), new Array { save });

                item.Connect(nameof(SaveListItem.OnOldSaveLoaded), this, nameof(OnOldSaveLoaded), new Array { save });

                // This can't use binds because we need an additional dynamic parameter from the list item here
                item.Connect(nameof(SaveListItem.OnUpgradeableSaveLoaded), this, nameof(OnUpgradeableSaveLoaded));
                item.Connect(nameof(SaveListItem.OnNewSaveLoaded), this, nameof(OnNewSaveLoaded), new Array { save });
                item.Connect(nameof(SaveListItem.OnBrokenSaveLoaded), this, nameof(OnInvalidLoaded),
                    new Array { save });
                item.Connect(nameof(SaveListItem.OnKnownIncompatibleLoaded), this, nameof(OnKnownIncompatibleLoaded));
                item.Connect(nameof(SaveListItem.OnDifferentVersionPrototypeLoaded), this,
                    nameof(OnDifferentVersionPrototypeLoaded));
                item.Connect(nameof(SaveListItem.OnProblemFreeSaveLoaded), this, nameof(OnSaveLoadedWithoutProblems));

                item.SaveName = save;
                savesList.AddChild(item);
            }
        }
        else
        {
            noSavesItem.Visible = true;
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

        savesList.QueueFreeChildren();

        loadingItem.Visible = true;
        readSavesList = new Task<List<string>>(() => SaveHelper.CreateListOfSaves());
        TaskExecutor.Instance.AddTask(readSavesList);
        EmitSignal(nameof(OnItemsChanged));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (LoadingItemPath != null)
            {
                LoadingItemPath.Dispose();
                NoSavesItemPath.Dispose();
                SavesListPath.Dispose();
                DeleteConfirmDialogPath.Dispose();
                LoadNewerSaveDialogPath.Dispose();
                LoadOlderSaveDialogPath.Dispose();
                LoadInvalidSaveDialogPath.Dispose();
                LoadIncompatibleDialogPath.Dispose();
                UpgradeSaveDialogPath.Dispose();
                UpgradeFailedDialogPath.Dispose();
                LoadIncompatiblePrototypeDialogPath.Dispose();
                ErrorSaveDeletionFailedPath.Dispose();
            }
        }

        base.Dispose(disposing);
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
        deleteConfirmDialog.DialogText = TranslationServer.Translate("SAVE_DELETE_WARNING").FormatSafe(saveName);
        deleteConfirmDialog.PopupCenteredShrink();
    }

    private void OnConfirmDelete()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (saveToBeDeleted == null)
        {
            GD.PrintErr("Save to confirm delete is null");
            return;
        }

        GD.Print("Deleting save: ", saveToBeDeleted);
        try
        {
            SaveHelper.DeleteSave(saveToBeDeleted);
        }
        catch (IOException e)
        {
            errorSaveDeletionFailed.PopupCenteredShrink();
            GD.Print("Failed to delete save: ", e.Message);
        }

        saveToBeDeleted = null;

        Refresh();
        EmitSignal(nameof(OnItemsChanged));
    }

    private void OnOldSaveLoaded(string saveName)
    {
        saveToBeLoaded = saveName;
        loadOlderConfirmDialog.PopupCenteredShrink();
    }

    private void OnUpgradeableSaveLoaded(string saveName, bool incompatible)
    {
        saveToBeLoaded = saveName;
        incompatibleIfNotUpgraded = incompatible;
        upgradeSaveDialog.PopupCenteredShrink();
    }

    private void OnNewSaveLoaded(string saveName)
    {
        saveToBeLoaded = saveName;
        loadNewerConfirmDialog.PopupCenteredShrink();
    }

    private void OnInvalidLoaded(string saveName)
    {
        saveToBeLoaded = saveName;
        loadInvalidConfirmDialog.PopupCenteredShrink();
    }

    private void OnKnownIncompatibleLoaded()
    {
        loadIncompatibleDialog.PopupCenteredShrink();
    }

    private void OnDifferentVersionPrototypeLoaded()
    {
        loadIncompatiblePrototypeDialog.PopupCenteredShrink();
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

    private void OnAcceptSaveUpgrade()
    {
        suppressSaveUpgradeClose = true;

        if (saveToBeLoaded == null)
        {
            GD.PrintErr("Save to upgrade is null");
            return;
        }

        GD.Print("Save upgrade accepted by user on: ", saveToBeLoaded);

        var saveToUpgrade = saveToBeLoaded;

        if (SaveUpgrader.IsSaveABackup(saveToBeLoaded))
        {
            saveToBeLoaded = SaveUpgrader.RemoveBackupSuffix(saveToBeLoaded);
            GD.Print("Selected save is a backup, really going to load after upgrade: ", saveToBeLoaded);
        }

        // Perform save upgrade (the game will lag here, but I'll leave it to someone else to make a progress bar)
        // Instead could show a popup with a spinner on it and run the upgrade with TaskExecutor in the background
        var task = new Task(() => SaveUpgrader.PerformSaveUpgrade(saveToUpgrade));

        TaskExecutor.Instance.AddTask(task);

        try
        {
            if (!task.Wait(TimeSpan.FromMinutes(1)))
            {
                throw new Exception("Upgrade failed to complete within acceptable time");
            }
        }
        catch (Exception e)
        {
            upgradeFailedDialog.ExceptionInfo = e.Message;
            upgradeFailedDialog.PopupCenteredShrink();

            GD.PrintErr("Save upgrade failed: ", e);
            return;
        }

        OnConfirmSaveLoad();
    }

    private void OnSaveUpgradeClosed()
    {
        // It seems this callback gets always called first, even when the accept button is pressed so we need to delay
        // processing this
        Invoke.Instance.Queue(() =>
        {
            if (!suppressSaveUpgradeClose)
            {
                // If it is known incompatible show that dialog instead
                if (incompatibleIfNotUpgraded)
                {
                    OnKnownIncompatibleLoaded();
                }
                else
                {
                    if (saveToBeLoaded == null)
                    {
                        GD.PrintErr("Save to load after upgrade is null");
                        return;
                    }

                    OnOldSaveLoaded(saveToBeLoaded);
                }
            }

            suppressSaveUpgradeClose = false;
            incompatibleIfNotUpgraded = false;
        });
    }

    private void OnConfirmSaveLoad()
    {
        GUICommon.Instance.PlayButtonPressSound();

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.3f, LoadSave, true);
    }

    private void OnItemDoubleClicked(SaveListItem item)
    {
        EmitSignal(nameof(OnConfirmed), item);
    }

    private void LoadSave()
    {
        if (saveToBeLoaded == null)
        {
            GD.PrintErr("Save to load is null");
            return;
        }

        SaveHelper.LoadSave(saveToBeLoaded);
        EmitSignal(nameof(OnSaveLoaded), saveToBeLoaded);
        saveToBeLoaded = null;
    }

    private void OnSaveLoadedWithoutProblems(string saveName)
    {
        EmitSignal(nameof(OnSaveLoaded), saveName);
    }
}
