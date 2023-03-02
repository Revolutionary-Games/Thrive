using System;
using Godot;

public class InteractableSystem : Control
{
#pragma warning disable CA2213
    private PackedScene interactableButtonScene = null!;
#pragma warning restore CA2213

    private Camera? camera;
    private Node worldRoot = null!;

    private Vector3 playerPosition;

    private float elapsed = 1;

    public override void _Ready()
    {
        interactableButtonScene = GD.Load<PackedScene>("res://src/gui_common/KeyPrompt.tscn");
    }

    public void Init(Camera stageCamera, Node worldEntityRoot)
    {
        camera = stageCamera;
        worldRoot = worldEntityRoot;
    }

    public override void _Process(float delta)
    {
        if (camera == null)
            throw new InvalidOperationException("This system is not initialized");

        elapsed += delta;

        // Update nearby entity list only few times per second
        if (elapsed > Constants.INTERACTION_BUTTONS_FULL_UPDATE_INTERVAL)
        {
            var nodes = worldRoot.GetTree().GetNodesInGroup(Constants.INTERACTABLE_GROUP);

            foreach (Node node in nodes)
            {
                if (node is IInteractableEntity interactable)
                {
                }
                else
                {
                    GD.PrintErr("Non-interactable object in interactable group");
                }
            }

            elapsed = 0;
        }

        // Update positions each frame
    }

    public void UpdatePlayerPosition(Vector3 position)
    {
        playerPosition = position;
    }
}
