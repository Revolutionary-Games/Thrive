using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Displays fancy selection name labels on top of strategic entities
/// </summary>
public class StrategicEntityNameLabelSystem : Control
{
    /// <summary>
    ///   As the labels are (can be) type specific, we need to store them according to their types for reuse
    /// </summary>
    private readonly Dictionary<Type, List<CreatedNameLabel>> createdNameLabels = new();

#pragma warning disable CA2213
    private Camera? camera;
    private Node worldRoot = null!;
#pragma warning restore CA2213

    private float elapsed = 1;

    public override void _Ready()
    {
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

        // As we are contained in a stage that initialized us with its camera, the camera should always be valid
        if (!IsInstanceValid(camera) || !camera.Current)
        {
            GD.PrintErr("Name label system camera is not active while the system is running");
            return;
        }

        elapsed += delta;

        if (elapsed > Constants.NAME_LABELS_FULL_UPDATE_INTERVAL)
        {
            DetectLabelsForEntities();

            UpdateLabelText();

            elapsed = 0;
        }

        // Update positions each frame
        UpdateNameLabelPositions();
    }

    private void DetectLabelsForEntities()
    {
        if (camera == null)
            throw new InvalidOperationException("Not initialized");

        var nodes = worldRoot.GetTree().GetNodesInGroup(Constants.NAME_LABEL_GROUP);

        var thresholdSquared = Constants.NAME_LABEL_VISIBILITY_DISTANCE *
            Constants.NAME_LABEL_VISIBILITY_DISTANCE;

        foreach (var entry in createdNameLabels)
        {
            foreach (var createdLabel in entry.Value)
            {
                createdLabel.Marked = false;
            }
        }

        var cameraPos = camera.GlobalTransform.origin;

        foreach (Node node in nodes)
        {
            if (node is not IEntityWithNameLabel labelable)
            {
                GD.PrintErr("Object that can't be labeled in name label group");
                continue;
            }

            var distance = cameraPos.DistanceSquaredTo(labelable.EntityNode.GlobalTranslation);

            // Skip too far away things
            if (distance > thresholdSquared)
                continue;

            // We cache based on the labelable type, we could use the name label type for some more e
            var labelType = labelable.NameLabelType;

            if (!createdNameLabels.TryGetValue(labelType, out var labelCategory))
            {
                labelCategory = new List<CreatedNameLabel>();
                createdNameLabels.Add(labelType, labelCategory);
            }

            // TODO: should we store indexes per type for faster lookup here?

            bool reused = false;

            foreach (var nameLabel in labelCategory)
            {
                if (nameLabel.Marked)
                    continue;

                reused = true;
                nameLabel.SetEntity(labelable);
                break;
            }

            if (!reused)
            {
                if (labelCategory.Count >= Constants.NAME_LABELS_MAX_COUNT_PER_CATEGORY)
                {
                    // Too many labels in this category
                    // TODO: intelligently prioritize the shown labels
                    break;
                }

                // Can create a new label
                var newLabel = labelable.NameLabelScene.Instance<IEntityNameLabel>();

                // TODO: sizing
                // newLabel.RectSize =

                AddChild(newLabel.LabelControl);
                labelCategory.Add(new CreatedNameLabel(newLabel, labelable));
            }
        }

        // Hide the unnecessary labels for future use
        foreach (var entry in createdNameLabels)
        {
            foreach (var createdLabel in entry.Value)
            {
                if (createdLabel.Marked || ReferenceEquals(createdLabel.Entity, null))
                    continue;

                createdLabel.ClearEntity();
            }
        }
    }

    private void UpdateLabelText()
    {
        foreach (var entry in createdNameLabels)
        {
            foreach (var createdLabel in entry.Value)
            {
                createdLabel.UpdateLabel();
            }
        }
    }

    private void UpdateNameLabelPositions()
    {
        foreach (var entry in createdNameLabels)
        {
            foreach (var createdLabel in entry.Value)
            {
                var entityWrapper = createdLabel.Entity;

                // Skip the prompts that have no entity for them to use (when entity is cleared the prompts are hidden)
                if (ReferenceEquals(entityWrapper, null))
                    continue;

                var entity = entityWrapper.Value;

                if (entity == null)
                {
                    createdLabel.ClearEntity();
                    continue;
                }

                var entityTransform = entity.EntityNode.GlobalTransform;
                var position = entityTransform.origin + InteractableEntityHelpers.RotateExtraInteractionOffset(
                    entity.LabelOffset, entityTransform.basis);

                if (camera!.IsPositionBehind(position))
                {
                    // Behind the camera, should be hidden
                    if (!createdLabel.HiddenForBeingBehindCamera)
                    {
                        createdLabel.HiddenForBeingBehindCamera = true;
                        createdLabel.NameLabel.Visible = false;
                    }

                    continue;
                }

                if (createdLabel.HiddenForBeingBehindCamera)
                {
                    // Reset hidden status if this was hidden before
                    createdLabel.HiddenForBeingBehindCamera = false;
                    createdLabel.NameLabel.Visible = true;
                }

                var control = createdLabel.NameLabel.LabelControl;

                var screenPosition = camera.UnprojectPosition(position) - control.RectSize * 0.5f;

                control.RectGlobalPosition = screenPosition;
            }
        }
    }

    private class CreatedNameLabel
    {
        public readonly IEntityNameLabel NameLabel;
        public EntityReference<IEntityWithNameLabel>? Entity;

        public bool HiddenForBeingBehindCamera;

        public bool Marked = true;

        public CreatedNameLabel(IEntityNameLabel nameLabel, IEntityWithNameLabel entity)
        {
            nameLabel.OnEntitySelectedHandler += OnClicked;
            NameLabel = nameLabel;
            Entity = new EntityReference<IEntityWithNameLabel>(entity);
        }

        public void ClearEntity()
        {
            Entity = null;
            NameLabel.Visible = false;
        }

        public void SetEntity(IEntityWithNameLabel interactable)
        {
            if (Entity?.Value == null)
                NameLabel.Visible = true;

            Entity = new EntityReference<IEntityWithNameLabel>(interactable);
            Marked = true;
        }

        public void UpdateLabel()
        {
            var entity = Entity?.Value;

            if (entity == null)
                return;

            NameLabel.UpdateFromEntity(entity);
        }

        private void OnClicked()
        {
            var entity = Entity?.Value;

            entity?.OnSelectedThroughLabel();
        }
    }
}
