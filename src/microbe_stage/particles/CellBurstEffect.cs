using Godot;

public class CellBurstEffect : Particles, ITimedLife
{
    public float TimeToLiveRemaining { get; set; }

    public Microbe Host;

    public override void _Ready()
    {
        TimeToLiveRemaining = Lifetime;

        var material = (ParticlesMaterial)ProcessMaterial;

        material.EmissionSphereRadius = Host.Radius / 2;
        material.LinearAccel = Host.Radius / 2;
        OneShot = true;
    }

    public void OnTimeOver()
    {
        this.DetachAndQueueFree();
    }
}
