using Nito.Collections;

public class CommandHistory
{
    private const uint MaxCommandHistorySize = 128;

    private readonly Deque<string> commandHistory = [];

    private enum HistoryCommandMode
    {
        [Alias("s")]
        Show,

        [Alias("c")]
        Clear,

        [Alias("a")]
        Add,
    }

    public bool LookingUp { get; set; }
    public int HistoryLength => commandHistory.Count;

    public int CommandHistoryIndex
    {
        get;
        set
        {
            int newValue = value;
            if (newValue < 0)
            {
                LookingUp = false;
                newValue = 0;
            }
            else if (newValue >= commandHistory.Count)
            {
                newValue = commandHistory.Count - 1;
            }

            field = newValue;
        }
    }

    public string CurrentCommand => LookingUp ? commandHistory[CommandHistoryIndex] : string.Empty;

    public void Add(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        // Update the command history.
        if (commandHistory.Count > 0)
        {
            // Avoid adding the exact same command to the history.
            if (command == commandHistory[0])
                return;

            // We have entered a command that is different from the one selected from the history. So, we go back to
            // zero and exit the history lookup.
            if (command != commandHistory[CommandHistoryIndex])
            {
                LookingUp = false;
                CommandHistoryIndex = 0;
            }

            if (commandHistory.Count >= MaxCommandHistorySize)
                commandHistory.RemoveFromBack();
        }

        commandHistory.AddToFront(command);
    }

    [Command("history", false, "Shows the used command history.")]
    private static void CommandManageHistory(CommandContext context, HistoryCommandMode mode = HistoryCommandMode.Show,
        string attribute = "")
    {
        var commandInvoker = context.CommandInvoker;
        var commandHistory = commandInvoker.CommandHistory;

        switch (mode)
        {
            case HistoryCommandMode.Show:
                for (int i = commandHistory.HistoryLength - 1; i >= 0; --i)
                    context.Print($"{i,-3:N0} {commandHistory.commandHistory[i]}");

                break;
            case HistoryCommandMode.Clear:
                commandHistory.CommandHistoryIndex = 0;
                commandHistory.commandHistory.Clear();
                break;
            case HistoryCommandMode.Add:
                commandHistory.Add(attribute);
                break;
            default:
                context.PrintErr($"Unknown parameter: {mode}");
                break;
        }
    }
}
