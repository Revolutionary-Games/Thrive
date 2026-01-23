using Godot;

/// <summary>
///   A panel that displays a DebugEntry.
/// </summary>
public partial class DebugEntryPanel : PanelContainer
{
    private bool shouldUpdate = false;

#pragma warning disable CA2213
    [Export]
    private Panel messageCirclePanel = null!;

    [Export]
    private RichTextLabel messageLabel = null!;

    [Export]
    private Label amountLabel = null!;

    [Export]
    private ActionButton copyButton = null!;
#pragma warning restore CA2213

    public DebugEntry CurrentDebugEntry
    {
        get
        {
            return field ?? DebugEntry.EmptyDebugEntry;
        }
        set
        {
            field = value;

            shouldUpdate = true;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!shouldUpdate)
            return;

        var messageColor = CurrentDebugEntry.MessageColor;
        var messageText = CurrentDebugEntry.Text;
        var messageAmountText = CurrentDebugEntry.AmountTextCache;

        messageLabel.SetText(messageText);
        amountLabel.SetText(messageAmountText);

        messageCirclePanel.SelfModulate = messageColor;

        shouldUpdate = false;
    }
}
