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
    private VScrollBar vScrollBar = null!;

    [Export]
    private HScrollBar hScrollBar = null!;

    [Export]
    private Control textClipArea = null!;

    [Export]
    private Font monospacedFont = null!;
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
    private bool shiftDown;
    private int globalStartId;

    /// <summary>
    ///   This determines whether the scrollbar should stick its value to the bottom of the list.
    /// </summary>
    public bool StickToBottom
    {
        get;
        set
        {
            field = value;
            Refresh();
        }
    }

    public override void _Ready()
    {
        onResizedCallable = new Callable(this, nameof(OnResized));
        Connect(Control.SignalName.Resized, onResizedCallable);

        StickToBottom = true;

        base._Ready();
    }

    public override void _Process(double delta)
    {
        if (dirty)
            LayOut();

        dirty = false;

        base._Process(delta);
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton mouseEvent:
            {
                ScrollBar bar = shiftDown ? hScrollBar : vScrollBar;
                float speedMultiplier = shiftDown ? 20.0f : 1.0f;
                switch (mouseEvent.ButtonIndex)
                {
                    case MouseButton.WheelUp:
                        bar.Value -= 1.0f * speedMultiplier;
                        OnScrolled();
                        break;
                    case MouseButton.WheelDown:
                        bar.Value += 1.0f * speedMultiplier;
                        OnScrolled();
                        break;
                }

                break;
            }

            case InputEventKey { Keycode: Key.Shift } keyEvent:
                shiftDown = keyEvent.Pressed;
                break;
        }

        GetViewport().SetInputAsHandled();

        base._GuiInput(@event);
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
                newLabel.AutowrapMode = TextServer.AutowrapMode.Off;
                newLabel.AddThemeFontOverride("normal_font", monospacedFont);
                newLabel.AddThemeFontSizeOverride("normal_font", 12);

                entryLabels.Add(newLabel);
                textClipArea.AddChild(newLabel);

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

        vScrollBar.Value = 0;
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
        LayOutScrollbars();
        LayOutClipArea();

        bool stickCheck = true;
        while (true)
        {
            int visibleEntries = LayOutEntriesFrom(0);

            var debugConsoleManager = DebugConsoleManager.Instance;

            long minTimestamp = 0;
            if (debugConsoleManager.MessageCountInHistory - globalStartId > 0)
            {
                minTimestamp = debugConsoleManager.GetMessageAt(globalStartId).BeginTimestamp;
            }

            int validPrivateCount = GetCountNewerThan(minTimestamp);

            vScrollBar.SetMax(debugConsoleManager.MessageCountInHistory - globalStartId + validPrivateCount);
            vScrollBar.SetPage(visibleEntries);
            vScrollBar.Visible = Math.Abs(vScrollBar.MaxValue - vScrollBar.Page) >= 0.1f;

            if (!stickCheck)
                break;

            if (StickToBottom)
            {
                vScrollBar.Value = vScrollBar.MaxValue - vScrollBar.Page;
                stickCheck = false;
            }
            else
            {
                break;
            }
        }
    }

    private int LayOutEntriesFrom(int id)
    {
        int visibleEntries;
        int idOffset = 0;
        int maxWidth = 0;
        bool retryLayout;

        if (entryLabels.Count == 0 || id >= entryLabels.Count)
            return 0;

        do
        {
            retryLayout = false;
            visibleEntries = 0;

            for (int i = id + idOffset; i < entryLabels.Count; ++i)
            {
                int previousLabelY;
                int previousLabelH;

                if (i > id + idOffset)
                {
                    var previousLabel = entryLabels[i - 1];
                    previousLabelY = (int)previousLabel.Position.Y;
                    previousLabelH = previousLabel.GetContentHeight();
                }
                else
                {
                    // First item of this pass: Reset the anchor to the top
                    previousLabelY = 0;
                    previousLabelH = 0;
                }

                var label = entryLabels[i];

                // We reached the end of the visible labels. Nothing more to lay out.
                if (label.Text == string.Empty)
                    break;

                float x = leftMargin - (float)hScrollBar.Value;
                float y = previousLabelY + previousLabelH + entrySeparation;

                label.Position = new Vector2(x, y);

                int contentWidth = label.GetContentWidth();
                if (contentWidth > maxWidth)
                    maxWidth = contentWidth;

                if (y > textClipArea.Size.Y)
                {
                    if (StickToBottom)
                    {
                        // We are not sticking to the bottom as requested, yet we still have some non-empty labels to
                        // lay out. So, we increment the offset and retry the whole process.

                        retryLayout = true;
                        ++idOffset;
                        break;
                    }

                    label.Visible = false;
                }
                else
                {
                    label.Visible = true;
                    if (textClipArea.Size.Y - y > label.GetContentHeight())
                    {
                        ++visibleEntries;
                    }
                    else if (StickToBottom)
                    {
                        // The last entry is not fully visible, but we should stick to the bottom. We must retry to lay
                        // out and eventually have this be fully visible.

                        retryLayout = true;
                        ++idOffset;
                        break;
                    }
                }
            }

            // This acts as a safety watchdog to prevent an infinite loop, but it should never be true if the algorithm
            // above is correct.
            if (id + idOffset >= entryLabels.Count)
                throw new Exception("Debug entry layout has failed due to a logical bug.");

            // Hide the previous label we don't need anymore if recalculating the layout.
            if (retryLayout)
                entryLabels[id + idOffset - 1].Visible = false;
        }
        while (retryLayout);

        hScrollBar.MaxValue = maxWidth;
        hScrollBar.Page = Size.X - leftMargin - rightMargin - vScrollBar.Size.X;
        hScrollBar.Visible = Math.Abs(hScrollBar.MaxValue - hScrollBar.Page) >= 0.1f;

        return visibleEntries;
    }

    private void LayOutScrollbars()
    {
        var w = Math.Min(10, vScrollBar.Size.X);
        var h = Math.Min(10, hScrollBar.Size.Y);

        vScrollBar.Position = new Vector2(Size.X - w, entrySeparation);
        vScrollBar.Size = new Vector2(w, Size.Y - 2 * entrySeparation);

        hScrollBar.Position = new Vector2(leftMargin, Size.Y - h);
        hScrollBar.Size = new Vector2(Size.X - leftMargin - rightMargin - vScrollBar.Size.X, h);
    }

    private void LayOutClipArea()
    {
        textClipArea.Position = new Vector2(leftMargin, entrySeparation);
        textClipArea.Size = new Vector2(Size.X - vScrollBar.Size.X - leftMargin - 2 * rightMargin,
            Size.Y - hScrollBar.Size.Y - 3 * entrySeparation);
    }

    private void OnResized()
    {
        dirty = true;
    }

    private void OnScrolled()
    {
        lastIdLoaded = (int)vScrollBar.Value;

        // The scrollbar should stick to the bottom of the list if its page-adjusted value is greater than the
        // maximum.
        StickToBottom = vScrollBar.MaxValue - vScrollBar.Value - vScrollBar.Page < 0.1f;

        Refresh();
    }
}
