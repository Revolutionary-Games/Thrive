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

#pragma warning disable CA2213 // Disposable fields should be disposed
    private Viewport viewport = null!;
#pragma warning restore CA2213 // Disposable fields should be disposed

    /// <summary>
    ///   All inspectable entities the player is pointing at.
    /// </summary>
    public IEnumerable<IInspectableEntity> InspectableEntities =>
        hits.Select(h => h.Collider).OfType<IInspectableEntity>().ToList();

    public override void _Ready()
    {
        viewport = GetViewport();
    }

    public virtual void Process(float delta)
    {
        var space = viewport.World.DirectSpaceState;
        var mousePos = viewport.GetMousePosition();
        var camera = viewport.GetCamera();

        var from = camera.ProjectRayOrigin(mousePos);
        var to = from + camera.ProjectRayNormal(mousePos) * RaycastDistance;

        hits.Clear();

        space.IntersectRay(hits, from, to);

        foreach (var hit in previousHits)
        {
            if (hits.Contains(hit))
                continue;

            if (hit.Collider is IInspectableEntity entity)
            {
                entity.OnMouseExit(hit);
            }
        }

        previousHits.RemoveWhere(m => !hits.Contains(m));

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
        catch
        {
            return null;
        }
    }
}
