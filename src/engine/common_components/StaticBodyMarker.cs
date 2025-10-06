namespace Components;

/// <summary>
///   Marks that an entity cannot be moved by physics thus excluding it from reading physics position data.
///   Makes static bodies much more efficient.
/// </summary>
[JSONDynamicTypeAllowed]
public struct StaticBodyMarker
{
}
