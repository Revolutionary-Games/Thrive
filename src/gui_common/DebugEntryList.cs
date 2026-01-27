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

    private int lastIdLoaded;

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

        Connect(Control.SignalName.Resized, onResizedCallable);

        base._Ready();
    }

    public override void _Process(double delta)
    {
        if (dirty)
            RecalculateLayoutFrom(0);

        dirty = false;

        base._Process(delta);
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

        GD.Print($"☺ {currentGlobalId} {maxGlobalId}");

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

            if (currentLabel.Size.Y > 0 && currentLabel.Position.Y + currentLabel.Size.Y > Size.Y)
                break;

            ++entryPanelIndex;
        }

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

    private void RecalculateLayoutFrom(int id)
    {
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

            float x = leftMargin;
            float y = previousLabelY + previousLabelH + entrySeparation;
            float w = Size.X - leftMargin - rightMargin;

            label.Position = new Vector2(x, y);

            // Ensure the label fills the control, as it doesn't resize on Y automatically even with FitContent enabled.
            label.CustomMinimumSize = new Vector2(w, 0);
        }
    }

    private void OnResized()
    {
        dirty = true;
    }
}
