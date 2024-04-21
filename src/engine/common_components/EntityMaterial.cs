namespace Components;

using Godot;
using Newtonsoft.Json;

/// <summary>
///   Access to a material defined on an entity
/// </summary>
[JSONDynamicTypeAllowed]
public struct EntityMaterial
{
    [JsonIgnore]
    public ShaderMaterial[]? Materials;

    /// <summary>
    ///   If not null then <see cref="AutoRetrieveFromSpatial"/> uses this as the relative path from the
    ///   <see cref="Node3D"/> node to where the material is retrieved from
    /// </summary>
    public string? AutoRetrieveModelPath;

    /// <summary>
    ///   When true and this entity has a <see cref="SpatialInstance"/> component the material is automatically
    ///   fetched
    /// </summary>
    public bool AutoRetrieveFromSpatial;

    /// <summary>
    ///   If set to true then the <see cref="AutoRetrieveFromSpatial"/> takes the scene attached node directly.
    ///   If false then this skips one parent level and gets the first child of the attached node and looks up the
    ///   material from there.
    /// </summary>
    public bool AutoRetrieveAssumesNodeIsDirectlyAttached;

    /// <summary>
    ///   Internal flag, don't modify
    /// </summary>
    [JsonIgnore]
    public bool MaterialFetchPerformed;
}
