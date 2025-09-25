namespace Components;

using System.Collections.Generic;
using Arch.Core;

/// <summary>
///   Marks an entity as emitting radiation
/// </summary>
[JSONDynamicTypeAllowed]
public struct RadiationSource
{
    public float RadiationStrength;
    public float Radius;

    public HashSet<Entity>? RadiatedEntities;
}
