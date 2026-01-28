using System.Diagnostics;
using Godot;

public class CommandContext(DebugConsole? debugConsole, int executionToken)
{
    public void Clear()
    {
        debugConsole?.Clear();
    }

    public void Print(string message, Color color)
    {
        if (debugConsole == null)
            return;

        var debugEntry = new DebugConsoleManager.RawDebugEntry(message, color, Stopwatch.GetTimestamp(), executionToken,
            true);

        debugConsole.AddPrivateLog(debugEntry);
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
