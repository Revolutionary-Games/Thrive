namespace Systems;

using Components;
using DefaultEcs;
using DefaultEcs.System;
using Godot;

/// <summary>
///   Creates and applies light properties in a <see cref="SpatialInstance"/> from <see cref="EntityLight"/> data
/// </summary>
[With(typeof(EntityLight))]
[With(typeof(SpatialInstance))]
[RunsOnMainThread]
[RuntimeCost(0.5f)]
public class EntityLightSystem : AEntitySetSystem<float>
{
    public EntityLightSystem(World world) : base(world, null)
    {
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var entityLight = ref entity.Get<EntityLight>();

        if (entityLight.LightsApplied)
            return;

        ref var spatialInstance = ref entity.Get<SpatialInstance>();

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

                            // TODO: light quality selection (this is lower quality)
                            OmniShadowMode = OmniLight3D.ShadowMode.DualParaboloid,
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
