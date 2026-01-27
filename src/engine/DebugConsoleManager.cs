using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Nito.Collections;
using Environment = System.Environment;

/// <summary>
///   The debug console manager.
///   This is used by Debug Consoles to retrieve logs.
///   System output is transferred to this manager by the LogInterceptor.
/// </summary>
[GodotAutoload]
public partial class DebugConsoleManager : Node
{
    public const uint MaxHistorySize = 255;

    public readonly Deque<DebugEntry> History = [];

    private static DebugConsoleManager? instance;

    private readonly Queue<int> customIdsBucket = [];
    private readonly Queue<RawDebugEntry> inbox = [];

    private readonly Dictionary<int, DebugEntry> activeEntries = [];

    private int customDebugEntryCounter;

    private DebugConsoleManager()
    {
        instance = this;

        CommandRegistry.Initialize();
        DebugEntryFactory.Initialize();
    }

    public event EventHandler<EventArgs>? OnHistoryUpdated;

    public static DebugConsoleManager Instance => instance ?? throw new InstanceNotLoadedYetException();

    public int TotalMessageCount { get; private set; }
    public int MessageCountInHistory { get; private set; }

    public override void _ExitTree()
    {
        CommandRegistry.Shutdown();
        DebugEntryFactory.Shutdown();

        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        if (OnHistoryUpdated == null)
            return;

        lock (inbox)
        {
            if (inbox.Count == 0)
                return;

            TotalMessageCount += inbox.Count;
            MessageCountInHistory += inbox.Count;

            var debugEntryFactory = DebugEntryFactory.Instance;

            while (inbox.TryDequeue(out var rawDebugEntry))
            {
                int id = rawDebugEntry.Id;

                if (!debugEntryFactory.TryAddMessage(id, rawDebugEntry))
                {
                    debugEntryFactory.UpdateDebugEntry(id);
                    debugEntryFactory.Flush(id);
                    activeEntries.Remove(id);

                    debugEntryFactory.NotifyRootMessage(id, rawDebugEntry);
                    if (debugEntryFactory.TryAddMessage(id, rawDebugEntry))
                    {
                        var newEntry = debugEntryFactory.GetDebugEntry(id);
                        History.AddToBack(newEntry);
                        activeEntries[id] = newEntry;
                    }
                    else
                    {
                        throw new Exception("DebugEntryFactory.Flush has failed in correctly resetting state.");
                    }
                }
                else
                {
                    if (!activeEntries.ContainsKey(id))
                    {
                        var liveEntry = debugEntryFactory.GetDebugEntry(id);
                        History.AddToBack(liveEntry);
                        activeEntries[id] = liveEntry;
                    }
                }

                if (History.Count <= MaxHistorySize)
                    continue;

                History.RemoveFromFront();

                MessageCountInHistory--;
            }

            OnHistoryUpdated.Invoke(null, EventArgs.Empty);
        }

        base._Process(delta);
    }

    public void Print(string line, int id, bool isError = false)
    {
        var color = isError ? Colors.Red : Colors.White;
        var timestamp = Stopwatch.GetTimestamp();
        var consoleLine = new RawDebugEntry(line, color, timestamp, id);

        Print(consoleLine);
    }

    /// <summary>
    ///   Adds a log entry to the console manager.
    /// </summary>
    /// <param name="line">The message of the log</param>
    /// <param name="isError">Whether it is an error message</param>
    public void Print(string line, bool isError = false)
    {
        int threadId = Environment.CurrentManagedThreadId;

        if (threadId >= 1 << 16)
        {
            GD.PrintErr($"Managed Thread Id is too large. Skipping messages for thread {threadId}.");
            return;
        }

        Print(line, threadId, isError);
    }

    /// <summary>
    ///   Adds a log entry to the console manager.
    /// </summary>
    /// <param name="rawDebugEntry">The console line data</param>
    public void Print(RawDebugEntry rawDebugEntry)
    {
        // This is for debugging. If we log a message that starts with the null char, we ignore it in the manager.
        if (rawDebugEntry.Line.StartsWith('☺'))
            return;

        // Avoid logging empty messages.
        if (rawDebugEntry.Line == string.Empty)
            return;

        lock (inbox)
        {
            inbox.Enqueue(rawDebugEntry);
        }
    }

    /// <summary>
    ///   Clears the debug console History.
    /// </summary>
    public void Clear()
    {
        History.Clear();
        activeEntries.Clear();
    }

    public void ReleaseCustomDebugEntryId(int id)
    {
        if (id < 1 << 16)
            GD.PrintErr("Invalid custom id. Custom ids should be generated with GenerateCustomDebugEntryId.");

        if (!DebugEntryFactory.Instance.Flush(id))
            GD.PrintErr("Invalid custom id. This id has never been generated.");

        var debugEntryFactory = DebugEntryFactory.Instance;

        debugEntryFactory.UpdateDebugEntry(id);
        debugEntryFactory.Flush(id);

        activeEntries.Remove(id);
        customIdsBucket.Enqueue(id);
    }

    /// <summary>
    ///   Acquires a token to identify an unique pipeline. When done, this token must be released by
    ///   ReleaseCustomDebugEntryId.
    /// </summary>
    public int GetAvailableCustomDebugEntryId()
    {
        int id = customIdsBucket.TryDequeue(out var customId) ? customId : GenerateCustomDebugEntryId();

        if (activeEntries.ContainsKey(id))
            return id;

        var liveEntry = DebugEntryFactory.Instance.GetDebugEntry(id);
        History.AddToBack(liveEntry);
        activeEntries[id] = liveEntry;

        return id;
    }

    /// <summary>
    ///   Generates a custom debug entry pipeline id. This is unlikely to collide with thread ids, unless we have more
    ///   than 2^16 threads.
    /// </summary>
    private int GenerateCustomDebugEntryId()
    {
        return ++customDebugEntryCounter << 16;
    }

    /// <summary>
    ///   A utility record to define console line properties and data.
    /// </summary>
    public sealed class RawDebugEntry(string line, Color color, long timestamp, int id, bool noTimeDifference = false)
        : IEquatable<RawDebugEntry>
    {
        public string Line { get; } = line;
        public Color Color { get; } = color;
        public long Timestamp { get; } = timestamp;
        public int Id { get; } = id;
        public bool NoTimeDifference { get; } = noTimeDifference;
        public int Amount { get; set; } = 1;

        public static bool operator ==(RawDebugEntry left, RawDebugEntry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RawDebugEntry left, RawDebugEntry right)
        {
            return !left.Equals(right);
        }

        public bool Equals(RawDebugEntry? other)
        {
            if (other is null)
                return false;

            return Line == other.Line && Color.IsEqualApprox(other.Color) && Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            return obj is RawDebugEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Line, Color, Id);
        }
    }
}
