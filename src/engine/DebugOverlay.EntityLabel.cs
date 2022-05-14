using System.Collections.Generic;
using Godot;

/// <summary>
///   Partial class: Entity label
/// </summary>
public partial class DebugOverlay
{
    private readonly Dictionary<RigidBody, Label> entityLabels = new();

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

    private void EntityLabelEnterTree()
    {
        var rootTree = GetTree();
        rootTree.Connect("node_added", this, nameof(OnNodeAdded));
        rootTree.Connect("node_removed", this, nameof(OnNodeRemoved));
    }

    private void EntityLabelExitTree()
    {
        var rootTree = GetTree();
        rootTree.Disconnect("node_added", this, nameof(OnNodeAdded));
        rootTree.Disconnect("node_removed", this, nameof(OnNodeRemoved));
    }

    private void EntityLabelProcess()
    {
        if (activeCamera is not { Current: true })
            activeCamera = GetViewport().GetCamera();

        if (showEntityLabels)
        {
            UpdateEntityLabels();
        }
    }

    private void UpdateEntityLabels()
    {
        if (activeCamera == null)
            return;

        foreach (var pair in entityLabels)
        {
            var body = pair.Key;
            var label = pair.Value;

            // Update names
            if (label.Text.Empty())
            {
                switch (body)
                {
                    case Microbe microbe:
                    {
                        if (microbe.Species != null!)
                        {
                            label.Text =
                                $"[{microbe.Name}:{microbe.Species.Genus[0]}.{microbe.Species.Epithet.Left(4)}]";
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
                        label.Text = $"[{body.Name}]";
                        break;
                    }
                }
            }

            label.RectPosition = activeCamera.UnprojectPosition(body.Transform.origin);
        }
    }

    private void UpdateLabelOnMicrobeDeath(Microbe microbe)
    {
        if (entityLabels.TryGetValue(microbe, out var label))
            label.Set("custom_colors/font_color", new Color(1.0f, 0.3f, 0.3f));
    }

    private void OnNodeAdded(Node node)
    {
        if (node is not RigidBody body)
            return;

        var label = new Label();
        labelsLayer.AddChild(label);
        entityLabels.Add(body, label);

        switch (body)
        {
            case Microbe microbe:
            {
                microbe.OnDeath += UpdateLabelOnMicrobeDeath;
                break;
            }

            case FloatingChunk:
            case AgentProjectile:
            {
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

        if (node is not RigidBody body)
            return;

        if (entityLabels.TryGetValue(body, out var label))
        {
            labelsLayer.RemoveChild(label);
            label.QueueFree();
            entityLabels.Remove(body);

            if (body is Microbe microbe)
            {
                microbe.OnDeath -= UpdateLabelOnMicrobeDeath;
            }
        }
    }
}
