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

        // autoEvoResults.

        // Queue a redraw to draw all the connection lines again
        QueueRedraw();
    }
}
