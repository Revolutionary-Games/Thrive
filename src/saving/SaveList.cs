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

    [Export]
    private bool isOpenedInAutoEvoTool;

    private PackedScene listItemScene = null!;
#pragma warning restore CA2213

    private List<string>? pendingSaveNames;
    private bool refreshedAtLeastOnce;

#pragma warning disable CA2213 // Task is disposed after its result is consumed
    private Task<List<string>>? readSavesList;
#pragma warning restore CA2213

    private string? saveToBeDeleted;
    private string? saveToBeLoaded;
    private bool suppressSaveUpgradeClose;

    private bool wasVisible;
    private bool incompatibleIfNotUpgraded;

    private bool isLoadingSave;

    private int loadingPreviewIndex = -1;
    private int nextPreviewIndex;
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
        if (!isCurrentlyVisible)
        {
            if (wasVisible)
            {
                wasVisible = false;
                CancelPreviewLoad();
                nextPreviewIndex = Math.Max(0, previousFirstVisible);
                needsInitialVisibilityCheck = true;
            }

            return;
        }

        if ((AutoRefreshOnFirstVisible && !refreshedAtLeastOnce) || (AutoRefreshOnBecomingVisible && !wasVisible))
        {
            wasVisible = true;
            Refresh();
            return;
        }

        wasVisible = true;

        if (readSavesList != null)
        {
            ProcessRefresh();
            if (readSavesList != null)
                return;
        }

        if (needsInitialVisibilityCheck && saveItemChildren.Count > 0 && saveItemChildren[0].Size.Y > 0)
            UpdateVisibleRange();

        ProcessPreviewLoad();

        if (pendingSaveNames != null)
        {
            AddSaveItem(pendingSaveNames[saveItemChildren.Count]);

            // Recheck after the container has laid out the new row
            needsInitialVisibilityCheck = true;
            if (saveItemChildren.Count == pendingSaveNames.Count)
                pendingSaveNames = null;
        }
    }

    public IEnumerable<SaveListItem> GetSelectedItems()
    {
        if (readSavesList != null)
            yield break;

        foreach (var child in saveItemChildren)
        {
            if (child.Selectable && child.Selected)
                yield return child;
        }
    }

    public void Refresh()
    {
        if (readSavesList != null)
            return;

        refreshedAtLeastOnce = true;
        pendingSaveNames = null;

        CancelPreviewLoad();
        nextPreviewIndex = 0;
        previousFirstVisible = -1;
        previousLastVisible = -1;
        needsInitialVisibilityCheck = false;

        savesList.Hide();
        noSavesItem.Hide();
        loadingItem.Show();

        readSavesList = new Task<List<string>>(() => SaveHelper.CreateListOfSaves());
        TaskExecutor.Instance.AddTask(readSavesList);
        EmitSignal(SignalName.OnItemsChanged);
    }

    private void ProcessRefresh()
    {
        int released = 0;
        while (saveItemChildren.Count > 0)
        {
            // Detaching large numbers of old rows can also stall the frame when reopening the list
            // so do a few at a time until they are all done.
            if (++released > 5)
            {
                return;
            }

            var index = saveItemChildren.Count - 1;
            var item = saveItemChildren[index];
            saveItemChildren.RemoveAt(index);
            item.DetachAndQueueFree();
        }

        if (!readSavesList!.IsCompleted)
            return;

        // TaskExecutor reports failures, so finish the refresh with an empty list if the scan failed
        if (readSavesList.IsCompletedSuccessfully && readSavesList.Result.Count > 0)
            pendingSaveNames = readSavesList.Result;

        readSavesList.Dispose();
        readSavesList = null;
        loadingItem.Hide();
        noSavesItem.Visible = pendingSaveNames == null;
        savesList.Show();
    }

    private void AddSaveItem(string save)
    {
        var item = listItemScene.Instantiate<SaveListItem>();
        item.Selectable = SelectableItems;
        item.Loadable = LoadableItems;

        if (SelectableItems)
        {
            item.Connect(SaveListItem.SignalName.OnSelectedChanged,
                new Callable(this, nameof(OnSubItemSelectedChanged)));
        }

        item.Connect(SaveListItem.SignalName.OnDoubleClicked,
            Callable.From(() => OnItemDoubleClicked(item)));

        item.Connect(SaveListItem.SignalName.OnDeleted, Callable.From(() => OnDeletePressed(save)));

        item.Connect(SaveListItem.SignalName.OnOldSaveLoaded, Callable.From(() => OnOldSaveLoaded(save)));

        // This can't use binds because we need an additional Dynamic parameter from the list item here
        item.Connect(SaveListItem.SignalName.OnUpgradeableSaveLoaded,
            new Callable(this, nameof(OnUpgradeableSaveLoaded)));
        item.Connect(SaveListItem.SignalName.OnNewSaveLoaded, Callable.From(() => OnNewSaveLoaded(save)));
        item.Connect(SaveListItem.SignalName.OnBrokenSaveLoaded,
            Callable.From(() => OnInvalidLoaded(save)));
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

    private void ProcessPreviewLoad()
    {
        if (loadingPreviewIndex >= 0 && saveItemChildren[loadingPreviewIndex].IsLoadingData)
            return;

        loadingPreviewIndex = -1;

        while (nextPreviewIndex <= previousLastVisible)
        {
            var index = nextPreviewIndex++;
            if (saveItemChildren[index].TriggerLoad())
            {
                loadingPreviewIndex = index;
                return;
            }
        }
    }

    private void CancelPreviewLoad()
    {
        if (loadingPreviewIndex < 0)
            return;

        saveItemChildren[loadingPreviewIndex].CancelLoad();
        loadingPreviewIndex = -1;
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

        if (isOpenedInAutoEvoTool)
        {
            EmitSignal(SignalName.OnSaveLoaded, saveToBeLoaded);
            return;
        }

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

        if (isOpenedInAutoEvoTool)
        {
            if (saveToBeLoaded == null)
            {
                GD.PrintErr("Save to load is null");
                return;
            }

            EmitSignal(SignalName.OnSaveLoaded, saveToBeLoaded);
        }
        else
        {
            StartLoadTransition();
        }
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
        if (readSavesList != null || !savesList.IsVisibleInTree())
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

        previousFirstVisible = paddedFirst;
        previousLastVisible = paddedLast;
        nextPreviewIndex = paddedFirst;

        if (loadingPreviewIndex < paddedFirst || loadingPreviewIndex > paddedLast)
            CancelPreviewLoad();
    }
}
