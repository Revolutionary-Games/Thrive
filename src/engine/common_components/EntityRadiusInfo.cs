namespace Components;

/// <summary>
///   Entity is roughly circular and this provides easy access to that entity's radius
/// </summary>
/// <remarks>
///   <para>
///     This component type was added as I wasn't confident enough in remaking the
///     <see cref="Systems.EngulfingSystem"/> without having access to microbe chunk radius when calculating engulf
///     positions -hhyyrylainen
///   </para>
/// </remarks>
[JSONDynamicTypeAllowed]
public struct EntityRadiusInfo
{
    public float Radius;

    public EntityRadiusInfo(float radius)
    {
        Radius = radius;
    }
}
