using System;
using Godot;

/// <summary>
///   Manages the help screen GUI
/// </summary>
public class HelpScreen : Control
{
    /// <summary>
    ///   The category which this help screen belongs to
    ///   (e.g. MicrobeStage, MicrobeEditor)
    /// </summary>
    [Export]
    public string Category;

    [Export]
    public NodePath LeftColumnPath;

    [Export]
    public NodePath RightColumnPath;

    [Export]
    public NodePath TipMessageLabelPath;

    [Export]
    public PackedScene HelpBoxScene;

    [Export]
    public NodePath TimerPath;

    private VBoxContainer leftColumn;
    private VBoxContainer rightColumn;
    private Label tipMessageLabel;
    private Timer timer;

    private Random random;

    [Signal]
    public delegate void HelpScreenClosed();

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

            tipMessageLabel.Text = TranslationServer.Translate(helpTexts.Messages.Random(random));
            tipMessageLabel.Show();

            timer.Start(20);
        }
    }

    /// <summary>
    ///   Loads the help screen with texts from the json file
    ///   and turn it into help boxes
    /// </summary>
    private void BuildHelpTexts(string category)
    {
        var helpTexts = SimulationParameters.Instance.GetHelpTexts(category);

        var middleIndex = helpTexts.Messages.Count / 2;

        for (var i = 0; i < helpTexts.Messages.Count; i++)
        {
            var message = TranslationServer.Translate(helpTexts.Messages[i]);

            var helpBox = HelpBoxScene.Instance();
            helpBox.GetNode<Label>("MarginContainer/Label").Text = message;

            if (i < middleIndex)
            {
                leftColumn.AddChild(helpBox);
            }
            else
            {
                rightColumn.AddChild(helpBox);
            }
        }
    }

    /*
        Callbacks
    */

    private void OnTimerTimeout()
    {
        tipMessageLabel.Hide();
    }

    private void OnCloseButtonPressed()
    {
        EmitSignal(nameof(HelpScreenClosed));
    }
}
