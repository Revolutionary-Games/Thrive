using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Nito.Collections;

/// <summary>
///   Handles the debug console
/// </summary>
public partial class DebugConsole : CustomWindow, ICommandInvoker
{
    private const uint MaxPrivateHistorySize = 32;

    /// <summary>
    ///   This is a local history for private debug messages.
    /// </summary>
    private readonly Deque<DebugEntry> privateHistory = [];

    private readonly Deque<EntryView> debugEntryLabels = [];
    private readonly HashSet<EntryView> liveEntries = [];

    private readonly StringName normalFontName = new("normal_font");

    private bool stickToBottom = true;

#pragma warning disable CA2213
    [Export]
    private VBoxContainer debugEntryList = null!;

    [Export]
    private CommandInput commandInput = null!;

    [Export]
    private ScrollContainer scrollContainer = null!;

    [Export]
    private Font font = null!;
#pragma warning restore CA2213

    public CommandHistory CommandHistory { get; } = new();

    public bool IsConsoleOpen
    {
        get => Visible;
        set
        {
            if (value)
            {
                Show();
                Activate();
            }
            else
            {
                Hide();
            }
        }
    }

    public override void _Ready()
    {
        scrollContainer.GetVScrollBar()
            .Connect(ScrollBar.SignalName.Scrolling, new Callable(this, nameof(OnScrolled)));

        // Make backgrounds more transparent
        var customPanelOverride = (StyleBoxFlat)PanelStyle.Duplicate(false);
        var color = customPanelOverride.BgColor;
        color.A = 0.48f;
        customPanelOverride.BgColor = color;
        PanelStyle = customPanelOverride;

        var titlebarPanelOverride = (StyleBoxFlat)TitleBarPanelStyle.Duplicate(false);
        color = titlebarPanelOverride.BgColor;
        color.A = 0.60f;
        titlebarPanelOverride.BgColor = color;
        TitleBarPanelStyle = titlebarPanelOverride;

        commandInput.CommandHistory = CommandHistory;

        base._Ready();
    }

    public override void _ExitTree()
    {
        // Make sure we unsubscribe from the event handler.
        IsConsoleOpen = false;
        Clear();

        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        UpdateLiveEntries();
        UpdateAutoscroll();

        base._Process(delta);
    }

    public void Clear()
    {
        privateHistory.Clear();

        foreach (var node in debugEntryList.GetChildren())
        {
            if (node is RichTextLabel label)
                label.Visible = false;
        }
    }

    public void Print(DebugConsoleManager.RawDebugEntry entry)
    {
        var debugEntryFactory = DebugConsoleManager.Instance.DebugEntryFactory;

        debugEntryFactory.TryAddMessage(entry.Id, entry, DebugEntryFactory.AddMessageMode.NoStacking);
        debugEntryFactory.UpdateDebugEntry(entry.Id);
    }

    public void SubmitCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        CommandHistory.Add(command);

        var debugConsoleManager = DebugConsoleManager.Instance;
        var commandRegistry = CommandRegistry.Instance;
        var debugEntryFactory = debugConsoleManager.DebugEntryFactory;

        // Debug entry setup
        int executionToken = debugConsoleManager.GetAvailableCustomDebugEntryId();
        long executionTimestamp = Stopwatch.GetTimestamp();

        debugEntryFactory.ResetTimestamp(executionToken, executionTimestamp);

        var entry = debugEntryFactory.GetDebugEntry(executionToken);

        AddPrivateEntry(entry);

        // Command setup
        var context = new CommandContext(this, executionToken);
        var commandMessage = new DebugConsoleManager.RawDebugEntry($"Command > {command}\n", Colors.LightGray,
            executionTimestamp, executionToken);

        // Prints command in console and updates the entry to immediately show what command is being executed.
        context.Print(commandMessage);
        debugEntryFactory.UpdateDebugEntry(executionToken);

        commandRegistry.Execute(context, command);

        // This updates the debug entry to reflect the final command output, if any, and releases the executionToken.
        debugConsoleManager.ReleaseCustomDebugEntryId(executionToken);

        // Update the labels.
        UpdateLiveEntries();

