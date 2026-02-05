using System.Diagnostics;
using Godot;

public class CommandContext(DebugConsole? debugConsole, int executionToken)
{
    public void Clear()
    {
        // Clear only if we're not headless.
        debugConsole?.Clear();
    }

    public void Print(string message, Color color)
    {
        var debugEntry = new DebugConsoleManager.RawDebugEntry(message, color, Stopwatch.GetTimestamp(), executionToken,
            true);

        if (debugConsole != null)
        {
            var debugEntryFactory = DebugConsoleManager.Instance.DebugEntryFactory;

            debugEntryFactory.TryAddMessage(executionToken, debugEntry, true);
            debugEntryFactory.UpdateDebugEntry(executionToken);
        }
        else
        {
            // We're headless, so we log to the global history.
            DebugConsoleManager.Instance.Print(debugEntry);
        }
    }

    public void Print(string message)
    {
        Print(message, Colors.White);
    }

    public void PrintWarning(string message)
    {
        Print(message, Colors.Yellow);
    }

    public void PrintErr(string message)
    {
        Print(message, Colors.Red);
    }
}
