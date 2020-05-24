using Godot;

public class MicrobeDeathEffect : Spatial, ITimedLife
{
    [Export]
    public NodePath MembraneBitsParticlesPath;

    public Microbe ParentMicrobe;

    private Particles membraneBitsParticles;

    public float TimeToLiveRemaining { get; set; } = 5.0f;

    public void OnTimeOver() => QueueFree();
    public override void _Ready()
    {
        membraneBitsParticles = GetNode<Particles>(MembraneBitsParticlesPath);

        Transform = ParentMicrobe.Transform;

        // Hide the particles since the they are supposed
        // to be "absorbed" by the engulfing cell
        if (ParentMicrobe.IsBeingEngulfed)
        {
            membraneBitsParticles.Hide();
        }

        var membraneBitsMaterial = (ParticlesMaterial)membraneBitsParticles.ProcessMaterial;
        membraneBitsMaterial.EmissionSphereRadius = ParentMicrobe.Radius / 2f;
        membraneBitsMaterial.LinearAccel = ParentMicrobe.Radius / 2;

        TimeToLiveRemaining = membraneBitsParticles.Lifetime;

        AddToGroup(Constants.TIMED_GROUP);
    }
}
