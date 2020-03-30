using Godot;

/// <summary>
///   This is a shot agent projectile, does damage on hitting a cell of different species
/// </summary>
public class AgentProjectile : RigidBody, ITimedLife
{
    public float TimeToLiveRemaining { get; set; }
    public float Amount { get; set; }
    public AgentProperties Properties { get; set; }
    public Node Emitter { get; set; }

    public override void _Ready()
    {
        Connect("body_entered", this, "OnBodyEntered");
    }

    public void OnBodyEntered(Node body)
    {
        if (body == Emitter) return; // Kinda hacky.
        if (body is Microbe)
        {
            var microbe = (Microbe)body;
            if(microbe.Species != Properties.Species)
            {
                // If more stuff needs to be damaged we could make an IAgentDamageable interface.
                microbe.Damage(Constants.OXYTOXY_DAMAGE, Properties.AgentType);
            }
        }

        // GD.Print("Collision with " + body.Name);
        Destroy();
    }

    public override void _Process(float delta)
    {
        TimeToLiveRemaining -= delta;
        if(TimeToLiveRemaining <= 0.0f)
        {
            Destroy();
        }
    }

    private void Destroy()
    {
        // We should probably get some *POP* effect here.
        QueueFree();
    }
}
