using Godot;
using Newtonsoft.Json;

[JSONAlwaysDynamicType]
[SceneLoadedClass("res://src/microbe_stage/particles/CellBurstEffect.tscn", UsesEarlyResolve = false)]
public class CellBurstEffect : Spatial, ITimedLife
{
    [JsonProperty]
    public float Radius;

#pragma warning disable CA2213
    private Particles particles = null!;
#pragma warning restore CA2213

    public float TimeToLiveRemaining { get; set; }

    public override void _Ready()
    {
        particles = GetNode<Particles>("Particles");

        TimeToLiveRemaining = particles.Lifetime;

        var material = (ParticlesMaterial)particles.ProcessMaterial;

        material.EmissionSphereRadius = Radius / 2;
        material.LinearAccel = Radius / 2;
        particles.OneShot = true;
    }

    public void OnTimeOver()
    {
        this.DetachAndQueueFree();
    }
}
