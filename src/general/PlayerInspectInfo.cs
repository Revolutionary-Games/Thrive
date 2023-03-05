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
    public float RayastDistance = 1000;

    private readonly List<RaycastResult> hits = new();
    private readonly HashSet<RaycastResult> previousHits = new();

    /// <summary>
    ///   All inspectable entities the player is pointing.
    /// </summary>
    public IReadOnlyList<IInspectableEntity> InspectableEntities =>
        hits.Select(h => h.Collider).OfType<IInspectableEntity>().ToList();

    public virtual void Process(float delta)
    {
        var space = GetViewport().World.DirectSpaceState;
        var mousePos = GetViewport().GetMousePosition();
        var camera = GetViewport().GetCamera();

        var from = camera.ProjectRayOrigin(mousePos);
        var to = from + camera.ProjectRayNormal(mousePos) * RayastDistance;

        hits.Clear();

        space.IntersectRay(hits, from, to);

        foreach (var hit in previousHits.ToHashSet())
        {
            if (hits.Contains(hit))
                continue;

            previousHits.Remove(hit);

            if (hit.Collider is IInspectableEntity entity)
            {
                entity.OnMouseExit(hit);
            }
        }

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
    ///   Returns the raycast data of the given raycasted inspectable entity.
    /// </summary>
    public RaycastResult GetRaycastData(IInspectableEntity entity)
    {
        var result = hits.FirstOrDefault(h => h.Collider == entity);
        return result;
    }
}
