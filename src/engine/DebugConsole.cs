using System;
using Godot;

/// <summary>
///   Handles the debug console
/// </summary>
public partial class DebugConsole : CustomWindow
{
    private int lastClearId;
    private int lastMessageId;

#pragma warning disable CA2213
    [Export]
    private RichTextLabel consoleArea = null!;
#pragma warning restore CA2213

    public bool IsConsoleOpen
    {
        get => Visible;
        set
        {
            var debugConsoleManager = DebugConsoleManager.GetInstance()!;

            if (value)
            {
                Show();

                debugConsoleManager.OnHistoryUpdated += RefreshLogs;

                RefreshLogs();
            }
            else
            {
                Hide();

                debugConsoleManager.OnHistoryUpdated -= RefreshLogs;
            }
        }
    }

    public override void _Ready()
    {
        RefreshLogs();

        base._Ready();
    }

    public override void _ExitTree()
    {
        // make sure we unsubscribe from the event handler.
        IsConsoleOpen = false;

        base._ExitTree();
    }

    public void AddLog(DebugConsoleManager.ConsoleLine line)
    {
        consoleArea.PushColor(line.Color);
        consoleArea.AddText(line.Line);
        consoleArea.Pop();
    }

    public void Clear()
    {
        lastClearId = lastMessageId;

        consoleArea.Clear();
    }

    private void RefreshLogs()
    {
        var debugConsoleManager = DebugConsoleManager.GetInstance()!;

        int newMessageCount = debugConsoleManager.MessageCount - lastMessageId;
        if (newMessageCount <= 0)
            return;

        int paragraphCount = consoleArea.GetParagraphCount();
        int linesToAdd = Math.Min(newMessageCount, debugConsoleManager.History.Count);
        if (paragraphCount + linesToAdd > debugConsoleManager.History.Count)
        {
            // Apparently, RichTextLabel uses a vector to store paragraphs. If it uses a vector, so we can't just remove
            // paragraphs freely efficiently. Perhaps, in the future, we should implement our own RichTextLabel or
            // console renderer backed by a ring-buffer, if performance ever becomes a concern in this console.
            // For now, I use this hybrid approach, which only removes the front paragraph (oldest line) if there's only
            // one pending message.
            if (newMessageCount == 1)
            {
                consoleArea.RemoveParagraph(0);
            }
            else
            {
                consoleArea.Clear();
                linesToAdd = debugConsoleManager.History.Count;
            }
        }

        int start = debugConsoleManager.History.Count - linesToAdd;
        for (int i = start; i < debugConsoleManager.History.Count; ++i)
        {
            var line = debugConsoleManager.History[i];
            AddLog(line);
        }

        lastMessageId = debugConsoleManager.MessageCount;
    }

    private void RefreshLogs(object? o, EventArgs e)
    {
        RefreshLogs();
    }
}
