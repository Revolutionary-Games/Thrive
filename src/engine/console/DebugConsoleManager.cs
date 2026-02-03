using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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

    public readonly DebugEntryFactory DebugEntryFactory;

    private static DebugConsoleManager? instance;

    private readonly Deque<DebugEntry> history = [];
    private readonly Queue<int> customIdsBucket = [];
    private readonly Queue<RawDebugEntry> inbox = [];

    private readonly Dictionary<int, DebugEntry> activeEntries = [];

    private int customDebugEntryCounter;

    private DebugConsoleManager()
    {
        instance = this;

        CommandRegistry.Initialize();

        DebugEntryFactory = new DebugEntryFactory();
    }

    public event EventHandler<EventArgs>? OnHistoryUpdated;

    public static DebugConsoleManager Instance => instance ?? throw new InstanceNotLoadedYetException();

    public int TotalMessageCount { get; private set; }
    public int MessageCountInHistory => history.Count;

    public override void _ExitTree()
    {
        CommandRegistry.Shutdown();

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

            while (inbox.TryDequeue(out var rawDebugEntry))
            {
                int id = rawDebugEntry.Id;

                if (!DebugEntryFactory.TryAddMessage(id, rawDebugEntry))
                {
                    DebugEntryFactory.UpdateDebugEntry(id);
                    DebugEntryFactory.Flush(id);
                    activeEntries.Remove(id);

                    DebugEntryFactory.NotifyRootMessage(id, rawDebugEntry);
                    if (DebugEntryFactory.TryAddMessage(id, rawDebugEntry))
                    {
                        var newEntry = DebugEntryFactory.GetDebugEntry(id);
                        history.AddToBack(newEntry);
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
                        var liveEntry = DebugEntryFactory.GetDebugEntry(id);
                        history.AddToBack(liveEntry);
                        activeEntries[id] = liveEntry;
                    }
                }

                if (history.Count <= MaxHistorySize)
                    continue;

                history.RemoveFromFront();
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
        // Avoid logging empty messages.
        if (rawDebugEntry.Line == string.Empty)
            return;

        lock (inbox)
        {
            inbox.Enqueue(rawDebugEntry);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DebugEntry GetMessageAt(int id)
    {
        return history[id];
    }

    /// <summary>
    ///   Clears the debug console History.
    /// </summary>
    public void Clear()
    {
        history.Clear();
        activeEntries.Clear();
    }

    public void ReleaseCustomDebugEntryId(int id)
    {
        if (id < 1 << 16)
            GD.PrintErr("Invalid custom id. Custom ids should be generated with GenerateCustomDebugEntryId.");

        if (!DebugEntryFactory.Flush(id))
            GD.PrintErr("Invalid custom id. This id has never been generated.");

        DebugEntryFactory.UpdateDebugEntry(id);
        DebugEntryFactory.Flush(id);

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

        var liveEntry = DebugEntryFactory.GetDebugEntry(id);
        history.AddToBack(liveEntry);
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
