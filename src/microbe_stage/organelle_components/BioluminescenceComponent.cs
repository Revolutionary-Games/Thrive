using System.Collections.Generic;
using Components;
using DefaultEcs;
using Godot;
using Newtonsoft.Json;
using ThriveScriptsShared;

/// <summary>
///   Adds toxin shooting capability
/// </summary>
#pragma warning disable CA1001 // components don't support dispose
public class BioluminescenceComponent : IOrganelleComponent
#pragma warning restore CA1001
{
    private readonly float consumption;
    private readonly float emissionStrength;
    private readonly float lightStrength;

    private readonly Color lightColour;

    private readonly StringName emissionParameterName = new("emissionEnergy");

    private bool lastActiveState;

    private bool newActiveState;

    private PlacedOrganelle? parentOrganelle;

    private ShaderMaterial? currentMaterial;

    private bool warnedAboutOutOfSlots;

    public BioluminescenceComponent(float consumptionSpeed, float emissionStrength, float lightStrength,
        Color lightColour)
    {
        consumption = consumptionSpeed;
        this.emissionStrength = emissionStrength;
        this.lightStrength = lightStrength;
        this.lightColour = lightColour;
    }

    /// <summary>
    ///   Uses the sync process when needs to update
    /// </summary>
    public bool UsesSyncProcess => newActiveState != lastActiveState;

    public void OnAttachToCell(PlacedOrganelle organelle)
    {
        parentOrganelle = organelle;
    }

    public void UpdateAsync(ref OrganelleContainer organelleContainer, in Entity microbeEntity,
        IWorldSimulation worldSimulation, float delta)
    {
        // Drain luciferase
        var compounds = microbeEntity.Get<CompoundStorage>().Compounds;

        var required = delta * consumption;

        if (compounds.TakeCompound(Compound.Luciferase, required) < required)
        {
            // Cannot stay active without enough compounds
            newActiveState = false;
        }
        else
        {
            newActiveState = true;
        }
    }

    public void UpdateSync(in Entity microbeEntity, float delta)
    {
        if (currentMaterial == null)
        {
            // Wait until initialised
            if (parentOrganelle?.OrganelleGraphics == null)
                return;

            if (!parentOrganelle.Definition.TryGetGraphicsScene(null, out var modelInfo))
            {
                GD.PrintErr("No graphics info could be fetched from bioluminescence parent organelle");
                return;
            }

            // TODO: is there some place that could cache these?
            // Bioluminescence models are single parts currently so this doesn't really need to be a list
            var temp = new List<ShaderMaterial>();

            parentOrganelle.OrganelleGraphics.GetMaterial(temp, modelInfo.ModelPath);

            if (temp.Count == 0)
            {
                GD.PrintErr("No material found for bioluminescence to update");
                return;
            }

            currentMaterial = temp[0];
        }

        if (parentOrganelle == null)
        {
            GD.PrintErr("Parent organelle shouldn't be null after material fetch");
            return;
        }

        currentMaterial.SetShaderParameter(emissionParameterName, newActiveState ? emissionStrength : 0);

        // Update light values if we are working on the player (who has real lights attached)
        if (microbeEntity.Has<EntityLight>())
        {
            // This works on one light info at once (determined by the position), and relies on the organelle layout
            // reset to reset the light properties for removed components.
            // It shouldn't be possible for the organelle position to change after adding, so we don't trigger periodic
            // checks to re-check the position.
            var position = Hex.AxialToCartesian(parentOrganelle.Position);

            ref var light = ref microbeEntity.Get<EntityLight>();

            // TODO: should this gradually apply the light level change?

            if (newActiveState)
            {
                // Create missing light update existing, and also apply our position
                // Note that the other bioluminescence components will update their own data, so we must only update our
                // data

                EntityLight.Light[]? lights;
                if (light.Lights == null)
                {
                    // Create a new lights list if missing
                    lights = new EntityLight.Light[Constants.ENTITY_REASONABLE_MAX_LIGHTS];
                    light.Lights = lights;
                }
                else
                {
                    lights = light.Lights;
                }

                if (lights == null)
                {
                    GD.PrintErr("Failed to get light data for bioluminescence component");
                    return;
                }

                // Activate our light if it is found in the list
                bool found = false;
                int count = lights.Length;
                for (int i = 0; i < count; ++i)
                {
                    var data = lights[i];

                    if (data.Position != position)
                        continue;

                    found = true;
                    data.Enabled = true;
                    lights[i] = data;
                    light.LightsApplied = false;
                }

                // If not found, we need to add a new light to the list
                if (!found)
                {
                    // TODO: how can we detect the lights list is full? Now we will just cause some bioluminescent
                    // components to fight and maybe flip around a light

                    // Add new
                    for (int i = 0; i < count; ++i)
                    {
                        var data = lights[i];

                        // For now, we assume that any slots that are enabled are in use permanently
                        if (!data.Enabled)
                        {
                            found = true;
                            data.Enabled = true;
                            data.Position = position;
                            data.Intensity = lightStrength;
                            data.Range = Constants.ENTITY_BIOLUMINESCENCE_LIGHT_RANGE;
                            data.Color = lightColour;
                            data.Attenuation = Constants.ENTITY_LIGHT_REALISTIC_ATTENUATION;

                            lights[i] = data;
                            light.LightsApplied = false;
                            break;
                        }
                    }

                    if (!found)
                    {
                        if (!warnedAboutOutOfSlots)
                        {
                            GD.Print("Not enough light slots to add to bioluminescence component");
                            warnedAboutOutOfSlots = true;
                        }
                    }
                }
            }
            else
            {
                // Disable the light that matches us
                if (light.Lights != null)
                {
                    var lights = light.Lights;
                    var count = lights.Length;
                    for (int i = 0; i < count; ++i)
                    {
                        var data = lights[i];

                        if (data.Position != position)
                            continue;

                        if (data.Enabled)
                        {
                            data.Enabled = false;
                            light.LightsApplied = false;
                            lights[i] = data;

                            // Each component just updates its own state, so once we find that, we can break
                            break;
                        }
                    }
                }
            }
        }

        lastActiveState = newActiveState;
    }
}

public class BioluminescenceComponentFactory : IOrganelleComponentFactory
{
    [JsonRequired]
    public float Consumption = 1;

    public float EmissionStrength = 20;

    public float LightStrength = 5;

    public Color LightColour = new(0.33f, 0.79f, 1);

    public IOrganelleComponent Create()
    {
        return new BioluminescenceComponent(Consumption, EmissionStrength, LightStrength, LightColour);
    }

    public void Check(string name)
    {
        if (Consumption < 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Consumption may not be negative");

        if (EmissionStrength < 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Emission strength may not be negative");

        if (LightStrength < 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Light strength may not be negative");

        // Alpha is always one to make the light make sense
        LightColour.A = 1;

        if (LightColour.R + LightColour.G + LightColour.B < 0.1f)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Light colour must not be black");
        }
    }
}
