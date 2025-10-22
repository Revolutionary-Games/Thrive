using System.Collections.Generic;
using Arch.Core;
using Saving.Serializers;
using SharedBase.Archive;

/// <summary>
///   Keeps track of entities that should not be saved. This is a separate class to make save converters possible to
///   do special actions on it.
/// </summary>
/// <remarks>
///   <para>
///     It is a bit hacky how this sets <see cref="ThriveArchiveManager.UnsavedEntities"/> when this is being saved.
///     That works as long as this is always serialized before the entity world.
///   </para>
/// </remarks>
public sealed class UnsavedEntities : IArchiveUpdatable
{
    private readonly List<Entity> entities = new();

    private IReadOnlyCollection<Entity>? additionalIgnoreSource;

    /// <summary>
    ///   Allows setting an additional source of entities to ignore saving. This is used to not save the entities that
    ///   should be destroyed in a World (if save happens to trigger while there are pending deletes)
    /// </summary>
    /// <param name="additionalIgnores">
    ///   Additional entities to ignore (this list is only read once a save is being made)
    /// </param>
    public UnsavedEntities(IReadOnlyCollection<Entity> additionalIgnores)
    {
        additionalIgnoreSource = additionalIgnores;
    }

    public ushort CurrentArchiveVersion => 1;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.UnsavedEntities;

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

    public void ActivateOnContext(ISaveContext context)
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

    public void SetExtraIgnoreSource(IReadOnlyCollection<Entity> additionalIgnores)
    {
        additionalIgnoreSource = additionalIgnores;
    }

    public void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        ActivateOnContext((ISaveContext)writer.WriteManager);
    }

    public void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
    }
}
