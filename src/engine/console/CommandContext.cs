using System.Diagnostics;
using Godot;

public class CommandContext(ICommandInvoker commandInvoker, int executionToken)
{
    public ICommandInvoker CommandInvoker => commandInvoker;

    public void Clear()
    {
        commandInvoker.Clear();
    }

    public void Print(DebugConsoleManager.RawDebugEntry entry)
    {
        commandInvoker.Print(entry);
    }

    public void Print(string message, Color color)
    {
        var debugEntry = new DebugConsoleManager.RawDebugEntry(message, color, Stopwatch.GetTimestamp(), executionToken,
            true);

        Print(debugEntry);
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
