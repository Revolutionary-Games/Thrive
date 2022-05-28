using System.Collections.Generic;
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

    private void UpdateEntityLabels()
    {
        if (activeCamera == null)
            return;

        foreach (var pair in entityLabels)
        {
            var body = pair.Key.EntityNode;
            var label = pair.Value;

            label.RectPosition = activeCamera.UnprojectPosition(body.Transform.origin);

            // Update names
            if (!label.Text.Empty())
                continue;

            switch (body)
            {
                case Microbe microbe:
                {
                    if (microbe.Species != null!)
                    {
                        label.Text = $"[{microbe.Name}:{microbe.Species.Genus[0]}.{microbe.Species.Epithet.Left(4)}]";
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
    }

    private void UpdateLabelOnMicrobeDeath(Microbe microbe)
    {
        if (entityLabels.TryGetValue(microbe, out var label))
            label.Set("custom_colors/font_color", new Color(1.0f, 0.3f, 0.3f));
    }

    private void OnNodeAdded(Node node)
    {
        if (node is not IEntity body)
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

        if (node is not IEntity body)
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
