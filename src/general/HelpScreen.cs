using System;
using Godot;

/// <summary>
///   Manages the help screen GUI
/// </summary>
public partial class HelpScreen : Control
{
    /// <summary>
    ///   The category which this help screen belongs to
    ///   (e.g. MicrobeStage, MicrobeEditor)
    /// </summary>
    [Export]
    public string Category = null!;

    [Export]
    public NodePath? LeftColumnPath;

    [Export]
    public NodePath RightColumnPath = null!;

    [Export]
    public NodePath TipMessageLabelPath = null!;

    [Export]
    public NodePath TimerPath = null!;

#pragma warning disable CA2213
    [Export]
    public PackedScene HelpBoxScene = null!;

    private VBoxContainer leftColumn = null!;
    private VBoxContainer rightColumn = null!;
    private Label tipMessageLabel = null!;
    private Timer timer = null!;
#pragma warning restore CA2213

    private Random random = null!;

    [Signal]
    public delegate void HelpScreenClosedEventHandler();

    public override void _Ready()
    {
        leftColumn = GetNode<VBoxContainer>(LeftColumnPath);
        rightColumn = GetNode<VBoxContainer>(RightColumnPath);
        tipMessageLabel = GetNode<Label>(TipMessageLabelPath);
        timer = GetNode<Timer>(TimerPath);

        random = new Random();

        if (!string.IsNullOrEmpty(Category))
        {
            BuildHelpTexts(Category);
        }
        else
        {
            GD.PrintErr("Help screen has no category, can't load texts");
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            leftColumn.QueueFreeChildren();
            rightColumn.QueueFreeChildren();
            BuildHelpTexts(Category);
        }
    }

    /// <summary>
    ///   Randomizes the easter egg messages
    ///   and its chance of showing up.
    /// </summary>
    public void RandomizeEasterEgg()
    {
        tipMessageLabel.Hide();

        if (random.Next(0, 6) > 1)
        {
            var helpTexts = SimulationParameters.Instance.GetHelpTexts("EasterEgg");

            tipMessageLabel.Text = Localization.Translate(helpTexts.Messages.Random(random).Message);
            tipMessageLabel.Show();

            timer.Start(20);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (LeftColumnPath != null)
            {
                LeftColumnPath.Dispose();
                RightColumnPath.Dispose();
                TipMessageLabelPath.Dispose();
                TimerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Loads the help screen with texts from the json file
    ///   and turn it into help boxes
    /// </summary>
    private void BuildHelpTexts(string category)
    {
        var helpTexts = SimulationParameters.Instance.GetHelpTexts(category);

        foreach (var text in helpTexts.Messages)
        {
            var message = Localization.Translate(text.Message);

            var helpBox = HelpBoxScene.Instantiate();
            helpBox.GetNode<CustomRichTextLabel>("MarginContainer/CustomRichTextLabel").ExtendedBbcode = message;

            if (text.Column == HelpText.TextColumn.Left)
            {
                leftColumn.AddChild(helpBox);
            }
            else if (text.Column == HelpText.TextColumn.Right)
            {
                rightColumn.AddChild(helpBox);
            }
            else
            {
                GD.PrintErr("Help text doesn't have a column set, couldn't be added into the container");

                // Queued as otherwise help text's ExtendedBbcode would be applied on a disposed object
                Invoke.Instance.Queue(helpBox.DetachAndQueueFree);
            }
        }
    }

    // Callbacks

    private void OnTimerTimeout()
    {
        tipMessageLabel.Hide();
    }

    private void OnCloseButtonPressed()
    {
        EmitSignal(SignalName.HelpScreenClosed);
    }
}
