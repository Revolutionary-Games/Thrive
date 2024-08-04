using System.Collections.Generic;
using System.Linq;
using Components;
using DefaultEcs;
using Godot;

/// <summary>
///   A system that manages detecting what the player is pointing with the cursor.
/// </summary>
public partial class PlayerInspectInfo : Node
{
    /// <summary>
    ///   The distance for detection.
    /// </summary>
    [Export]
    public float RaycastDistance = 1000;

    private readonly PhysicsRayWithUserData[] hits = new PhysicsRayWithUserData[Constants.MAX_RAY_HITS_FOR_INSPECT];
    private readonly HashSet<Entity> previousHits = new();

    private int validHits;

    /// <summary>
    ///   Needs to be set to the physical world to use
    /// </summary>
    public PhysicalWorld? PhysicalWorld { get; set; }

    /// <summary>
    ///   All (physics) entities the player is pointing at.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: switch this away from LINQ to reduce memory allocations
    ///   </para>
    /// </remarks>
    public IEnumerable<Entity> Entities =>
        hits.Take(validHits).Where(h => h.BodyEntity != default).Select(h => h.BodyEntity);

    public virtual void Process(double delta)
    {
        var viewport = GetViewport();
        var camera = viewport.GetCamera3D();
        var mousePos = viewport.GetMousePosition();
        mousePos = ApplyScreenEffects(mousePos, viewport.GetVisibleRect().Size);

        // Safety check to disable this node when there's no active camera
        if (camera == null)
            return;

        if (PhysicalWorld == null)
        {
            GD.PrintErr($"{nameof(PlayerInspectInfo)} doesn't have physics world set");
            return;
        }

        var from = camera.ProjectRayOrigin(mousePos);
        var offsetToEnd = camera.ProjectRayNormal(mousePos) * RaycastDistance;

        validHits = PhysicalWorld.CastRayGetAllHits(from, offsetToEnd, hits);

        // Process hits to real microbes (as in colonies the body hit is the colony leader always)
        for (int i = 0; i < validHits; ++i)
        {
            var originalHitEntity = hits[i].BodyEntity;
            if (originalHitEntity.Has<MicrobeColony>() && originalHitEntity.Has<PhysicsShapeHolder>())
            {
                var shape = originalHitEntity.Get<PhysicsShapeHolder>().Shape;

                if (shape == null)
                {
                    GD.PrintErr("Ray hit entity with unknown shape");
                    continue;
                }

                ref var colony = ref originalHitEntity.Get<MicrobeColony>();
                if (colony.GetMicrobeFromSubShape(ref originalHitEntity.Get<MicrobePhysicsExtraData>(),
                        shape.GetSubShapeIndexFromData(hits[i].SubShapeData), out var actualMicrobe))
                {
                    hits[i] = new PhysicsRayWithUserData(hits[i], actualMicrobe);
                }
            }
        }

        previousHits.RemoveWhere(m =>
        {
            if (hits.Take(validHits).All(h => h.BodyEntity != m))
            {
                // Hit removed
                if (m.IsAlive && m.Has<Selectable>())
                {
                    ref var selectable = ref m.Get<Selectable>();
                    selectable.Selected = false;
                }

                return true;
            }

            return false;
        });

        foreach (var hit in hits.Take(validHits))
        {
            if (!previousHits.Add(hit.BodyEntity))
                continue;

            // New hit added

            if (hit.BodyEntity.IsAlive && hit.BodyEntity.Has<Selectable>())
            {
                ref var selectable = ref hit.BodyEntity.Get<Selectable>();
                selectable.Selected = true;
            }
        }
    }

    /// <summary>
    ///   Returns the raycast data of the given raycast hit entity. Note that the ray data doesn't have sub-shape index
    ///   resolved. Except for microbe colonies those are already processed at this point.
    /// </summary>
    /// <param name="entity">Entity to get the data for</param>
    /// <param name="rayData">Where to put the found ray data, initialized to default if not found</param>
    /// <returns>True when the data was found</returns>
    public bool GetRaycastData(Entity entity, out PhysicsRayWithUserData rayData)
    {
        for (int i = 0; i < validHits; ++i)
        {
            if (hits[i].BodyEntity == entity)
            {
                rayData = hits[i];
                return true;
            }
        }

        rayData = default;
        return false;
    }

    /// <summary>
    /// Applies screen effects to mouse position.
    /// </summary>
    /// <returns>
    /// True screen position of what visually is under cursor.
    /// </returns>
    protected virtual Vector2 ApplyScreenEffects(Vector2 mousePos, Vector2 viewportSize)
    {
        // There are no screen effects by default
        return mousePos;
    }
}
