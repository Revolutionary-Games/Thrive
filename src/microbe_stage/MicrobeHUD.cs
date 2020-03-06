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

    public RichTextLabel CompoundsLabel;

    /// <summary>
    ///   Access to the stage to retrieve information for display as
    ///   well as call some player initiated actions.
    /// </summary>
    public MicrobeStage Stage;

    public override void _Ready()
    {
        CompoundsLabel = GetNode<RichTextLabel>(CompoundsLabelPath);
    }

    public override void _Process(float delta)
    {
        if (Stage == null)
            return;

        if (Stage.Player != null)
        {
            CompoundsLabel.Text = CompoundsToString(Stage.Player.Compounds.Compounds);
        }

        // Stage.
    }

    private string CompoundsToString(Dictionary<string, float> compounds)
    {
        StringBuilder compoundsText = new StringBuilder(string.Empty, 150);

        bool first = true;

        foreach (var entry in compounds)
        {
            if (!first)
                compoundsText.Append("\n");
            first = false;

            compoundsText.Append($"{entry.Key}: {entry.Value}");
        }

        return compoundsText.ToString();
    }
}
