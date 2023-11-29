using System.Collections.Generic;
using DefaultEcs;
using Newtonsoft.Json;

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

    private IReadOnlyCollection<Entity>? additionalIgnoreSource;

    [JsonConstructor]
    public UnsavedEntities()
    {
    }

    /// <summary>
    ///   Allows setting an additional source of entities to ignore saving. This is used to not save the entities that
    ///   should be destroyed in a world (if save happens to trigger while there are pending deletes)
    /// </summary>
    /// <param name="additionalIgnores">
    ///   Additional entities to ignore (this list is only read once a save is being made)
    /// </param>
    public UnsavedEntities(IReadOnlyCollection<Entity> additionalIgnores)
    {
        additionalIgnoreSource = additionalIgnores;
    }

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

        if (additionalIgnoreSource != null)
        {
            foreach (var entity in additionalIgnoreSource)
            {
                unsaved.Add(entity);
            }
        }
    }
}
