using System;
using System.Linq;
using System.Threading;
using Arch.Core;

/// <summary>
///   Helpers for creating Arch worlds in Thrive. This must be used instead of creating worlds directly!
/// </summary>
/// <remarks>
///   <para>
///     Arch's component registry is not safe for concurrent writers. Register all Thrive component types before any
///     world is created so that parallel world users only perform lookups afterwards.
///   </para>
/// </remarks>
public static class ThriveWorld
{
    private static readonly Lock ComponentInitializationLock = new();
    private static bool componentTypesInitialized;

    public static World Create(int chunkSizeInBytes = 16_384, int minimumAmountOfEntitiesPerChunk = 100,
        int archetypeCapacity = 2, int entityCapacity = 64)
    {
        EnsureComponentTypesInitialized();

        return World.Create(chunkSizeInBytes, minimumAmountOfEntitiesPerChunk, archetypeCapacity, entityCapacity);
    }

    private static void EnsureComponentTypesInitialized()
    {
        lock (ComponentInitializationLock)
        {
            if (componentTypesInitialized)
                return;

            var componentInterface = typeof(IArchivableComponent);
            var componentAttribute = typeof(ComponentIsReadByDefaultAttribute);
            int initializedComponentCount = 0;

            // TODO: extend this to mod components
            foreach (var type in componentInterface.Assembly.GetTypes()
                         .Where(type => !type.IsAbstract && !type.IsInterface &&
                             (componentInterface.IsAssignableFrom(type) ||
                                 type.IsDefined(componentAttribute, false))))
            {
                if (!ComponentRegistry.Has(type))
                    ComponentRegistry.Add(type);

                ++initializedComponentCount;
            }

            // At the time this is the component count in the game. This is so high to ensure that any components
            // aren't missed somehow due to namespaces or something silly.
            if (initializedComponentCount < 75)
                throw new Exception("Thrive ECS component types were not detected");

            componentTypesInitialized = true;
        }
    }
}
