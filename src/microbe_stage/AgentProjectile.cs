using Godot;

/// <summary>
///   This is a shot agent projectile, does damage on hitting a cell of different species
/// </summary>
[JSONAlwaysDynamicType]
public class AgentProjectile : RigidBody, ITimedLife
{
    public float TimeToLiveRemaining { get; set; }
    public float Amount { get; set; }
    public AgentProperties Properties { get; set; }
    public Node Emitter { get; set; }

    public void OnTimeOver() => Destroy();

    public override void _Ready()
    {
        Connect("body_entered", this, "OnBodyEntered");
    }

    public void OnBodyEntered(Node body)
    {
        if (body == Emitter)
            return; // Kinda hacky.

        if (body is Microbe microbe)
        {
            if (microbe.Species != Properties.Species)
            {
                // If more stuff needs to be damaged we
                // could make an IAgentDamageable interface.
                microbe.Damage(Constants.OXYTOXY_DAMAGE, Properties.AgentType);
            }
        }

        Destroy();
    }

    public void ApplyPropertiesFromSave(AgentProjectile projectile)
    {
        NodeGroupSaveHelper.CopyGroups(this, projectile);

        TimeToLiveRemaining = projectile.TimeToLiveRemaining;
        Amount = projectile.Amount;
        Properties = projectile.Properties;
        Transform = projectile.Transform;
        LinearVelocity = projectile.LinearVelocity;
        AngularVelocity = projectile.AngularVelocity;
    }

    private void Destroy()
    {
        // We should probably get some *POP* effect here.
        QueueFree();
    }
}
