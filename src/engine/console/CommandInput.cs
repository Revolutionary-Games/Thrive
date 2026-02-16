using Godot;
using Nito.Collections;

/// <summary>
///   A custom LineEdit to handle command inputs.
/// </summary>
public partial class CommandInput : LineEdit
{
    private const uint MaxCommandHistorySize = 128;

    private readonly Deque<string> commandHistory = [];

    private int commandHistoryIndex;
    private bool commandHistoryLookup;
    private bool inputKeyPressed;

    private enum HistoryCommandMode
    {
        [Alias("s")]
        Show,

        [Alias("c")]
        Clear,

        [Alias("a")]
        Add,
    }

    public override void _Input(InputEvent @event)
    {
        if (!HasFocus() || commandHistory.Count == 0)
            return;

        if (@event is not InputEventKey keyEvent)
            return;

        if (inputKeyPressed)
        {
            if (!keyEvent.Pressed)
                inputKeyPressed = false;

            return;
        }

        switch (keyEvent.Keycode)
        {
            case Key.Up:
                if (!commandHistoryLookup)
                {
                    commandHistoryLookup = true;
                    break;
                }

                ++commandHistoryIndex;
                break;
            case Key.Down:
                if (!commandHistoryLookup)
                    return;

                --commandHistoryIndex;
                break;
            default:
                return;
        }

        inputKeyPressed = true;

        if (commandHistoryIndex < 0)
        {
            commandHistoryIndex = 0;
        }
        else if (commandHistoryIndex >= commandHistory.Count)
        {
            commandHistoryIndex = commandHistory.Count - 1;
        }

        SetText(commandHistory[commandHistoryIndex]);
        GetViewport().SetInputAsHandled();

        CaretColumn = Text.Length;

        base._Input(@event);
    }

    [Command("history", false, "Shows the used command history.")]
    private static void CommandHistory(CommandContext context, HistoryCommandMode mode = HistoryCommandMode.Show,
        string attrib = "")
    {
        var commandInput = context.DebugConsole!.CommandInput;
        var commandHistory = commandInput.commandHistory;

        switch (mode)
        {
            case HistoryCommandMode.Show:
                for (int i = commandHistory.Count - 1; i >= 0; --i)
                    context.Print($"{i,-3:N0} {commandHistory[i]}");

                break;
            case HistoryCommandMode.Clear:
                commandInput.commandHistoryIndex = 0;
                commandHistory.Clear();
                break;
            case HistoryCommandMode.Add:
                if (string.IsNullOrWhiteSpace(attrib))
                    return;

                commandHistory.AddToFront(attrib);
                break;
            default:
                context.PrintErr($"Unknown parameter: {mode}");
                break;
        }
    }

    private void OnInput(string command)
    {
        Clear();

        if (string.IsNullOrWhiteSpace(command))
            return;

        // Update the command history.
        if (commandHistory.Count > 0)
        {
            // Avoid adding the exact same command to the history.
            if (command == commandHistory[0])
                return;

            if (commandHistory.Count >= MaxCommandHistorySize)
                commandHistory.RemoveFromBack();
        }

        commandHistory.AddToFront(command);
    }
}
