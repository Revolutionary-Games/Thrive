using System.Collections.Generic;
using System.Linq;
using Components;
using DefaultEcs;
using Godot;

/// <summary>
///   Partial class: Entity label
/// </summary>
public partial class DebugOverlays
{
    private readonly Dictionary<Entity, Label> entityLabels = new();

    private readonly HashSet<Entity> seenEntities = new();

#pragma warning disable CA2213
    private Font smallerFont = null!;
    private Camera? activeCamera;
#pragma warning restore CA2213

    private IWorldSimulation? labelsActiveForSimulation;

    private bool showEntityLabels;

    private bool ShowEntityLabels
    {
        get => showEntityLabels;
        set
        {
            showEntityLabels = value;
            labelsLayer.Visible = value;
        }
    }

    public void UpdateActiveEntities(IWorldSimulation worldSimulation)
    {
        if (!ShowEntityLabels)
            return;

        // Only one world at a time can show labels so clear existing labels if the world changes
        if (worldSimulation != labelsActiveForSimulation)
            ClearEntityLabels();

        // Detect new entities
        foreach (var entity in worldSimulation.EntitySystem)
        {
            // Only display positional entities
            if (!entity.Has<WorldPosition>())
                return;

            seenEntities.Add(entity);

            if (!entityLabels.TryGetValue(entity, out _))
            {
                // New entity seen
                OnEntityAdded(entity);
            }
        }

        // Delete labels for gone entities
        var toDelete = entityLabels.Keys.Where(k => !seenEntities.Contains(k)).ToList();

        foreach (var entity in toDelete)
        {
            OnEntityRemoved(entity);
        }

        seenEntities.Clear();
    }

    public void OnWorldDisabled(IWorldSimulation? worldSimulation)
    {
        if (labelsActiveForSimulation == worldSimulation)
            ClearEntityLabels();
    }

    private bool UpdateLabelColour(Entity entity, Label label)
    {
        if (!entity.IsAlive)
        {
            label.AddColorOverride("font_color", new Color(1.0f, 0.3f, 0.3f));
            return false;
        }

        if (entity.Has<MicrobeControl>())
        {
            ref var control = ref entity.Get<MicrobeControl>();

            switch (control.State)
            {
                case MicrobeState.Binding:
                {
                    label.AddColorOverride("font_color", new Color(0.2f, 0.5f, 0.0f));
                    break;
                }

                case MicrobeState.Engulf:
                {
                    label.AddColorOverride("font_color", new Color(0.2f, 0.5f, 1.0f));
                    break;
                }

                case MicrobeState.Unbinding:
                {
                    label.AddColorOverride("font_color", new Color(1.0f, 0.5f, 0.2f));
                    break;
                }

                default:
                {
                    label.AddColorOverride("font_color", new Color(1.0f, 1.0f, 1.0f));
                    break;
                }
            }
        }

        return true;
    }

    private void UpdateEntityLabels()
    {
        if (!IsInstanceValid(activeCamera) || activeCamera is not { Current: true })
            activeCamera = GetViewport().GetCamera();

        if (activeCamera == null)
            return;

        foreach (var pair in entityLabels)
        {
            var entity = pair.Key;
            var label = pair.Value;

            if (!UpdateLabelColour(entity, label))
            {
                // Entity is dead can't reposition. Will be deleted the next time UpdateActiveEntities is called
                continue;
            }

            if (!entity.Has<WorldPosition>())
            {
                GD.PrintErr("Entity with a debug label no longer has a position");
                continue;
            }

            ref var position = ref entity.Get<WorldPosition>();

            label.RectPosition = activeCamera.UnprojectPosition(position.Position);

            if (!label.Text.Empty())
                continue;

            // Update names

            // TODO: chunks used to have their label be $"[{entity}:{chunk.ChunkName}]"
            // Chunk configuration is not currently saved so the chunk name is not really
            if (entity.Has<SpeciesMember>())
            {
                var species = entity.Get<SpeciesMember>().Species;

                label.Text = $"[{entity}:{species.Genus.Left(1)}.{species.Epithet.Left(4)}]";
                continue;
            }

            if (entity.Has<ReadableName>())
            {
                // TODO: localization support? Should all labels be re-initialized on language change?

                // TODO: some entities would probably be fine with not displaying the entity reference before the
                // readable name
                label.Text = $"[{entity}:{entity.Get<ReadableName>().Name}]";
                continue;
            }

            // Fallback to just showing the raw entity reference, nothing else can be shown
            label.Text = $"[{entity}]";
        }
    }

    private void OnEntityAdded(Entity entity)
    {
        var label = new Label();
        labelsLayer.AddChild(label);
        entityLabels.Add(entity, label);

        // This used to check for floating chunk, but now this just has to do with by checking a couple of components
        // that all chunks have at least one of. Projectile is still easy to check with the toxin damage source.
        if (entity.Has<ToxinDamageSource>() || entity.Has<CompoundVenter>() || entity.Has<DamageOnTouch>())
        {
            // To reduce the labels overlapping each other
            label.AddFontOverride("font", smallerFont);
        }
    }

    private void OnEntityRemoved(Entity entity)
    {
        if (entityLabels.TryGetValue(entity, out var label))
        {
            label.DetachAndQueueFree();
            entityLabels.Remove(entity);
        }
    }

    private void ClearEntityLabels()
    {
        foreach (var entityLabelsKey in entityLabels.Keys.ToList())
            OnEntityRemoved(entityLabelsKey);

        activeCamera = null;
        labelsActiveForSimulation = null;
    }
}
