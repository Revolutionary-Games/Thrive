using System.Collections.Generic;
using AutoEvo;
using Godot;

/// <summary>
///   Displays a food chain from auto-evo results in the GUI for the player to inspect. This node should be put inside
///   a <see cref="DraggableScrollContainer"/> to allow big food chains to be viewed
/// </summary>
public partial class FoodChainDisplay : Control
{
    private readonly List<SpeciesResultButton> resultButtons = new();

#pragma warning disable CA2213
    private PackedScene speciesResultButtonScene = null!;
#pragma warning restore CA2213

    private RunResults? lastResults;
    private Patch? lastPatch;

    public override void _Ready()
    {
        speciesResultButtonScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/SpeciesResultButton.tscn");
    }

    public override void _Process(double delta)
    {
    }

    public override void _Draw()
    {
        base._Draw();

        DrawLine(new Vector2(0, 0), new Vector2(Size.X, Size.Y), Colors.Aquamarine, 2, true);
    }

    public void DisplayFoodChainIfRequired(RunResults autoEvoResults, Patch forPatch)
    {
        // Only update if data has changed
        if (autoEvoResults == lastResults && forPatch == lastPatch)
            return;

        lastResults = autoEvoResults;
        lastPatch = forPatch;

        // TODO: reuse nodes that can be to make this faster
        this.QueueFreeChildren();
        resultButtons.Clear();

        float x = 10;
        float y = 20;

        foreach (var stuff in autoEvoResults.GetSpeciesRecords())
        {
            if (stuff.Value.Species != null)
            {
                var resultDisplay = speciesResultButtonScene.Instantiate<SpeciesResultButton>();

                resultDisplay.DisplaySpecies(autoEvoResults.GetSpeciesResultForInternalUse(stuff.Value.Species), false);

                // Ensure the control size doesn't blow up
                resultDisplay.SizeFlagsVertical = SizeFlags.ShrinkBegin;
                resultDisplay.AnchorLeft = 0;
                resultDisplay.AnchorRight = 0;
                resultDisplay.AnchorTop = 0;
                resultDisplay.AnchorBottom = 0;

                // resultDisplay.Size = resultDisplay.CustomMinimumSize;
                resultDisplay.Size = new Vector2(110, 92);

                // Position the node
                resultDisplay.Position = new Vector2(x, y);

                // TODO: signals

                AddChild(resultDisplay);
                resultButtons.Add(resultDisplay);

                // Ensure the control size doesn't blow up
                // resultDisplay.Size = resultDisplay.CustomMinimumSize;
                // resultDisplay.Size = new Vector2(110, 92);

                y += 10 + resultDisplay.Size.Y * 2;
            }
        }

        // Make sure this control is big enough to contain all the child nodes and to make the scroll container work
        CustomMinimumSize = new Vector2(Size.X, y + 10);

        // Queue a redraw to draw all the connection lines again
        QueueRedraw();
    }
}
