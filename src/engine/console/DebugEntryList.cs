using System;
using System.Collections.Generic;
using Godot;
using Nito.Collections;

/// <summary>
///   A container for DebugEntryPanels
/// </summary>
public partial class DebugEntryList : Control
{
    private const uint MaxPrivateHistorySize = 32;
    private const int DefaultMaxVisiblePanels = 32;

    private readonly List<RichTextLabel> entryLabels = [];

    /// <summary>
    ///   This is a local history for private debug messages.
    /// </summary>
    private readonly Deque<DebugEntry> privateHistory = [];

    private Callable onResizedCallable;

    private int lastIdLoaded;

#pragma warning disable CA2213
    [Export]
    private VScrollBar scrollBar = null!;
#pragma warning restore CA2213

    [Export]
    private int entrySeparation = 2;

    [Export]
    private int leftMargin = 3;

    [Export]
    private int rightMargin = 3;

    [Export(PropertyHint.Range, "10, 100")]
    private int maxVisiblePanels = DefaultMaxVisiblePanels;

    private bool dirty;

    private int globalStartId;

    /// <summary>
    ///   This determines whether the scrollbar should stick its value to the bottom of the list.
    /// </summary>
    public bool StickToBottom { get; set; } = true;

    public override void _Ready()
    {
        onResizedCallable = new Callable(this, nameof(MarkDirty));
        Connect(Control.SignalName.Resized, new Callable(this, nameof(OnResized)));

        base._Ready();
    }

    public override void _Process(double delta)
    {
        if (dirty)
            LayOut();

        dirty = false;

        base._Process(delta);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            switch (mouseEvent.ButtonIndex)
            {
                case MouseButton.WheelUp:
                    scrollBar.Value -= 1;
                    OnScrolled();
                    break;
                case MouseButton.WheelDown:
                    scrollBar.Value += 1;
                    OnScrolled();
                    break;
            }
        }