        // Put focus on the command message.
        stickToBottom = true;
    }

    protected override void OnHidden()
    {
        DebugConsoleManager.Instance.OnHistoryUpdated -= RefreshLogs;

        base.OnHidden();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            normalFontName.Dispose();
        }

        base.Dispose(disposing);
    }

    [Command("clear", false, "Clears this console.")]
    private static void CommandClear(CommandContext context)
    {
        context.Clear();
    }

    [Command("echo", false, "Echoes a message in this console.")]
    private static void CommandEcho(CommandContext context, string msg)
    {
        context.Print(msg);
    }

    [Command("echo", false, "Echoes a colored message in this console.")]
    private static void CommandEcho(CommandContext context, string msg, int r, int g, int b)
    {
        context.Print(msg, new Color(r, g, b));
    }

    private void Activate()
    {
        DebugConsoleManager.Instance.OnHistoryUpdated += RefreshLogs;

        commandInput.GrabFocus();

        RefreshLogs(DebugConsoleManager.Instance.MessageCountInHistory, privateHistory.Count);
    }

    private void AddPrivateEntry(DebugEntry entry)
    {
        privateHistory.AddToBack(entry);

        if (privateHistory.Count > MaxPrivateHistorySize)
            privateHistory.RemoveFromFront();

        RefreshLogs(0, 1);
    }

    private void RefreshLogs(int newMessages, int newPrivateMessages)
    {
        int size = DebugConsoleManager.Instance.MessageCountInHistory;

        int globalAdded = 0;
        int localAdded = 0;
        while (globalAdded < newMessages || localAdded < newPrivateMessages)
        {
            DebugEntry entry;

            int globalIndex = size - newMessages + globalAdded;
            int localIndex = privateHistory.Count - newPrivateMessages + localAdded;

            if (globalIndex == size)
            {
                entry = privateHistory[localIndex];

                ++localAdded;
            }
            else if (localIndex == privateHistory.Count)
            {
                entry = DebugConsoleManager.Instance.GetMessageAt(globalIndex);

                ++globalAdded;
            }
            else
            {
                var globalEntry = DebugConsoleManager.Instance.GetMessageAt(globalIndex);
                var localEntry = privateHistory[localIndex];

                if (globalEntry.BeginTimestamp < localEntry.BeginTimestamp)
                {
                    entry = DebugConsoleManager.Instance.GetMessageAt(globalIndex);
                    ++globalAdded;
                }
                else
                {
                    entry = privateHistory[localIndex];
                    ++localAdded;
                }
            }

            RichTextLabel label;
            EntryView view;
            if (debugEntryLabels.Count > DebugConsoleManager.MaxHistorySize)
            {
                view = debugEntryLabels.RemoveFromFront();
                view.Content = entry;

                label = view.Label;
                label.Visible = true;

                debugEntryList.RemoveChild(label);
            }
            else
            {
                label = new RichTextLabel();
                label.FitContent = true;
                label.BbcodeEnabled = true;
                label.SizeFlagsVertical = SizeFlags.ShrinkBegin;
                label.AutowrapMode = TextServer.AutowrapMode.Off;
                label.AddThemeFontOverride(normalFontName, font);
                label.AddThemeFontSizeOverride(normalFontName, 8);

                view = new EntryView(label, entry);
            }

            debugEntryLabels.AddToBack(view);
            debugEntryList.AddChild(label);

            label.Text = entry.Text;

            if (!entry.Frozen)
                liveEntries.Add(view);
        }

        UpdateLiveEntries();
    }

    private void UpdateLiveEntries()
    {
        if (liveEntries.Count == 0)
            return;

        liveEntries.RemoveWhere(view =>
        {
            var label = view.Label;
            var entry = view.Content;

            label.Text = entry.Text;

            return entry.Frozen;
        });
    }

    private void RefreshLogs(object? o, DebugConsoleManager.HistoryUpdatedEventArgs e)
    {
        RefreshLogs(e.NewMessages, 0);
    }

    private void CommandSubmitted(string command)
    {
        SubmitCommand(command);
    }

    private void OnScrolled()
    {
        var scrollBar = scrollContainer.GetVScrollBar();

        double diff = Math.Abs(scrollBar.Value - scrollBar.MaxValue + scrollBar.Page);

        stickToBottom = diff < 0.1f;
    }

    private void OnResized()
    {
        OnScrolled();
    }

    private void UpdateAutoscroll()
    {
        if (!stickToBottom)
            return;

        var scrollBar = scrollContainer.GetVScrollBar();

        scrollBar.Value = scrollBar.MaxValue - scrollBar.Page;
    }

    private sealed class EntryView(RichTextLabel label, DebugEntry content)
    {
        public readonly RichTextLabel Label = label;
        public DebugEntry Content = content;
    }
}
