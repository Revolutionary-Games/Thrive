using System.Collections.Generic;
using DefaultEcs;

/// <summary>
///   Keeps track of entities that should not be saved. This is a separate class to make save converters possible to
///   do special actions on it.
/// </summary>
/// <remarks>
///   <para>
///     It is a bit hacky how this sets <see cref="SaveContext.UnsavedEntities"/> when this is being saved. That works
///     as long as this is always serialized before the entity system.
///   </para>
/// </remarks>
public sealed class UnsavedEntities
{
    private readonly List<Entity> entities = new();

    public void Add(in Entity entity)
    {
        entities.Add(entity);
    }

    public bool Contains(in Entity entity)
    {
        return entities.Contains(entity);
    }

    public bool Remove(in Entity entity)
    {
        return entities.Remove(entity);
    }

    public void ActivateOnContext(SaveContext context)
    {
        var unsaved = context.UnsavedEntities;

        foreach (var entity in entities)
        {
            unsaved.Add(entity);
        }
    }
}
