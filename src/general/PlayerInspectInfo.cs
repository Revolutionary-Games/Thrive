using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   A system that manages detecting what the player is pointing with the cursor.
/// </summary>
public class PlayerInspectInfo : Node
{
    /// <summary>
    ///   The distance for detection.
    /// </summary>
    [Export]
    public float RaycastDistance = 1000;

    private readonly List<RaycastResult> hits = new();
    private readonly HashSet<RaycastResult> previousHits = new();

    /// <summary>
    ///   All inspectable entities the player is pointing at.
    /// </summary>
    public IEnumerable<IInspectableEntity> InspectableEntities =>
        hits.Select(h => h.Collider).OfType<IInspectableEntity>();

    public virtual void Process(float delta)
    {
        var viewport = GetViewport();
        var space = viewport.World.DirectSpaceState;
        var mousePos = viewport.GetMousePosition();
        var camera = viewport.GetCamera();

        var from = camera.ProjectRayOrigin(mousePos);
        var to = from + camera.ProjectRayNormal(mousePos) * RaycastDistance;

        hits.Clear();

        space.IntersectRay(hits, from, to);

        previousHits.RemoveWhere(m =>
        {
            if (!hits.Contains(m))
            {
                if (m.Collider is IInspectableEntity entity)
                {
                    entity.OnMouseExit(m);
                }

                return true;
            }

            return false;
        });

        foreach (var hit in hits)
        {
            if (!previousHits.Add(hit))
                continue;

            if (hit.Collider is IInspectableEntity entity)
            {
                entity.OnMouseEnter(hit);
            }
        }
    }

    /// <summary>
    ///   Returns the raycast data of the given raycast inspectable entity.
    /// </summary>
    /// <returns>The raycast data or null if not found.</returns>
    public RaycastResult? GetRaycastData(IInspectableEntity entity)
    {
        try
        {
            return hits.First(h => h.Collider == entity);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
