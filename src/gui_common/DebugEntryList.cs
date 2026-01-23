using System.Collections.Generic;
using Godot;
using Nito.Collections;

/// <summary>
///   A container for DebugEntryPanels
/// </summary>
public partial class DebugEntryList : Panel
{
    public const uint MaxPrivateHistorySize = 32;
    private const int MaxVisiblePanels = 50;

    private static PackedScene? debugEntryPanelScene;

    private readonly List<DebugEntryPanel> entryPanels = [];

    // This is a local history for private debug messages.
    private readonly Deque<DebugEntry> privateHistory = [];

    private int lastIdLoaded;

#pragma warning disable CA2213
    [Export]
    private VBoxContainer entryPanelsContainer = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        debugEntryPanelScene ??= SceneManager.Instance.LoadScene("res://src/gui_common/DebugEntryPanel.tscn");
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        debugEntryPanelScene?.Dispose();
        debugEntryPanelScene = null;
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
                count++;
        }

        return count;
    }

    public int LoadFrom(int visualSkipCount, int globalStartId)
    {
        if (debugEntryPanelScene == null)
            return 0;

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
            currentLocalIndex++;
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
                currentLocalIndex++;
            }
            else
            {
                currentGlobalId++;
            }

            currentVisualIndex++;
        }

        // Rendering
        int entryPanelIndex = 0;
        while (currentGlobalId < maxGlobalId || currentLocalIndex < privateHistory.Count)
        {
            if (entryPanelIndex >= MaxVisiblePanels)
                break;

            DebugEntryPanel currentPanel;
            if (entryPanels.Count == entryPanelIndex)
            {
                var newPanel = debugEntryPanelScene.Instantiate<DebugEntryPanel>();
                entryPanels.Add(newPanel);
                entryPanelsContainer.AddChild(newPanel);
                currentPanel = newPanel;
            }
            else
            {
                currentPanel = entryPanels[entryPanelIndex];
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
                currentLocalIndex++;
            }
            else
            {
                currentDebugEntry = history[currentGlobalId];
                currentGlobalId++;
            }

            currentDebugEntry.Update();
            currentPanel.CurrentDebugEntry = currentDebugEntry;
            currentPanel.Visible = true;

            if (currentPanel.Size.Y > 0 &&
                currentPanel.Position.Y + currentPanel.Size.Y > Size.Y)
            {
                break;
            }

            entryPanelIndex++;
        }

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
}
