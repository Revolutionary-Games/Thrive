using System;
using Godot;
using Newtonsoft.Json;

[JSONAlwaysDynamicType]
public class CellBurstEffect : SimulatedEntity, ISimulatedEntityWithDirectVisuals, ITimedLife
{
    private static readonly Lazy<PackedScene> VisualsScene =
        new(() => GD.Load<PackedScene>("res://src/microbe_stage/particles/CellBurstEffect.tscn"));

    [JsonProperty]
    public float Radius { get; set; }

    [JsonProperty]
    public float TimeToLiveRemaining { get; set; }

    [JsonProperty]
    public float? FadeTimeRemaining { get; set; }

    [JsonIgnore]
    public Spatial VisualNode { get; private set; } = null!;

    public override void OnAddedToSimulation(IWorldSimulation simulation)
    {
        base.OnAddedToSimulation(simulation);

        var particles = VisualsScene.Value.Instance<Particles>();

        TimeToLiveRemaining = particles.Lifetime;

        var material = (ParticlesMaterial)particles.ProcessMaterial;

        material.EmissionSphereRadius = Radius / 2;
        material.LinearAccel = Radius / 2;
        particles.OneShot = true;

        VisualNode = particles;
    }

    public override void Process(float delta)
    {
    }

    public void OnTimeOver()
    {
    }
}
