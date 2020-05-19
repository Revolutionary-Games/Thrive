using System;
using Godot;

/// <summary>
///   Manages the help screen GUI
/// </summary>
public class HelpScreen : Control
{
    [Export]
    public NodePath LeftColumPath;

    [Export]
    public NodePath RightColumnPath;

    [Export]
    public NodePath LineSeparatorPath;

    [Export]
    public NodePath TipMessageLabelPath;

    [Export]
    public PackedScene HelpBoxScene;

    [Export]
    public NodePath TimerPath;

    private VBoxContainer leftColumn;
    private VBoxContainer rightColumn;
    private HSeparator lineSeparator;
    private Label tipMessageLabel;
    private Timer timer;

    private Random random;

    [Signal]
    public delegate void HelpScreenClosed();

    public override void _Ready()
    {
        leftColumn = GetNode<VBoxContainer>(LeftColumPath);
        rightColumn = GetNode<VBoxContainer>(RightColumnPath);
        lineSeparator = GetNode<HSeparator>(LineSeparatorPath);
        tipMessageLabel = GetNode<Label>(TipMessageLabelPath);
        timer = GetNode<Timer>(TimerPath);

        random = new Random();
    }

    /// <summary>
    ///   Fill the help screen with texts from the json file
    /// </summary>
    public void BuildHelpTexts(string category)
    {
        var texts = SimulationParameters.Instance.GetHelpTexts(category);

        var leftTexts = texts.LeftTexts;
        var rightTexts = texts.RightTexts;

        // Fill the left column with text boxes
        foreach (var entry in leftTexts)
        {
            var helpBox = HelpBoxScene.Instance();
            helpBox.GetNode<Label>("MarginContainer/Label").Text = entry;
            leftColumn.AddChild(helpBox);
        }

        // Fill the right column with text boxes
        foreach (var entry in rightTexts)
        {
            var helpBox = HelpBoxScene.Instance();
            helpBox.GetNode<Label>("MarginContainer/Label").Text = entry;
            rightColumn.AddChild(helpBox);
        }
    }

    /// <summary>
    ///   Toggles the help screen visibility. Also randomizes the
    ///   easter egg messages and its chance of showing up.
    /// </summary>
    public void Toggle()
    {
        if (!Visible)
        {
            Show();

            lineSeparator.Hide();
            tipMessageLabel.Hide();

            if (random.Next(0, 6) > 1)
            {
                var messages = SimulationParameters.Instance.EasterEggMessages;

                tipMessageLabel.Text = messages.Messages.Random(random);

                lineSeparator.Show();
                tipMessageLabel.Show();

                timer.Start(20);
            }
        }
        else
        {
            Hide();
        }
    }

    /*
        Callbacks
    */

    private void OnTimerTimeout()
    {
        lineSeparator.Hide();
        tipMessageLabel.Hide();
    }

    private void OnCloseButtonPressed()
    {
        EmitSignal(nameof(HelpScreenClosed));
    }
}
