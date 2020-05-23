using Godot;

public class MicrobeDeathEffect : Spatial, ITimedLife
{
    [Export]
    public NodePath MembraneBitsParticlesPath;

    public float EmissionRadius;

    private Particles membraneBitsParticles;

    public float TimeToLiveRemaining { get; set; } = 5.0f;

    public void OnTimeOver() => QueueFree();
    public override void _Ready()
    {
        membraneBitsParticles = GetNode<Particles>(MembraneBitsParticlesPath);

        var membraneBitsMaterial = (ParticlesMaterial)membraneBitsParticles.ProcessMaterial;
        membraneBitsMaterial.EmissionSphereRadius = EmissionRadius;

        AddToGroup(Constants.TIMED_GROUP);
    }
}
