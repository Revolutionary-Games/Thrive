namespace Components;

using Godot;
using Newtonsoft.Json;
using Systems;

/// <summary>
///   Allows specifying lights on an entity to use with <see cref="EntityLightSystem"/>
/// </summary>
public struct EntityLight
{
    public Light[]? Lights;

    [JsonIgnore]
    public bool LightsApplied;

    public struct Light
    {
        public bool Enabled;
        public float Intensity;
        public float Range;
        public Vector3 Color;
    }
}
