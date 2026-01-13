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

    [Export]
    private LineEdit commandInput = null!;
#pragma warning restore CA2213

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
        if (Visible)
            Activate();

        commandInput.Connect(LineEdit.SignalName.TextSubmitted,
            new Callable(this, nameof(CommandSubmitted)));

        base._Ready();
    }

    public override void _ExitTree()
    {
        // make sure we unsubscribe from the event handler.
        IsConsoleOpen = false;

        Clear();

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

    protected override void OnHidden()
    {
        DebugConsoleManager.Instance.OnHistoryUpdated -= RefreshLogs;

        base.OnHidden();
    }

    [Command("clear", false, "Clears this console.")]
    private static void CommandClear(DebugConsole console)
    {
        console.Clear();
    }

    [Command("echo", false, "Echoes a message in this console.")]
    private static void CommandEcho(DebugConsole console, string msg)
    {
        console.AddLog(new DebugConsoleManager.ConsoleLine(msg + "\n", Colors.White));
    }

    [Command("echo", false, "Echoes a colored message in this console.")]
    private static void CommandEcho(DebugConsole console, string msg, int r, int g, int b)
    {
        console.AddLog(new DebugConsoleManager.ConsoleLine(msg + "\n", new Color(r, g, b)));
    }

    private void Activate()
    {
        DebugConsoleManager.Instance.OnHistoryUpdated += RefreshLogs;

        RefreshLogs();
    }

    private void RefreshLogs()
    {
        var debugConsoleManager = DebugConsoleManager.Instance;

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
            // For now, I use this hybrid approach, which should be good enough.
            if (newMessageCount <= 0.1 * DebugConsoleManager.MaxConsoleSize)
            {
                for (int i = 0; i < newMessageCount; ++i)
                    consoleArea.RemoveParagraph(0, true);

                consoleArea.InvalidateParagraph(0);
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

    private void CommandSubmitted(string cmd)
    {
        commandInput.Clear();

        AddLog(new DebugConsoleManager.ConsoleLine($"> {cmd}\n", Colors.LightGray));

        CommandRegistry.Instance.Execute(this, cmd);
    }
}
