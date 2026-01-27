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
    private const int DefaultMaxVisiblePanels = 50;

    private readonly List<RichTextLabel> entryLabels = [];

    // This is a local history for private debug messages.
    private readonly Deque<DebugEntry> privateHistory = [];

    private Callable onResizedCallable;
    private Callable onScrolledCallable;

    private int lastIdLoaded;

#pragma warning disable CA2213
    [Export]
    private VScrollBar scrollBar = null!;
#pragma warning restore CA2213

    [Export]
    private int entrySeparation = 3;

    [Export]
    private int leftMargin = 3;

    [Export]
    private int rightMargin = 3;

    [Export(PropertyHint.Range, "10, 100")]
    private int maxVisiblePanels = DefaultMaxVisiblePanels;

    private bool dirty;

    public override void _Ready()
    {
        onResizedCallable = new Callable(this, nameof(OnResized));
        onScrolledCallable = new Callable(this, nameof(OnScrolled));

        Connect(Control.SignalName.Resized, onResizedCallable);
        scrollBar.Connect(ScrollBar.SignalName.Scrolling, onScrolledCallable);

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
                    break;
                case MouseButton.WheelDown:
                    scrollBar.Value += 1;
                    break;
            }
        }

        OnScrolled();

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

    public int LoadFrom(int visualSkipCount, int globalStartId)
    {
        var history = DebugConsoleManager.Instance.History;
        int maxGlobalId = history.Count;

        long minTimestamp = 0;
        if (globalStartId < maxGlobalId && globalStartId >= 0)
        {
            minTimestamp = history[globalStartId].BeginTimestamp;
        }

        int currentGlobalId = globalStartId;
        int currentLocalIndex = 0;

        while (currentLocalIndex < privateHistory.Count &&
               privateHistory[currentLocalIndex].BeginTimestamp < minTimestamp)
        {
            ++currentLocalIndex;
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
                if (privateHistory[currentLocalIndex].BeginTimestamp < history[currentGlobalId].BeginTimestamp)
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
                if (privateHistory[currentLocalIndex].BeginTimestamp < history[currentGlobalId].BeginTimestamp)
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
                currentDebugEntry = history[currentGlobalId];
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

        dirty = true;

        lastIdLoaded = visualSkipCount;
        return entryPanelIndex;
    }

    public void Refresh()
    {
        LoadFrom(lastIdLoaded, 0);
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
        int visibleEntries = LayOutEntriesFrom(0);

        LayOutScrollbar();

        var history = DebugConsoleManager.Instance.History;

        long minTimestamp = 0;
        if (history.Count > 0)
        {
            minTimestamp = history[0].BeginTimestamp;
        }

        int validPrivateCount = GetCountNewerThan(minTimestamp);

        scrollBar.SetMax(history.Count + validPrivateCount);
        scrollBar.SetPage(visibleEntries);
    }

    private int LayOutEntriesFrom(int id)
    {
        int visibleEntries = 0;
        int previousLabelY = 0, previousLabelH = 0;
        for (int i = id; i < entryLabels.Count; ++i)
        {
            if (i != 0)
            {
                var previousLabel = entryLabels[i - 1];

                previousLabelY = (int)previousLabel.Position.Y;
                previousLabelH = previousLabel.GetContentHeight();
            }

            var label = entryLabels[i];

            // We reached the end of the visible labels. Nothing more to lay out.
            if (label.Text == string.Empty)
                break;

            float x = leftMargin;
            float y = previousLabelY + previousLabelH + entrySeparation;
            float w = Size.X - leftMargin - 2 * rightMargin - scrollBar.Size.X;

            label.Position = new Vector2(x, y);

            // Ensure the label fills the control, as it doesn't resize on Y automatically even with FitContent enabled.
            label.CustomMinimumSize = new Vector2(w, 0);
            label.Size = new Vector2(w, 0);

            if (y > Size.Y)
            {
                label.Visible = false;
            }
            else
            {
                label.Visible = true;
                if (Size.Y - y > label.GetContentHeight())
                    ++visibleEntries;
            }
        }

        return visibleEntries;
    }

    private void LayOutScrollbar()
    {
        var w = Math.Min(10, scrollBar.Size.X);

        scrollBar.Position = new Vector2(Size.X - w + rightMargin, entrySeparation);
        scrollBar.Size = new Vector2(w, Size.Y - 2 * entrySeparation);
    }

    private void OnResized()
    {
        dirty = true;
    }

    private void OnScrolled()
    {
        lastIdLoaded = (int)scrollBar.Value;

        Refresh();
    }
}
