namespace Components;

using Godot;
using Newtonsoft.Json;

[JSONDynamicTypeAllowed]
public struct IntercellularMatrix
{
    [JsonIgnore]
    public Node3D? GeneratedConnection;
}
