using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   The debug console manager.
///   This is used by Debug Consoles to retrieve logs.
///   System output is transferred to this manager by the LogInterceptor.
/// </summary>
public static class DebugConsoleManager
{
    public const uint MaxConsoleSize = 255;

    private static readonly List<ConsoleLine> Lines = [];

    public static event EventHandler<ConsoleLineArgs>? OnMessageReceived;

    /// <summary>
    ///   Adds a log entry to the console manager.
    /// </summary>
    /// <param name="line">The message of the log</param>
    /// <param name="isError">Whether it is an error message</param>
    public static void Print(string line, bool isError = false)
    {
        var col = isError ? Colors.Red : Colors.White;
        var consoleLine = new ConsoleLine(line, col);

        lock (Lines)
        {
            // for now, we cap max console size to MaxConsoleSize to avoid flooding
            if (Lines.Count > MaxConsoleSize)
            {
                Lines.RemoveAt(0);
            }

            Lines.Add(consoleLine);
        }

        OnMessageReceived?.Invoke(null, new ConsoleLineArgs(consoleLine));
    }

    /// <summary>
    ///   Clears logs in the console manager.
    /// </summary>
    public static void Clear()
    {
        lock (Lines)
        {
            Lines.Clear();
        }
    }

    /// <summary>
    ///   Returns a readonly copy list of the console lines.
    /// </summary>
    public static IReadOnlyList<ConsoleLine> GetLines()
    {
        lock (Lines)
        {
            return Lines.ToArray();
        }
    }

    /// <summary>
    ///   A utility record to define console line properties and data.
    /// </summary>
    /// <param name="Line"> the line content </param>
    /// <param name="Color"> the line color</param>
    public record ConsoleLine(string Line, Color Color);

    public class ConsoleLineArgs(ConsoleLine line) : EventArgs
    {
        public ConsoleLine Line { get; } = line;
    }
}