        base._Input(@event);
    }

    public int GetPrivateCount()
    {
        return privateHistory.Count;
    }

    public int GetCountNewerThan(long minTimestamp)
    {
        int count = 0;
        foreach (var entry in privateHistory)
        {
            if (entry.BeginTimestamp >= minTimestamp)
                ++count;
        }

        return count;
    }

    public int LoadFrom(int visualSkipCount)
    {
        var debugConsoleManager = DebugConsoleManager.Instance;
        int maxGlobalId = debugConsoleManager.MessageCountInHistory;

        long minTimestamp = 0;
        if (globalStartId < maxGlobalId && globalStartId >= 1)
        {
            minTimestamp = debugConsoleManager.GetMessageAt(globalStartId - 1).BeginTimestamp;
        }

        int currentGlobalId = globalStartId;
        int currentLocalIndex = 0;

        if (minTimestamp > 0)
        {
            while (currentLocalIndex < privateHistory.Count &&
                   privateHistory[currentLocalIndex].BeginTimestamp < minTimestamp)
            {
                ++currentLocalIndex;
            }
        }

        int currentVisualIndex = 0;
        while (currentVisualIndex < visualSkipCount)
        {
            if (currentGlobalId >= maxGlobalId && currentLocalIndex >= privateHistory.Count)
                break;

            bool skipLocal = false;
            if (currentGlobalId >= maxGlobalId)
            {
                skipLocal = true;
            }
            else if (currentLocalIndex < privateHistory.Count)
            {
                if (privateHistory[currentLocalIndex].BeginTimestamp <
                    debugConsoleManager.GetMessageAt(currentGlobalId).BeginTimestamp)
                {
                    skipLocal = true;
                }
            }

            if (skipLocal)
            {
                ++currentLocalIndex;
            }
            else
            {
                ++currentGlobalId;
            }

            ++currentVisualIndex;
        }

        // Rendering
        int entryPanelIndex = 0;
        while (currentGlobalId < maxGlobalId || currentLocalIndex < privateHistory.Count)
        {
            if (entryPanelIndex >= maxVisiblePanels)
                break;

            RichTextLabel currentLabel;
            if (entryLabels.Count == entryPanelIndex)
            {
                var newLabel = new RichTextLabel();
                newLabel.FitContent = true;
                newLabel.BbcodeEnabled = true;
                newLabel.SizeFlagsVertical = SizeFlags.ShrinkBegin;
                newLabel.SetScrollActive(true);

                entryLabels.Add(newLabel);
                AddChild(newLabel);

                newLabel.Connect(Control.SignalName.Resized, onResizedCallable);

                currentLabel = newLabel;
            }
            else
            {
                currentLabel = entryLabels[entryPanelIndex];
            }

            bool useLocal = false;
            if (currentGlobalId >= maxGlobalId)
            {
                useLocal = true;
            }
            else if (currentLocalIndex < privateHistory.Count)
            {
                if (privateHistory[currentLocalIndex].BeginTimestamp <
                    debugConsoleManager.GetMessageAt(currentGlobalId).BeginTimestamp)
                {
                    useLocal = true;
                }
            }

            DebugEntry currentDebugEntry;
            if (useLocal)
            {
                currentDebugEntry = privateHistory[currentLocalIndex];
                ++currentLocalIndex;
            }
            else
            {
                currentDebugEntry = debugConsoleManager.GetMessageAt(currentGlobalId);
                ++currentGlobalId;
            }

            currentDebugEntry.Update();

            currentLabel.Text = currentDebugEntry.Text;
            currentLabel.Visible = true;

            ++entryPanelIndex;
        }

        // Hide the remaining labels.
        // We don't set Visible to false here, because the visibility can be reset to true in LayOutEntriesFrom, causing
        // the rendering of duplicates. Instead, we empty the text completely.
        for (int i = entryPanelIndex; i < entryLabels.Count; ++i)
            entryLabels[i].Text = string.Empty;

        MarkDirty();

        lastIdLoaded = visualSkipCount;
        return entryPanelIndex;
    }

    public void Refresh()
    {
        LoadFrom(lastIdLoaded);
    }

    public void Clear()
    {
        globalStartId = DebugConsoleManager.Instance.MessageCountInHistory;
        privateHistory.Clear();

        foreach (var label in entryLabels)
        {
            label.Text = string.Empty;
            label.Visible = false;
        }

        scrollBar.Value = 0;
        lastIdLoaded = 0;
    }

    public void AddPrivateEntry(DebugEntry entry)
    {
        privateHistory.AddToBack(entry);

        if (privateHistory.Count > MaxPrivateHistorySize)
            privateHistory.RemoveFromFront();

        Refresh();
    }

    private void LayOut()
    {
        LayOutScrollbar();

        bool stickCheck = true;
        while (true)
        {
            int visibleEntries = LayOutEntriesFrom(0);

            if (entryLabels.Count > 0 && visibleEntries == entryLabels.Count)
            {
                var lastLabel = entryLabels[^1];
                float listBottom = lastLabel.Position.Y + lastLabel.Size.Y;

                if (listBottom < Size.Y)
                {
                    float remainingSpace = Size.Y - listBottom;
                    float avgHeight = Math.Max(10, lastLabel.Size.Y + entrySeparation);
                    int phantomItems = (int)(remainingSpace / avgHeight);

                    visibleEntries += phantomItems;
                }
            }
            else if (entryLabels.Count == 0 && Size.Y > 0)
            {
                visibleEntries = (int)(Size.Y / 20.0f);
            }

            var debugConsoleManager = DebugConsoleManager.Instance;

            long minTimestamp = 0;
            if (debugConsoleManager.MessageCountInHistory - globalStartId > 0)
            {
                minTimestamp = debugConsoleManager.GetMessageAt(globalStartId).BeginTimestamp;
            }

            int validPrivateCount = GetCountNewerThan(minTimestamp);

            scrollBar.SetMax(debugConsoleManager.MessageCountInHistory - globalStartId + validPrivateCount);
            scrollBar.SetPage(visibleEntries);

            if (!stickCheck)
                break;

            if (StickToBottom)
            {
                double targetValue = scrollBar.MaxValue - scrollBar.Page;

                if (Math.Abs(scrollBar.Value - targetValue) > 0.1)
                {
                    scrollBar.Value = targetValue;
                    stickCheck = false;
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
    }

    private int LayOutEntriesFrom(int id)
    {
        if (entryLabels.Count == 0 || id >= entryLabels.Count)
            return 0;

        int visibleEntries = 0;
        float w = Size.X - leftMargin - 2 * rightMargin - scrollBar.Size.X;

        float currentY = entrySeparation;

        for (int i = id; i < entryLabels.Count; ++i)
        {
            var label = entryLabels[i];

            if (label.Text == string.Empty)
                break;

            if (i > id)
            {
                var previousLabel = entryLabels[i - 1];
                currentY += previousLabel.GetContentHeight() + entrySeparation;
            }

            label.Position = new Vector2(leftMargin, currentY);

            label.CustomMinimumSize = new Vector2(w, 0);
            label.Size = new Vector2(w, 0);

            float labelHeight = label.GetContentHeight();

            if (currentY >= Size.Y)
            {
                label.Visible = false;
            }
            else
            {
                label.Visible = true;

                if (currentY + labelHeight <= Size.Y || i == id)
                {
                    ++visibleEntries;
                }
            }
        }

        return visibleEntries;
    }

    private void LayOutScrollbar()
    {
        var w = Math.Min(10, scrollBar.Size.X);

        scrollBar.Position = new Vector2(Size.X - w, entrySeparation);
        scrollBar.Size = new Vector2(w, Size.Y - 2 * entrySeparation);
    }

    private void OnResized()
    {
        bool boundsStick = false;
        if (entryLabels.Count > 0)
        {
            var lastEntryLabel = entryLabels[^1];
            boundsStick = entryLabels.Count > 0 && lastEntryLabel.Position.Y + lastEntryLabel.Size.Y > Size.Y;
        }

        StickToBottom = scrollBar.MaxValue - scrollBar.Value - scrollBar.Page < 0.1f || boundsStick;

        Refresh();
    }

    private void OnScrolled()
    {
        lastIdLoaded = (int)scrollBar.Value;

        // The scrollbar should stick to the bottom of the list if its page-adjusted value is greater than the
        // maximum.
        StickToBottom = scrollBar.MaxValue - scrollBar.Value - scrollBar.Page < 0.1f;

        Refresh();
    }

    private void MarkDirty()
    {
        dirty = true;
    }
}
