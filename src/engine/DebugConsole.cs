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
    private DebugEntryList debugEntryList = null!;

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
        commandInput.Connect(LineEdit.SignalName.TextSubmitted, new Callable(this, nameof(CommandSubmitted)));

        base._Ready();
    }

    public override void _ExitTree()
    {
        // Make sure we unsubscribe from the event handler.
        IsConsoleOpen = false;
        Clear();

        base._ExitTree();
    }

    public void AddPrivateLog(DebugConsoleManager.RawDebugEntry line)
    {
        var debugEntryFactory = DebugEntryFactory.Instance;

        debugEntryFactory.TryAddMessage(line.Id, line, true);
        debugEntryFactory.UpdateDebugEntry(line.Id);
    }

    public void Clear()
    {
        lastClearId = DebugConsoleManager.Instance.History.Count;
        RefreshLogs();
    }

    protected override void OnHidden()
    {
        DebugConsoleManager.Instance.OnHistoryUpdated -= RefreshLogs;

        base.OnHidden();
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

        RefreshLogs();
    }

    private void RefreshLogs()
    {
        debugEntryList.Refresh();
    }

    private void RefreshLogs(object? o, EventArgs e)
    {
        RefreshLogs();
    }

    private void CommandSubmitted(string cmd)
    {
        commandInput.Clear();

        var debugConsoleManager = DebugConsoleManager.Instance;
        var commandRegistry = CommandRegistry.Instance;
        var debugEntryFactory = DebugEntryFactory.Instance;

        int executionToken = debugConsoleManager.GetAvailableCustomDebugEntryId();

        debugEntryList.AddPrivateEntry(debugEntryFactory.GetDebugEntry(executionToken));

        var context = new CommandContext(this, executionToken);

        // Prints command in console and updates the entry to immediately show what command is being executed.
        context.Print($"Command > {cmd}\n", Colors.LightGray);
        debugEntryFactory.UpdateDebugEntry(executionToken);

        commandRegistry.Execute(context, cmd);

        // This updates the debug entry to reflect the final command output, if any, and releases the executionToken.
        debugConsoleManager.ReleaseCustomDebugEntryId(executionToken);

        RefreshLogs();
    }
}
