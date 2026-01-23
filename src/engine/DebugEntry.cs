using Godot;

public sealed class DebugEntry
{
    public static readonly DebugEntry EmptyDebugEntry = new(string.Empty, 1, string.Empty, Colors.White, 0, true, -1);

    private readonly int pipelineId;

    public DebugEntry(string text, int amount, string? amountTextCache, Color messageColor, long beginTimestamp,
        bool frozen, int pipelineId)
    {
        Text = text;
        Amount = amount;
        AmountTextCache = amountTextCache;
        MessageColor = messageColor;
        Frozen = frozen;
        BeginTimestamp = beginTimestamp;

        this.pipelineId = pipelineId;
    }

    public Color MessageColor { get; }

    public string Text
    {
        get;
        set
        {
            if (!Frozen)
                field = value;
        }
    }

    public int Amount
    {
        get;
        set
        {
            if (!Frozen)
                field = value;
        }
    }

    public string? AmountTextCache
    {
        get;
        set
        {
            if (!Frozen)
                field = value;
        }
    }

    public long BeginTimestamp { get; }

    public long EndTimestamp
    {
        get;
        set
        {
            if (!Frozen)
                field = value;
        }
    }

    public bool Frozen
    {
        get;
        set
        {
            // AmountTextCache should be set before freezing this entry, but to be careful we fall back to this if we
            // didn't.
            if (value && AmountTextCache == null)
                AmountTextCache = $"x{Amount}";

            if (!Frozen)
                field = value;
        }
    }

    public bool IsMultipleMessages => Amount > 1;

    public void Update()
    {
        if (Frozen)
            return;

        DebugEntryFactory.Instance.UpdateDebugEntry(pipelineId);
    }
}
