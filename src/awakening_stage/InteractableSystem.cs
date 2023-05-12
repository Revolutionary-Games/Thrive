using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Handles positioning the interact buttons on screen on top of interactable entities
/// </summary>
public class InteractableSystem : Control
{
    [Export]
    public Color InactiveInteractable = new(0.3f, 0.3f, 0.3f);

    [Export]
    public Color ActiveInteractable = new(1, 1, 1);

    private readonly List<CreatedPrompt> createdKeyPromptButtons = new();
    private readonly List<IInteractableEntity> allNearbyInteractables = new();

#pragma warning disable CA2213
    private PackedScene interactableButtonScene = null!;

    private Camera? camera;
    private Node worldRoot = null!;
#pragma warning restore CA2213

    private Vector3 playerPosition;
    private float playerDistanceModifier;

    private float elapsed = 1;
    private bool currentlyActive = true;

    private CreatedPrompt? bestInteractable;

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
        if (!currentlyActive)
            return;

        if (camera == null)
            throw new InvalidOperationException("This system is not initialized");

        // As we are contained in a stage that initialized us with its camera, the camera should always be valid
        if (!IsInstanceValid(camera) || !camera.Current)
        {
            GD.PrintErr("Interactable system camera is not active while the system is running");
            return;
        }

        elapsed += delta;

        // Update nearby entity list only few times per second
        if (elapsed > Constants.INTERACTION_BUTTONS_FULL_UPDATE_INTERVAL)
        {
            DetectPromptsForEntities();

            elapsed = 0;
        }

