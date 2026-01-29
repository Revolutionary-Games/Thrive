using System;
using Godot;

public sealed class DebugEntry
{
    private readonly int pipelineId;

    public DebugEntry(string text, int amount, string? amountText, Color messageColor, long beginTimestamp, bool frozen,
        int pipelineId)
    {
        Text = text;
        Amount = amount;
        AmountText = amountText;
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
            ThrowIfFrozen();

            field = value;
        }
    }

    public int Amount
    {
        get;
        set
        {
            ThrowIfFrozen();

            field = value;
        }
    }

    public string? AmountText
    {
        get;
        set
        {
            ThrowIfFrozen();

            field = value;
        }
    }

    public long BeginTimestamp { get; }

    public long EndTimestamp
    {
        get;
        set
        {
            ThrowIfFrozen();

            field = value;
        }
    }

    public bool Frozen { get; set; }

    public bool IsMultipleMessages => Amount > 1;

    public bool Update()
    {
        return !Frozen && DebugConsoleManager.Instance.DebugEntryFactory.UpdateDebugEntry(pipelineId);
    }

    private void ThrowIfFrozen()
    {
        if (Frozen)
            throw new InvalidOperationException("Can't modify a frozen DebugEntry!");
    }
}
