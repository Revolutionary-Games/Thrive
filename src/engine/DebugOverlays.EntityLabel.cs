using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Components;
using Godot;

/// <summary>
///   Partial class: Entity label
/// </summary>
public partial class DebugOverlays
{
    private const float TextUpdateInterval = 0.2f;

    private readonly Dictionary<Entity, Label> entityLabels = new();

    private readonly HashSet<Entity> seenEntities = new();

#pragma warning disable CA2213
    [Export]
    private LabelSettings entityLabelSmallFont = null!;

    [Export]
    private LabelSettings entityLabelDefaultFont = null!;

    [Export]
    private LabelSettings entityDeadFont = null!;

    [Export]
    private LabelSettings entityBindingFont = null!;

    [Export]
    private LabelSettings entityEngulfingFont = null!;

    [Export]
    private LabelSettings entityUnbindingFont = null!;

    private Camera3D? activeCamera;
#pragma warning restore CA2213

    private IWorldSimulation? labelsActiveForSimulation;

    private bool showEntityLabels;

    private double textUpdateTimer;

    private bool ShowEntityLabels
    {
        get => showEntityLabels;
        set
        {
            showEntityLabels = value;
            labelsLayer.Visible = value;
        }
    }

    public static string FormatEntityDebugLabel(Entity entity)
    {
        if (!entity.IsAliveAndNotNull())
            return $"[{entity.Id}-{entity.Version}]";

        // TODO: chunks used to have their label be $"[{entity}:{chunk.ChunkName}]".
        // Chunk configuration is not currently saved so the chunk name is not really available.
        string text;

        if (entity.Has<SpeciesMember>())
        {
            var species = entity.Get<SpeciesMember>().Species;

            text = $"[{entity.Id}-{entity.Version}:{species.Genus.Left(1)}.{species.Epithet.Left(4)}]";
        }
        else if (entity.Has<ReadableName>())
        {
            // TODO: localization support? Should all labels be re-initialized on language change?

            // TODO: some entities would probably be fine with not displaying the entity reference before the
            // readable name
            text = $"[{entity.Id}-{entity.Version}:{entity.Get<ReadableName>().Name}]";
        }
        else
        {
            // Fallback to just showing the raw entity reference, nothing else can be shown
            text = $"[{entity.Id}-{entity.Version}]";
        }

        // Showing signalling agent state for debugging AI
        if (entity.Has<CommandSignaler>())
        {
            ref var signaler = ref entity.Get<CommandSignaler>();

            if (signaler.Command != MicrobeSignalCommand.None)
            {
                // This is on a new line as otherwise things would be a bit long
                text += $"\n{signaler.Command}";
            }
        }

        return text;
    }

    public void UpdateActiveEntities(IWorldSimulation worldSimulation)
    {
        if (!ShowEntityLabels)
            return;

        // Only one world at a time can show labels so clear as existing labels if the world changes
        if (worldSimulation != labelsActiveForSimulation)
            ClearEntityLabels();

        // Detect new entities
        foreach (var archetype in worldSimulation.EntitySystem)
        {
            foreach (var chunk in archetype)
            {
                var count = chunk.Count;
                var entities = chunk.Entities;

                for (int i = 0; i < count; ++i)
                {
                    var entity = entities[i];

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

    private static bool IsEntityAliveAndHasWorldPosition(Entity entity)
    {
        // The world null check here is against a disposed world that still has a valid index.
        if (entity == Entity.Null || entity.IsAllZero() || entity.WorldId < 0 ||
            entity.WorldId >= World.Worlds.Length ||
            World.Worlds[entity.WorldId] == null!)
        {
            return false;
        }

        return entity.IsAliveAndHas<WorldPosition>();
    }

    private bool UpdateLabelColour(Entity entity, Label label)
    {
        // Do not use EntityExtensions.IsAlive here. Labels can outlive the world they were created from, in which case
        // Arch's global world lookup returns null and IsAlive throws instead of returning false.
        if (!IsEntityAliveAndHasWorldPosition(entity))
        {
            label.LabelSettings = entityDeadFont;
            return false;
        }

        if (entity.Has<MicrobeControl>())
        {
            ref var control = ref entity.Get<MicrobeControl>();

            switch (control.State)
            {
                case MicrobeState.Binding:
                {
                    label.LabelSettings = entityBindingFont;
                    break;
                }

                case MicrobeState.Engulf:
                {
                    label.LabelSettings = entityEngulfingFont;
                    break;
                }

                case MicrobeState.Unbinding:
                {
                    label.LabelSettings = entityUnbindingFont;
                    break;
                }

                default:
                {
                    label.LabelSettings = entityLabelDefaultFont;
                    break;
                }
            }
        }

        return true;
    }

    private void UpdateEntityLabels(double delta)
    {
        if (!IsInstanceValid(activeCamera) || activeCamera is not { Current: true } || !activeCamera.IsInsideTree())
            activeCamera = GetViewport().GetCamera3D();

        if (activeCamera == null || !activeCamera.IsInsideTree())
            return;

        textUpdateTimer -= delta;
        var updateText = textUpdateTimer <= 0;

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

            label.Position = activeCamera.UnprojectPosition(position.Position);

            if (updateText)
            {
                var newText = FormatEntityDebugLabel(entity);

                if (label.Text != newText)
                    label.Text = newText;
            }
        }

        if (updateText)
            textUpdateTimer = TextUpdateInterval;
    }

    private void OnEntityAdded(Entity entity)
    {
        var label = new Label();
        labelsLayer.AddChild(label);
        entityLabels.Add(entity, label);
        label.Text = FormatEntityDebugLabel(entity);

        // This used to check for floating chunk, but now this just has to do with by checking a couple of components
        // that all chunks have at least one of. Projectile is still easy to check with the toxin damage source.
        if (entity.Has<ToxinDamageSource>() || entity.Has<CompoundVenter>() || entity.Has<DamageOnTouch>())
        {
            // To reduce the labels overlapping each other
            label.LabelSettings = entityLabelSmallFont;
        }
    }

    private void OnEntityRemoved(Entity entity)
    {
        // TODO: pooling for entity labels?
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
