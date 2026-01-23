using System;
using System.Collections.Generic;
using System.Text;
using Godot;

/// <summary>
///   This singleton is responsible for the controlled creation and updates of DebugEntry instances.
/// </summary>
public class DebugEntryFactory
{
    // This is in ticks, or 100 ns, so this is equivalent to 1 ms.
    private const long DEFAULT_MAX_TIMESTAMP_DIFFERENCE_FOR_STACKING = 10000;

    private static DebugEntryFactory? instance;

    private readonly DebugConsoleManager.RawDebugEntry debugEntryBuildDataPlaceholder = new(string.Empty, Colors.White,
        0, -1);

    private readonly Dictionary<int, DebugEntryFactoryPipeline> pipelines = new();

    public static DebugEntryFactory Instance => instance ?? throw new InstanceNotLoadedYetException();

    /// <summary>
    ///   The maximum timestamp difference required to stack two consecutive messages into one, in ticks (100 ns).
    /// </summary>
    public long MaxTimestampDifferenceForStacking { get; set; } = DEFAULT_MAX_TIMESTAMP_DIFFERENCE_FOR_STACKING;

    public static void Initialize()
    {
        if (instance != null)
        {
            GD.PrintErr("DebugEntryFactory: Already initialized.");
            return;
        }

        instance = new DebugEntryFactory();
    }

    public static void Shutdown()
    {
        if (instance == null)
        {
            GD.PrintErr("DebugEntryFactory: Not initialized.");
            return;
        }

        instance = null;
    }

    public bool Flush(int id)
    {
        if (!pipelines.TryGetValue(id, out var pipeline))
            return false;

        // Flush the pipeline by resetting everything.
        pipeline.DebugEntryCache = null;
        pipeline.RichTextBuilder.Clear();
        pipeline.AmountStringBuilder?.Clear();
        pipeline.LastMessage = debugEntryBuildDataPlaceholder;
        pipeline.MultipleMessages = false;

        return true;
    }

    /// <summary>
    ///   This should be called right after flushing a pipeline to initialize the timestamp.
    /// </summary>
    /// <param name="id"> The pipeline id. </param>
    /// <param name="toProcess"> The first RawDebugEntry to process in the flushed pipeline. </param>
    /// <returns>true iff the pipeline exists.</returns>
    public bool NotifyRootMessage(int id, DebugConsoleManager.RawDebugEntry toProcess)
    {
        if (!pipelines.TryGetValue(id, out var pipeline))
            return false;

        // We need to keep track of the initial timestamp when we flush to start processing a new entry.
        // The first timestamp on the pipeline is handled by GetPipeline.
        pipeline.BeginTimestamp = toProcess.Timestamp;

        return true;
    }

    public void UpdateDebugEntry(int id, bool freeze = false)
    {
        var pipeline = GetPipeline(id, 0, out var richTextBuilder);
        var debugEntry = GetDebugEntry(id);

        if (debugEntry.Frozen)
            return;

        var lastMessage = pipeline.LastMessage;
        var value = richTextBuilder.ToString();
        var amountTextCache = GetAmountString(id);

        debugEntry.Text = value;
        debugEntry.Amount = lastMessage.Amount;
        debugEntry.AmountTextCache = amountTextCache;
        debugEntry.Frozen = freeze;
    }

    public DebugEntry GetDebugEntry(int id, bool freeze = false)
    {
        var pipeline = GetPipeline(id, 0, out var richTextBuilder);

        if (pipeline.DebugEntryCache != null)
            return pipeline.DebugEntryCache;

        richTextBuilder = pipeline.RichTextBuilder;

        var lastMessage = pipeline.LastMessage;
        var value = richTextBuilder.ToString();
        var amountTextCache = GetAmountString(id);
        var debugEntry = new DebugEntry(value,
            lastMessage.Amount,
            amountTextCache,
            lastMessage.Color,
            pipeline.BeginTimestamp,
            freeze,
            id);

        pipeline.DebugEntryCache = debugEntry;

        return debugEntry;
    }

    public string GetAmountString(int id)
    {
        if (!pipelines.TryGetValue(id, out var pipeline))
            return string.Empty;

        var amountStringBuilder = pipeline.AmountStringBuilder;
        var lastMessage = pipeline.LastMessage;

        if (lastMessage.Amount <= 1)
            return string.Empty;

        amountStringBuilder ??= new StringBuilder();

        if (amountStringBuilder.Length == 0)
            amountStringBuilder.Append('x');

        // We copy the number string in the stringbuilder to avoid a double allocation.
        // Max int is 10 digits.
        Span<char> buffer = stackalloc char[10];
        if (lastMessage.Amount.TryFormat(buffer, out int written))
        {
            ReadOnlySpan<char> span = buffer[..written];

            // + 1 is because we use the 'x' char as prefix.
            amountStringBuilder.Length = written + 1;

            for (int i = 0; i < written; i++)
            {
                amountStringBuilder[i + 1] = span[i];
            }
        }

        var amountString = amountStringBuilder.ToString();

        return amountString;
    }

