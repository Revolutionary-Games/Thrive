using System;
using System.Collections.Concurrent;
using Godot;

/// <summary>
///   The debug console manager.
///   This is used by Debug Consoles to retrieve logs.
///   System output is transferred to this manager by the LogInterceptor.
/// </summary>
public static class DebugConsoleManager
{
    public const uint MaxConsoleSize = 255;

    public static readonly ConcurrentQueue<ConsoleLine> Lines = [];

    /// <summary>
    ///   Adds a log entry to the console manager.
    /// </summary>
    /// <param name="line">The message of the log</param>
    /// <param name="isError">Whether it is an error message</param>
    public static void Print(string line, bool isError = false)
    {
        var color = isError ? Colors.Red : Colors.White;
        var consoleLine = new ConsoleLine(line, color);

        Print(consoleLine);
    }

    /// <summary>
    ///   Adds a log entry to the console manager.
    /// </summary>
    /// <param name="consoleLine">The console line data</param>
    public static void Print(ConsoleLine consoleLine)
    {
        // for now, we cap max console size to MaxConsoleSize to avoid flooding
        if (Lines.Count > MaxConsoleSize)
        {
            Lines.TryDequeue(out _);
        }

        Lines.Enqueue(consoleLine);
    }

    public static void Clear()
    {
        Lines.Clear();
    }

    /// <summary>
    ///   A utility record to define console line properties and data.
    /// </summary>
    /// <param name="Line"> the line content </param>
    /// <param name="Color"> the line color</param>
    public record struct ConsoleLine(string Line, Color Color);

    public class ConsoleLineArgs(ConsoleLine line) : EventArgs
    {
        public ConsoleLine Line { get; } = line;
    }
}