        // Update positions each frame
        UpdatePromptPositions();
    }

    /// <summary>
    ///   Sets the system active or disabled (based on the current stage of the player)
    /// </summary>
    /// <param name="active">True to set active</param>
    public void SetActive(bool active)
    {
        if (currentlyActive == active)
            return;

        currentlyActive = active;

        // Disable everything currently showing if this is made inactive
        if (!currentlyActive)
        {
            foreach (var createdPrompt in createdKeyPromptButtons)
            {
                createdPrompt.ClearEntity();
            }
        }
    }

    public void UpdatePlayerPosition(Vector3 position, float playerInteractionDistanceModifier)
    {
        playerPosition = position;
        playerDistanceModifier = playerInteractionDistanceModifier;
    }

    public IInteractableEntity? GetInteractionTarget()
    {
        return bestInteractable?.Entity?.Value;
    }

    public IReadOnlyCollection<IInteractableEntity> GetAllNearbyObjects()
    {
        return allNearbyInteractables;
    }

    private void DetectPromptsForEntities()
    {
        var nodes = worldRoot.GetTree().GetNodesInGroup(Constants.INTERACTABLE_GROUP);

        var thresholdSquared = Constants.INTERACTION_DEFAULT_VISIBILITY_DISTANCE *
            Constants.INTERACTION_DEFAULT_VISIBILITY_DISTANCE;

        int nextPromptIndex = 0;

        foreach (var createdPrompt in createdKeyPromptButtons)
        {
            createdPrompt.Marked = false;
        }

        foreach (Node node in nodes)
        {
            if (node is not IInteractableEntity interactable)
            {
                GD.PrintErr("Non-interactable object in interactable group");
                continue;
            }

            if (interactable.InteractionDisabled)
                continue;

            var distance = playerPosition.DistanceSquaredTo(interactable.EntityNode.GlobalTranslation);

            var offset = interactable.InteractDistanceOffset;
            offset += playerDistanceModifier;

            if (distance > thresholdSquared + offset * offset)
                continue;

            // Need to show an interact button for this
            if (nextPromptIndex >= createdKeyPromptButtons.Count)
            {
                // We need more buttons
                if (createdKeyPromptButtons.Count >= Constants.INTERACTION_BUTTONS_MAX_COUNT)
                {
                    // Too many interact buttons in use

                    // TODO: intelligently choose the nearest things to the player to keep the most important
                    // interact buttons
                    break;
                }

                var newPrompt = interactableButtonScene.Instance<KeyPrompt>();
                newPrompt.ShowPress = false;
                newPrompt.ActionName = "g_interact";
                newPrompt.RectSize =
                    new Vector2(Constants.INTERACTION_BUTTON_SIZE, Constants.INTERACTION_BUTTON_SIZE);
                newPrompt.Modulate = InactiveInteractable;

                AddChild(newPrompt);
                createdKeyPromptButtons.Add(new CreatedPrompt(newPrompt, interactable));
            }
            else
            {
                // Can reuse an existing button
                var prompt = createdKeyPromptButtons[nextPromptIndex];

                prompt.SetEntity(interactable);
            }

            ++nextPromptIndex;
        }

        // Clear the entities from prompts that are excess
        foreach (var createdPrompt in createdKeyPromptButtons)
        {
            if (createdPrompt.Marked || ReferenceEquals(createdPrompt.Entity, null))
                continue;

            createdPrompt.ClearEntity();
        }
    }

    private void UpdatePromptPositions()
    {
        var interactThreshold = Constants.INTERACTION_DEFAULT_INTERACT_DISTANCE *
            Constants.INTERACTION_DEFAULT_INTERACT_DISTANCE;

        var viewDirection = camera!.GlobalTransform.basis.Quat().Xform(Vector3.Forward);

        bestInteractable = null;
        var bestInteractableScore = double.MaxValue;
        allNearbyInteractables.Clear();

        foreach (var createdPrompt in createdKeyPromptButtons)
        {
            var entityWrapper = createdPrompt.Entity;

            // Skip the prompts that have no entity for them to use (when entity is cleared the prompts are hidden)
            if (ReferenceEquals(entityWrapper, null))
                continue;

            // We don't clear things each frame so we need to skip removed things for a few frames until the full update
            var entity = entityWrapper.Value;

            if (entity == null)
            {
                createdPrompt.ClearEntity();
                continue;
            }

            var entityTransform = entity.EntityNode.GlobalTransform;
            var position = entityTransform.origin +
                new Vector3(0, Constants.INTERACTION_BUTTON_DEFAULT_Y_OFFSET, 0);

            var extraOffset = entity.ExtraInteractionCenterOffset;

            if (extraOffset != null)
            {
                // Extra offset is relative to a non-rotated state of the object, so we need to correct that here
                position += InteractableEntityHelpers.RotateExtraInteractionOffset(extraOffset.Value,
                    entityTransform.basis);
            }

            if (camera.IsPositionBehind(position))
            {
                // Hide interact buttons behind the camera
                if (!createdPrompt.HiddenForBeingBehindCamera)
                {
                    createdPrompt.HiddenForBeingBehindCamera = true;
                    createdPrompt.Prompt.Visible = false;
                }

                continue;
            }

            if (createdPrompt.HiddenForBeingBehindCamera)
            {
                // Reset hidden status if this was hidden before
                createdPrompt.HiddenForBeingBehindCamera = false;
                createdPrompt.Prompt.Visible = true;
            }

            var screenPosition = camera.UnprojectPosition(position) +
                new Vector2(Constants.INTERACTION_BUTTON_X_PIXEL_OFFSET, Constants.INTERACTION_BUTTON_Y_PIXEL_OFFSET);

            // TODO: Now with that IsPositionBehind check the angle check below (which doesn't really work)
            // can be probably be removed, though the priority for closer angled objects should be kept

            // TODO: position smoothing somehow as when the camera moves slightly when the player wobbles, the prompts
            // move quite a bit. Same fix should also be added to the ProgressBarSystem

            createdPrompt.Prompt.RectGlobalPosition = screenPosition;

            if (createdPrompt.Highlighted)
            {
                createdPrompt.Prompt.Modulate = InactiveInteractable;
                createdPrompt.Highlighted = false;
            }

            // Update the closest object / what can be interacted with
            var vectorFromPlayer = position - playerPosition;

            var distance = vectorFromPlayer.LengthSquared();

            var offset = entity.InteractDistanceOffset;
            offset += playerDistanceModifier;

            if (distance > interactThreshold + offset * offset)
                continue;

            // Close enough to interact
            allNearbyInteractables.Add(entity);

            // Update the best interactable object
            var angle = Math.Abs(vectorFromPlayer.SignedAngleTo(viewDirection, Vector3.Up));

            // If an object is out of view, it cannot be the best to interact with
            // TODO: this angle check doesn't really work currently (might need to use the signed method) but as we use
            // a third person camera, this doesn't actually seem all that bad as usually any objects near the player
            // are visible, but would be good to fix.
            if (angle > Constants.INTERACTION_MAX_ANGLE_TO_VIEW)
                continue;

            // Select the closest to view direction and weight some distance also in
            var score = Math.Log(distance) + angle * 2.0f;

            if (bestInteractable == null)
            {
                bestInteractable = createdPrompt;
                bestInteractableScore = score;
            }
            else
            {
                if (score < bestInteractableScore)
                {
                    bestInteractableScore = score;
                    bestInteractable = createdPrompt;
                }
            }
        }

        if (bestInteractable != null)
        {
            bestInteractable.Highlighted = true;
            bestInteractable.Prompt.Modulate = ActiveInteractable;
        }
    }

    private class CreatedPrompt
    {
        public readonly KeyPrompt Prompt;
        public EntityReference<IInteractableEntity>? Entity;

        public bool Marked = true;

        public bool Highlighted;
        public bool HiddenForBeingBehindCamera;

        public CreatedPrompt(KeyPrompt prompt, IInteractableEntity entity)
        {
            Prompt = prompt;
            Entity = new EntityReference<IInteractableEntity>(entity);
        }

        public void ClearEntity()
        {
            Entity = null;
            Prompt.Visible = false;
        }

        public void SetEntity(IInteractableEntity interactable)
        {
            if (Entity?.Value == null)
                Prompt.Visible = true;

            Entity = new EntityReference<IInteractableEntity>(interactable);
            Marked = true;
        }
    }
}
