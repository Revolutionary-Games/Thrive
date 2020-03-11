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

    private RichTextLabel compoundsLabel;

    private RichTextLabel hoveredItemsLabel;

    /// <summary>
    ///   Access to the stage to retrieve information for display as
    ///   well as call some player initiated actions.
    /// </summary>
    private MicrobeStage stage;

    public override void _Ready()
    {
        compoundsLabel = GetNode<RichTextLabel>(CompoundsLabelPath);
        hoveredItemsLabel = GetNode<RichTextLabel>(HoveredItemsLabelPath);
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
}
