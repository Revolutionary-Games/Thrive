using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Displays progress bars above entities that want to display a progress
/// </summary>
public partial class ProgressBarSystem : Control
{
    private readonly List<CreatedProgressBar> createdProgressBars = new();

#pragma warning disable CA2213
    private PackedScene progressBarScene = null!;

    private Camera3D? camera;
    private Node worldRoot = null!;
#pragma warning restore CA2213

    private Vector3 playerPosition;

    private double elapsed = 1;

    public override void _Ready()
    {
        progressBarScene = GD.Load<PackedScene>("res://src/gui_common/WorldProgressBar.tscn");
    }

    public void Init(Camera3D stageCamera, Node worldEntityRoot)
    {
        camera = stageCamera;
        worldRoot = worldEntityRoot;
    }

    public override void _Process(double delta)
    {
        if (camera == null)
            throw new InvalidOperationException("This system is not initialized");

        // As we are contained in a stage that initialized us with its camera, the camera should always be valid
        if (!IsInstanceValid(camera) || !camera.Current)
        {
            GD.PrintErr("Progress bar system camera is not active while the system is running");
            return;
        }

        elapsed += delta;

        // Do full entity scan update at a reduced interval
        if (elapsed > Constants.WORLD_PROGRESS_BAR_FULL_UPDATE_INTERVAL)
        {
            DetectProgressForEntities();

            elapsed = 0;
        }

        // Update positions and progress each frame
        UpdateProgressPositions();
    }

    public void UpdatePlayerPosition(Vector3 position)
    {
        playerPosition = position;
    }

    private void DetectProgressForEntities()
    {
        var nodes = worldRoot.GetTree().GetNodesInGroup(Constants.PROGRESS_ENTITY_GROUP);

        var thresholdSquared = Constants.WORLD_PROGRESS_BAR_MAX_DISTANCE *
            Constants.WORLD_PROGRESS_BAR_MAX_DISTANCE;

        int nextBarIndex = 0;

        foreach (var createdBar in createdProgressBars)
        {
            createdBar.Marked = false;
        }

        foreach (Node node in nodes)
        {
            if (node is not IActionProgressSource progressSource)
            {
                GD.PrintErr("Non-progress reporting object in progress group");
                continue;
            }

            // Don't need to process things that don't have an in-progress action
            if (!progressSource.ActionInProgress)
                continue;

            if (node is not IEntity entity)
            {
                GD.PrintErr("Progress system sees an entity that is not IEntity");
                continue;
            }

            if (!progressSource.ActionInProgress)
                continue;

            var distance = playerPosition.DistanceSquaredTo(entity.EntityNode.GlobalPosition);

            if (distance > thresholdSquared)
                continue;

            // Need to show a bar for this
            if (nextBarIndex >= createdProgressBars.Count)
            {
                // We need more buttons
                if (createdProgressBars.Count >= Constants.WORLD_PROGRESS_BAR_MAX_COUNT)
                {
                    // Too many progress bars in use

                    // TODO: intelligently choose the nearest things to the player to keep the most important
                    // things visible
                    break;
                }

                var newBar = progressBarScene.Instantiate<ProgressBar>();
                var barHolder = new CreatedProgressBar(newBar, entity, progressSource);

                AddChild(newBar);
                createdProgressBars.Add(barHolder);
            }
            else
            {
                // Can reuse an existing button
                var barHolder = createdProgressBars[nextBarIndex];

                barHolder.SetEntity(entity, progressSource);
            }

            ++nextBarIndex;
        }

        foreach (var createdBar in createdProgressBars)
        {
            if (createdBar.Marked || ReferenceEquals(createdBar.Entity, null))
                continue;

            createdBar.ClearEntity();
        }
    }

    private void UpdateProgressPositions()
    {
        foreach (var createdBar in createdProgressBars)
        {
            var entityWrapper = createdBar.Entity;

            // Skip the bars that have nothing to show (we cache unused bars)
            if (ReferenceEquals(entityWrapper, null))
                continue;

            // We don't clear things each frame so we need to skip removed things for a few frames until the full update
            var entity = entityWrapper.Value;
            var progressData = createdBar.EntityProgressData;

            // TODO: could show the finished bar and fade it out when an action is complete
            // To do that also DetectProgressForEntities will need changes
            if (entity == null || progressData == null || !progressData.ActionInProgress)
            {
                createdBar.ClearEntity();
                continue;
            }

            var entityTransform = entity.EntityNode.GlobalTransform;
            var position = entityTransform.Origin +
                new Vector3(0, Constants.WORLD_PROGRESS_DEFAULT_Y_OFFSET, 0);

            var extraOffset = progressData.ExtraProgressBarWorldOffset;

            if (extraOffset != null)
            {
                // Extra offset is relative to a non-rotated state of the object, so we need to correct that here
                position += entityTransform.Basis * extraOffset.Value;
            }

            if (camera!.IsPositionBehind(position))
            {
                // Hide things that are behind the camera
                if (!createdBar.HiddenMomentarily)
                {
                    createdBar.HiddenMomentarily = true;
                    createdBar.Bar.Visible = false;
                }

                continue;
            }

            if (createdBar.HiddenMomentarily)
            {
                // Reset hidden status if this was hidden before
                createdBar.HiddenMomentarily = false;
                createdBar.Bar.Visible = true;
            }

            var screenPosition = camera.UnprojectPosition(position);
            var distance = (position - playerPosition).Length();

            createdBar.UpdateSizeAndPosition(distance, screenPosition);

            // Update progress
            createdBar.Bar.Value = progressData.ActionProgress;
        }
    }

    private class CreatedProgressBar
    {
        public readonly ProgressBar Bar;
        public IActionProgressSource? EntityProgressData;
        public EntityReference<IEntity>? Entity;

        public bool Marked = true;

        /// <summary>
        ///   True when hidden due to being behind the camera or being too small
        /// </summary>
        public bool HiddenMomentarily;

        public CreatedProgressBar(ProgressBar bar, IEntity entity, IActionProgressSource entityProgressData)
        {
            Bar = bar;
            Entity = new EntityReference<IEntity>(entity);
            EntityProgressData = entityProgressData;
        }

        public void ClearEntity()
        {
            Entity = null;
            EntityProgressData = null;
            Bar.Visible = false;
        }

        public void SetEntity(IEntity entity, IActionProgressSource entityProgressData)
        {
            if (Entity?.Value == null)
                Bar.Visible = true;

            Entity = new EntityReference<IEntity>(entity);
            EntityProgressData = entityProgressData;
            Marked = true;
        }

        public void UpdateSizeAndPosition(float distance, Vector2 screenPosition)
        {
            var width = Constants.WORLD_PROGRESS_BAR_DEFAULT_WIDTH -
                distance * Constants.WORLD_PROGRESS_BAR_DISTANCE_SIZE_SCALE;

            // If width would fall too low, then need to hide this
            if (width < Constants.WORLD_PROGRESS_BAR_MIN_WIDTH_TO_SHOW)
            {
                HiddenMomentarily = true;
                Bar.Visible = false;
                return;
            }

            var height = Constants.WORLD_PROGRESS_BAR_DEFAULT_HEIGHT *
                (width / Constants.WORLD_PROGRESS_BAR_DEFAULT_WIDTH);

            height = Mathf.Clamp(height, Constants.WORLD_PROGRESS_BAR_MIN_HEIGHT,
                Constants.WORLD_PROGRESS_BAR_DEFAULT_HEIGHT);

            Bar.Size = new Vector2(width, height);

            Bar.GlobalPosition = screenPosition - new Vector2(width * 0.5f, height * 0.5f);
        }
    }
}
