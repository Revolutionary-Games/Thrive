using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Partial class: Entity label
/// </summary>
public partial class DebugOverlays
{
    private readonly Dictionary<IEntity, Label> entityLabels = new();

    private bool showEntityLabels;
    private Font smallerFont = null!;
    private Camera? activeCamera;

    private bool ShowEntityLabels
    {
        get => showEntityLabels;
        set
        {
            showEntityLabels = value;
            labelsLayer.Visible = value;
        }
    }

    private void UpdateLabelColour(IEntity iEntity, Spatial entity, Label label)
    {
        if (!iEntity.AliveMarker.Alive)
        {
            label.Set("custom_colors/font_color", new Color(1.0f, 0.3f, 0.3f));
        }

        switch (entity)
        {
            case Microbe microbe:
            {
                switch (microbe.State)
                {
                    case Microbe.MicrobeState.Binding:
                    {
                        label.Set("custom_colors/font_color", new Color(0.2f, 0.5f, 0.0f));
                        break;
                    }

                    case Microbe.MicrobeState.Engulf:
                    {
                        label.Set("custom_colors/font_color", new Color(0.2f, 0.5f, 1.0f));
                        break;
                    }

                    case Microbe.MicrobeState.Unbinding:
                    {
                        label.Set("custom_colors/font_color", new Color(1.0f, 0.5f, 0.2f));
                        break;
                    }

                    default:
                    {
                        label.Set("custom_colors/font_color", new Color(1.0f, 1.0f, 1.0f));
                        break;
                    }
                }

                break;
            }
        }
    }

    private void UpdateEntityLabels()
    {
        if (activeCamera is not { Current: true })
            activeCamera = GetViewport().GetCamera();

        if (activeCamera == null)
            return;

        foreach (var pair in entityLabels)
        {
            var iEntity = pair.Key;
            var entity = iEntity.EntityNode;
            var label = pair.Value;

            label.RectPosition = activeCamera.UnprojectPosition(entity.Transform.origin);

            UpdateLabelColour(iEntity, entity, label);

            if (!label.Text.Empty())
                continue;

            // Update names
            switch (entity)
            {
                case Microbe microbe:
                {
                    if (microbe.Species != null)
                    {
                        label.Text =
                            $"[{microbe.Name}:{microbe.Species.Genus.Left(1)}.{microbe.Species.Epithet.Left(4)}]";
                    }

                    break;
                }

                case FloatingChunk chunk:
                {
                    label.Text = $"[{chunk.Name}:{chunk.ChunkName}]";
                    break;
                }

                default:
                {
                    label.Text = $"[{entity.Name}]";
                    break;
                }
            }
        }
    }

    private void OnNodeAdded(Node node)
    {
        if (node is not IEntity entity)
            return;

        var label = new Label();
        labelsLayer.AddChild(label);
        entityLabels.Add(entity, label);

        switch (entity)
        {
            case FloatingChunk:
            case AgentProjectile:
            {
                // To reduce the labels overlapping each other
                label.AddFontOverride("font", smallerFont);
                break;
            }
        }
    }

    private void OnNodeRemoved(Node node)
    {
        if (node is Camera camera)
        {
            // When a camera is removed from the scene tree, it can't be active and will be disposed soon.
            // This makes sure the active camera is not disposed so we don't check it in _Process().
            if (activeCamera == camera)
                activeCamera = null;

            return;
        }

        if (node is not IEntity entity)
            return;

        if (entityLabels.TryGetValue(entity, out var label))
        {
            label.DetachAndQueueFree();
            entityLabels.Remove(entity);
        }
    }

    private void SearchSceneTreeForEntity(Node node)
    {
        if (node is IEntity)
            OnNodeAdded(node);

        foreach (Node child in node.GetChildren())
            SearchSceneTreeForEntity(child);
    }

    private void InitiateEntityLabels()
    {
        var rootTree = GetTree();

        SearchSceneTreeForEntity(rootTree.Root);

        rootTree.Connect("node_added", this, nameof(OnNodeAdded));
        rootTree.Connect("node_removed", this, nameof(OnNodeRemoved));
    }

    private void CleanEntityLabels()
    {
        foreach (var entityLabelsKey in entityLabels.Keys.ToList())
            OnNodeRemoved(entityLabelsKey.EntityNode);

        activeCamera = null;

        var rootTree = GetTree();
        rootTree.Disconnect("node_added", this, nameof(OnNodeAdded));
        rootTree.Disconnect("node_removed", this, nameof(OnNodeRemoved));
    }
}
