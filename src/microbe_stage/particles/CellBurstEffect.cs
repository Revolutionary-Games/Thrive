using Godot;
using Newtonsoft.Json;

[JSONAlwaysDynamicType]
[SceneLoadedClass("res://src/microbe_stage/particles/CellBurstEffect.tscn", UsesEarlyResolve = false)]
public class CellBurstEffect : Spatial, ITimedLife
{
    [JsonProperty]
    public float Radius;

    private Particles particles;

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
