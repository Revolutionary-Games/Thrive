using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Range = Godot.Range;

/// <summary>
///   A widget containing a list of saves
/// </summary>
public partial class SaveList : ScrollContainer
{
    [Export]
    public bool AutoRefreshOnFirstVisible = true;

    [Export]
    public bool AutoRefreshOnBecomingVisible = true;

    [Export]
    public bool SelectableItems;

    [Export]
    public bool LoadableItems = true;

    private readonly List<SaveListItem> saveItemChildren = [];

#pragma warning disable CA2213
    [Export]
    private Control loadingItem = null!;

    [Export]
    private Control noSavesItem = null!;

    [Export]
    private BoxContainer savesList = null!;

    [Export]
    private CustomConfirmationDialog deleteConfirmDialog = null!;

    [Export]
    private CustomConfirmationDialog loadNewerConfirmDialog = null!;

    [Export]
    private CustomConfirmationDialog loadOlderConfirmDialog = null!;

    [Export]
    private CustomConfirmationDialog loadInvalidConfirmDialog = null!;

    [Export]
    private CustomConfirmationDialog loadIncompatibleDialog = null!;

    [Export]
    private CustomConfirmationDialog upgradeSaveDialog = null!;

    [Export]
    private CustomConfirmationDialog loadIncompatiblePrototypeDialog = null!;

    [Export]
    private ErrorDialog upgradeFailedDialog = null!;

    [Export]
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

    private bool isLoadingSave;

    private int previousFirstVisible = -1;
    private int previousLastVisible = -1;
    private bool needsInitialVisibilityCheck;

    [Signal]
    public delegate void OnSelectedChangedEventHandler();

    [Signal]
    public delegate void OnItemsChangedEventHandler();

    [Signal]
    public delegate void OnConfirmedEventHandler(SaveListItem item);

    [Signal]
    public delegate void OnSaveLoadedEventHandler(string saveName);

    public override void _Ready()
    {
        listItemScene = GD.Load<PackedScene>("res://src/saving/SaveListItem.tscn");

        GetVScrollBar().Connect(Range.SignalName.ValueChanged, Callable.From<double>(_ => UpdateVisibleRange()));
        Connect(Control.SignalName.Resized, Callable.From(UpdateVisibleRange));
    }

    public override void _Process(double delta)
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

        if (needsInitialVisibilityCheck && saveItemChildren.Count > 0 && saveItemChildren[0].Size.Y > 0)
        {
            UpdateVisibleRange();
        }

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
                var item = listItemScene.Instantiate<SaveListItem>();
                item.Selectable = SelectableItems;
                item.Loadable = LoadableItems;

                if (SelectableItems)
                {
                    item.Connect(SaveListItem.SignalName.OnSelectedChanged,
                        new Callable(this, nameof(OnSubItemSelectedChanged)));
                }

                item.Connect(SaveListItem.SignalName.OnDoubleClicked, Callable.From(() => OnItemDoubleClicked(item)));

                item.Connect(SaveListItem.SignalName.OnDeleted, Callable.From(() => OnDeletePressed(save)));

                item.Connect(SaveListItem.SignalName.OnOldSaveLoaded, Callable.From(() => OnOldSaveLoaded(save)));

                // This can't use binds because we need an additional Dynamic parameter from the list item here
                item.Connect(SaveListItem.SignalName.OnUpgradeableSaveLoaded,
                    new Callable(this, nameof(OnUpgradeableSaveLoaded)));
                item.Connect(SaveListItem.SignalName.OnNewSaveLoaded, Callable.From(() => OnNewSaveLoaded(save)));
                item.Connect(SaveListItem.SignalName.OnBrokenSaveLoaded, Callable.From(() => OnInvalidLoaded(save)));
                item.Connect(SaveListItem.SignalName.OnKnownIncompatibleLoaded,
                    new Callable(this, nameof(OnKnownIncompatibleLoaded)));
                item.Connect(SaveListItem.SignalName.OnDifferentVersionPrototypeLoaded,
                    new Callable(this, nameof(OnDifferentVersionPrototypeLoaded)));
                item.Connect(SaveListItem.SignalName.OnProblemFreeSaveLoaded,
                    Callable.From(() => OnProblemFreeLoaded(save)));

