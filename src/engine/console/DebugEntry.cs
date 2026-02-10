using System;
using Godot;

public sealed class DebugEntry
{
    public DebugEntry(string text, int amount, string? amountText, Color messageColor, long beginTimestamp, bool frozen)
    {
        Text = text;
        Amount = amount;
        AmountText = amountText;
        MessageColor = messageColor;
        Frozen = frozen;
        BeginTimestamp = beginTimestamp;
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

    private void ThrowIfFrozen()
    {
        if (Frozen)
            throw new InvalidOperationException("Can't modify a frozen DebugEntry!");
    }
}
