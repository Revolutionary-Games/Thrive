namespace Components;

using Godot;
using Newtonsoft.Json;

[JSONDynamicTypeAllowed]
public struct IntercellularMatrix
{
    /// <summary>
    ///   True when the cell doesn't need a connection because it's already close enough, false otherwise.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Should be reset to false when the cell gets disconnected.
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public bool IsConnectionRedundant;

    [JsonIgnore]
    public Node3D? GeneratedConnection;
}
