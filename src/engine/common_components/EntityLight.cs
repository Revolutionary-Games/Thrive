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
        public Color Color;
        public Vector3 Position;

        /// <summary>
        ///   Don't touch, internal variable used by <see cref="EntityLightSystem"/>
        /// </summary>
        [JsonIgnore]
        public OmniLight3D? CreatedLight;

        public float Intensity;
        public float Range;
        public float Attenuation;

        public bool Enabled;
    }
}

public static class EntityLightHelpers
{
    public static void DisableAllLights(this ref EntityLight entityLight)
    {
        entityLight.LightsApplied = false;

        var lights = entityLight.Lights;
        if (lights != null)
        {
            int count = lights.Length;
            for (int i = 0; i < count; ++i)
            {
                lights[i].Enabled = false;
            }
        }
    }
}
