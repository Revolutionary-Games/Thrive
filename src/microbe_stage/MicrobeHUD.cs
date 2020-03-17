using System;
using System.Collections.Generic;
using System.Text;
using Godot;

/// <summary>
///   Manages the microbe HUD display
/// </summary>
public class MicrobeHUD : Node
{
    [Export]
    public NodePath CompoundsLabelPath;

    [Export]
    public NodePath HoveredItemsLabelPath;

    public AudioStreamPlayer GUIAudio;

    private RichTextLabel compoundsLabel;

    private RichTextLabel hoveredItemsLabel;

    private VBoxContainer menu;

    /// <summary>
    ///   Access to the stage to retrieve information for display as
    ///   well as call some player initiated actions.
    /// </summary>
    private MicrobeStage stage;

    public override void _Ready()
    {
        compoundsLabel = GetNode<RichTextLabel>(CompoundsLabelPath);
        hoveredItemsLabel = GetNode<RichTextLabel>(HoveredItemsLabelPath);
        GUIAudio = GetNode<AudioStreamPlayer>("MicrobeGUIAudio");
        menu = GetNode<VBoxContainer>("CenterContainer/MicrobeStageMenu");
    }

    public override void _Process(float delta)
    {
        if (stage == null)
            return;

        if (stage.Player != null)
        {
            compoundsLabel.Text = CompoundsToString(stage.Player.Compounds.Compounds);
        }

        if (stage.Camera != null)
        {
            var compounds = stage.Clouds.GetAllAvailableAt(stage.Camera.CursorWorldPos);

            StringBuilder builder = new StringBuilder("Things at ", 250);

            builder.AppendFormat("{0:F1}, {1:F1}:\n",
                stage.Camera.CursorWorldPos.x, stage.Camera.CursorWorldPos.z);
            builder.Append(CompoundsToString(compounds));

            hoveredItemsLabel.Text = builder.ToString();
        }
    }

    public void Init(MicrobeStage stage)
    {
        this.stage = stage;
    }

    private string CompoundsToString(Dictionary<string, float> compounds)
    {
        var simulation = SimulationParameters.Instance;

        StringBuilder compoundsText = new StringBuilder(string.Empty, 150);

        bool first = true;

        foreach (var entry in compounds)
        {
            if (!first)
                compoundsText.Append("\n");
            first = false;

            var readableName = simulation.GetCompound(entry.Key).Name;
            compoundsText.Append(readableName);
            compoundsText.AppendFormat(": {0:F1}", entry.Value);
        }

        return compoundsText.ToString();
    }

    // Function to play a blinky sound when a button is pressed
    public void PlayButtonPressSound()
    {
        var sound = GD.Load<AudioStream>(
            "res://assets/sounds/soundeffects/gui/button-hover-click.ogg");

        GUIAudio.Stream = sound;
        GUIAudio.Play();
    }

    // Received for button that opens the menu inside the Microbe Stage
    public void OpenMicrobeStageMenuPressed()
    {
        if (menu.IsVisible())
        {
            menu.Hide();
        }else
        {
            menu.Show();
        }
        PlayButtonPressSound();
    }
}
