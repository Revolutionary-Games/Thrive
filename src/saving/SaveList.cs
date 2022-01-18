using System;
using System.Collections.Generic;
using System.Globalization;
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
    public NodePath LoadingItemPath;

    [Export]
    public NodePath NoSavesItemPath;

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

    [Export]
    public NodePath UpgradeSaveDialogPath;

    [Export]
    public NodePath UpgradeFailedDialogPath;

    private Control loadingItem;
    private Control noSavesItem;
    private BoxContainer savesList;
    private CustomConfirmationDialog deleteConfirmDialog;
    private CustomConfirmationDialog loadNewerConfirmDialog;
    private CustomConfirmationDialog loadOlderConfirmDialog;
    private CustomConfirmationDialog loadInvalidConfirmDialog;
    private CustomConfirmationDialog loadIncompatibleDialog;
    private CustomConfirmationDialog upgradeSaveDialog;
    private ErrorDialog upgradeFailedDialog;

    private PackedScene listItemScene;

    private bool refreshing;
    private bool refreshedAtLeastOnce;

    private Task<List<string>> readSavesList;

    private string saveToBeDeleted;
    private string saveToBeLoaded;
    private bool suppressSaveUpgradeClose;

    private bool wasVisible;
    private bool incompatibleIfNotUpgraded;

    [Signal]
    public delegate void OnSelectedChanged();

    [Signal]
    public delegate void OnItemsChanged();

    [Signal]
    public delegate void OnConfirmed(SaveListItem item);

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
        deleteConfirmDialog.PopupCenteredShrink();
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

        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeOut, 0.3f, true);
        TransitionManager.Instance.StartTransitions(this, nameof(LoadSave));
    }

    private void OnItemDoubleClicked(SaveListItem item)
    {
        EmitSignal(nameof(OnConfirmed), item);
    }

    private void LoadSave()
    {
        SaveHelper.LoadSave(saveToBeLoaded);
        saveToBeLoaded = null;
    }
}
