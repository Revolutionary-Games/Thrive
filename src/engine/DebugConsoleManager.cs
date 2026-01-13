using System;
using System.Collections.Generic;
using Godot;
using Nito.Collections;

/// <summary>
///   The debug console manager.
///   This is used by Debug Consoles to retrieve logs.
///   System output is transferred to this manager by the LogInterceptor.
/// </summary>
[GodotAutoload]
public partial class DebugConsoleManager : Node
{
    public const uint MaxConsoleSize = 255;

    public readonly Deque<ConsoleLine> History = [];

    private static DebugConsoleManager? instance;

    private readonly Queue<ConsoleLine> inbox = [];

    private DebugConsoleManager()
    {
        instance = this;

        CommandRegistry.Initialize();
    }

    public event EventHandler<EventArgs>? OnHistoryUpdated;

    public static DebugConsoleManager Instance => instance ?? throw new InstanceNotLoadedYetException();

    public int MessageCount { get; private set; }

    public override void _Process(double delta)
    {
        if (OnHistoryUpdated == null)
            return;

        lock (inbox)
        {
            if (inbox.Count == 0)
                return;

            MessageCount += inbox.Count;

            while (inbox.TryDequeue(out var line))
            {
                History.AddToBack(line);

                if (History.Count > MaxConsoleSize)
                {
                    History.RemoveFromFront();
                }
            }

            OnHistoryUpdated.Invoke(null, EventArgs.Empty);
        }

        base._Process(delta);
    }

    /// <summary>
    ///   Adds a log entry to the console manager.
    /// </summary>
    /// <param name="line">The message of the log</param>
    /// <param name="isError">Whether it is an error message</param>
    public void Print(string line, bool isError = false)
    {
        var color = isError ? Colors.Red : Colors.White;
        var consoleLine = new ConsoleLine(line, color);

        Print(consoleLine);
    }

    /// <summary>
    ///   Adds a log entry to the console manager.
    /// </summary>
    /// <param name="consoleLine">The console line data</param>
    public void Print(ConsoleLine consoleLine)
    {
        lock (inbox)
        {
            inbox.Enqueue(consoleLine);
        }
    }

    /// <summary>
    ///   Clears the debug console History.
    /// </summary>
    public void Clear()
    {
        History.Clear();
    }

    /// <summary>
    ///   A utility record to define console line properties and data.
    /// </summary>
    /// <param name="Line"> the line content </param>
    /// <param name="Color"> the line color</param>
    public record struct ConsoleLine(string Line, Color Color);
}
