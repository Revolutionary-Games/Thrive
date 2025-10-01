namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;
using Godot;

/// <summary>
///   Creates and applies light properties in a <see cref="SpatialInstance"/> from <see cref="EntityLight"/> data
/// </summary>
[RunsOnMainThread]
[RuntimeCost(0.5f)]
public partial class EntityLightSystem : BaseSystem<World, float>
{
    // TODO: light quality selection (this is lower quality)
    private OmniLight3D.ShadowMode lightShadowMode = OmniLight3D.ShadowMode.DualParaboloid;

    public EntityLightSystem(World world) : base(world)
    {
        // The low-quality light is not supported in fallback renderer, so switch to the other light mode if that
        // is detected
        if (lightShadowMode != OmniLight3D.ShadowMode.Cube &&
            FeatureInformation.GetVideoDriver() == OS.RenderingDriver.Opengl3)
        {
            lightShadowMode = OmniLight3D.ShadowMode.Cube;
            GD.PrintErr("Falling back to cube shadows for light quality due to used renderer");
        }
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref EntityLight entityLight, ref SpatialInstance spatialInstance)
    {
        if (entityLight.LightsApplied)
            return;

        // Wait until graphics initialised
        if (spatialInstance.GraphicalInstance == null)
            return;

        var lights = entityLight.Lights;

        if (lights is { Length: > 0 })
        {
            var count = lights.Length;

            for (int i = 0; i < count; ++i)
            {
                var data = lights[i];

                if (data.Enabled)
                {
                    // Create or update a light instance
                    if (data.CreatedLight == null)
                    {
                        var light = new OmniLight3D
                        {
                            LightColor = data.Color,
                            OmniRange = data.Range,
                            OmniAttenuation = data.Attenuation,
                            LightEnergy = data.Intensity,

                            // TODO: tweak these settings / allow changing these per-entity or per-stage
                            ShadowEnabled = true,
                            DistanceFadeEnabled = true,
                            DistanceFadeBegin = 80,
                            DistanceFadeLength = 30,
                            DistanceFadeShadow = 100,

                            OmniShadowMode = lightShadowMode,
                        };

                        spatialInstance.GraphicalInstance.AddChild(light);
                        light.Position = data.Position;
                        data.CreatedLight = light;

                        // Must save the modified data
                        lights[i] = data;
                    }
                    else
                    {
                        // Update the light instance
                        var light = data.CreatedLight;
                        light.Position = data.Position;
                        light.LightColor = data.Color;
                        light.OmniRange = data.Range;
                        light.OmniAttenuation = data.Attenuation;
                        light.LightEnergy = data.Intensity;
                        light.Visible = true;
                    }
                }
                else
                {
                    // Disable light if created
                    if (data.CreatedLight != null)
                        data.CreatedLight.Visible = false;
                }
            }
        }

        entityLight.LightsApplied = true;
    }
}