                item.SaveName = save;
                savesList.AddChild(item);
                saveItemChildren.Add(item);
            }
        }
        else
        {
            noSavesItem.Visible = true;
        }

        loadingItem.Visible = false;
        refreshing = false;
        needsInitialVisibilityCheck = true;
    }

    public IEnumerable<SaveListItem> GetSelectedItems()
    {
        foreach (var child in saveItemChildren)
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

        previousFirstVisible = -1;
        previousLastVisible = -1;
        needsInitialVisibilityCheck = false;

        saveItemChildren.Clear();
        savesList.QueueFreeChildren();

        loadingItem.Visible = true;
        readSavesList = new Task<List<string>>(() => SaveHelper.CreateListOfSaves());
        TaskExecutor.Instance.AddTask(readSavesList);
        EmitSignal(SignalName.OnItemsChanged);
    }

    private void OnSubItemSelectedChanged()
    {
        EmitSignal(SignalName.OnSelectedChanged);
    }

    private void OnDeletePressed(string saveName)
    {
        GUICommon.Instance.PlayButtonPressSound();

        saveToBeDeleted = saveName;

        // Deleting this save cannot be undone, are you sure you want to permanently delete {0}?
        deleteConfirmDialog.DialogText = Localization.Translate("SAVE_DELETE_WARNING").FormatSafe(saveName);
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
        EmitSignal(SignalName.OnItemsChanged);
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

    private void OnProblemFreeLoaded(string saveName)
    {
        saveToBeLoaded = saveName;
        StartLoadTransition();
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

        var timer = new Stopwatch();
        timer.Start();

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

        GD.Print($"Total time save upgrade took: {timer.Elapsed}");

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

        StartLoadTransition();
    }

    private void StartLoadTransition()
    {
        // If a load is already queued, don't queue another one
        if (isLoadingSave)
            return;

        isLoadingSave = true;

        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.3f, LoadSave, true);
    }

    private void OnItemDoubleClicked(SaveListItem item)
    {
        EmitSignal(SignalName.OnConfirmed, item);
    }

    private void LoadSave()
    {
        if (saveToBeLoaded == null)
        {
            GD.PrintErr("Save to load is null");
            return;
        }

        SaveHelper.LoadSave(saveToBeLoaded);

        EmitSignal(SignalName.OnSaveLoaded, saveToBeLoaded);
        saveToBeLoaded = null;
        isLoadingSave = false;
    }

    private void UpdateVisibleRange()
    {
        if (!IsVisibleInTree())
            return;

        // This is reset here to ensure that if this were to somehow refresh while invisible, then the item visibility
        // would be refreshed once this becomes visible.
        needsInitialVisibilityCheck = false;

        int itemCount = saveItemChildren.Count;
        if (itemCount == 0)
            return;

        float scrollTop = ScrollVertical;
        float scrollBottom = scrollTop + Size.Y;

        int first = -1;
        int last = -1;
        for (int i = 0; i < itemCount; ++i)
        {
            var item = saveItemChildren[i];
            float itemTop = item.Position.Y;
            float itemBottom = itemTop + item.Size.Y;

            if (itemBottom > scrollTop && itemTop < scrollBottom)
            {
                if (first < 0)
                    first = i;

                last = i;
            }
            else if (first >= 0)
            {
                break;
            }
        }

        if (first < 0)
            return;

        int paddedFirst = Math.Max(0, first - Constants.SAVE_LIST_LAZY_LOAD_PADDING);
        int paddedLast = Math.Min(itemCount - 1, last + Constants.SAVE_LIST_LAZY_LOAD_PADDING);
        if (paddedFirst == previousFirstVisible && paddedLast == previousLastVisible)
            return;

        int oldFirst = previousFirstVisible;
        int oldLast = previousLastVisible;

        previousFirstVisible = paddedFirst;
        previousLastVisible = paddedLast;

        for (int i = paddedFirst; i <= paddedLast; ++i)
        {
            saveItemChildren[i].TriggerLoad();
        }

        if (oldFirst >= 0)
        {
            for (int i = oldFirst; i < paddedFirst; ++i)
            {
                saveItemChildren[i].CancelLoad();
            }

            for (int i = paddedLast + 1; i <= oldLast; ++i)
            {
                saveItemChildren[i].CancelLoad();
            }
        }
    }
}