    /// <summary>
    ///   Tries to add a new message to this entry panel. This method may refuse to add the message in case the messages
    ///   are stacked and the new message is different, or in case the messages are different and the new message
    ///   requires stacking. If this method fails, a new DebugEntryPanel should be created.
    /// </summary>
    /// <param name="id">The message id</param>
    /// <param name="rawDebugEntry">The console rawDebugEntry to append.</param>
    /// <param name="suppressMessageStacking">Whether message stacking should be suppressed. Default is false.</param>
    /// <returns>true if adding the message was successful, false otherwise.</returns>
    public bool TryAddMessage(int id, DebugConsoleManager.RawDebugEntry rawDebugEntry,
        bool suppressMessageStacking = false)
    {
        var pipeline = GetPipeline(id, rawDebugEntry.Timestamp, out _);
        var lastMessage = pipeline.LastMessage;
        var isFirstMessage = lastMessage.Id == -1;

        if ((!rawDebugEntry.NoTimeDifference || MaxTimestampDifferenceForStacking <= 0) && !isFirstMessage &&
            rawDebugEntry.Timestamp - lastMessage.Timestamp >= MaxTimestampDifferenceForStacking)
        {
            return false;
        }

        if (suppressMessageStacking || rawDebugEntry != lastMessage)
        {
            if (lastMessage.Amount > 1)
            {
                // The current new message is different from the multiple equivalent previous messages.
                return false;
            }

            // The current new message is different from the previous one, or we suppressed message stacking
            // so we can append it.
            AddMessage(pipeline, rawDebugEntry, false);

            if (!isFirstMessage)
            {
                pipeline.MultipleMessages = true;
            }
        }
        else
        {
            if (pipeline.MultipleMessages)
            {
                // This DebugEntryPanel shows multiple different messages, so to stack multiple new messages we need
                // another DebugEntryPanel.

                return false;
            }

            // The current new message is equivalent to the previous one. Instead of appending it, we stack them and
            // increment the amount counter.
            AddMessage(pipeline, rawDebugEntry, true);
        }

        return true;
    }

    private static void AddMessage(DebugEntryFactoryPipeline pipeline, DebugConsoleManager.RawDebugEntry message,
        bool stack)
    {
        var lastMessage = pipeline.LastMessage;
        var amountStringBuilder = pipeline.AmountStringBuilder;
        var richTextBuilder = pipeline.RichTextBuilder;

        if (stack)
        {
            lastMessage.Amount++;
        }
        else
        {
            if (lastMessage.Amount > 1 && amountStringBuilder != null)
            {
                richTextBuilder.Append(amountStringBuilder);
                amountStringBuilder.Clear();
            }

            // RichText to set foreground color.
            richTextBuilder.Append($"[color=#{message.Color.ToHtml()}]");
            richTextBuilder.Append(message.Line);

            if (richTextBuilder[^1] != '\n')
                richTextBuilder.Append('\n');

            richTextBuilder.Append("[/color]");

            pipeline.LastMessage = message;
        }
    }

    private DebugEntryFactoryPipeline GetPipeline(int id, long beginTimestamp, out StringBuilder richTextBuilder)
    {
        if (!pipelines.TryGetValue(id, out var pipeline))
        {
            richTextBuilder = new StringBuilder();
            pipeline = new DebugEntryFactoryPipeline(richTextBuilder, null,
                debugEntryBuildDataPlaceholder, null, beginTimestamp, false);

            pipelines.Add(id, pipeline);
        }

        richTextBuilder = pipeline.RichTextBuilder;

        return pipeline;
    }

    private sealed class DebugEntryFactoryPipeline(StringBuilder richTextBuilder, StringBuilder? amountStringBuilder,
        DebugConsoleManager.RawDebugEntry lastMessage, DebugEntry? debugEntryCache, long beginTimestamp,
        bool multipleMessages)
    {
        public StringBuilder RichTextBuilder { get; } = richTextBuilder;
        public StringBuilder? AmountStringBuilder { get; } = amountStringBuilder;
        public DebugConsoleManager.RawDebugEntry LastMessage { get; set; } = lastMessage;
        public DebugEntry? DebugEntryCache { get; set; } = debugEntryCache;
        public bool MultipleMessages { get; set; } = multipleMessages;
        public long BeginTimestamp { get; set; } = beginTimestamp;
    }
}
