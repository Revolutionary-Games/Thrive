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

    public Timer DespawnTimer;
    //Delay has to be defined
    public float Delaydespawn = 250;


    public void OnTimeOver()
    {
        Destroy();
    }

    public override void _Ready()
    {
        AddCollisionExceptionWith(Emitter);
        Connect("body_entered", this, "OnBodyEntered");

        // Timer that delay despawn of projectiles
        DespawnTimer = new Timer();
        DespawnTimer.OneShot = true;
        DespawnTimer.WaitTime = Delaydespawn;
        DespawnTimer.Connect("timeout", this, "OnTimerTimeout");
        AddChild(DespawnTimer);
    }

    public void OnBodyEntered(Node body)
    {
        if (body is Microbe microbe)
        {
            if (microbe.Species != Properties.Species)
            {
                // If more stuff needs to be damaged we
                // could make an IAgentDamageable interface.
                microbe.Damage(Constants.OXYTOXY_DAMAGE, Properties.AgentType);
            }
        }


        DespawnTimer.Start();
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

    public void OnTimerTimeout()
    {
        Destroy();
    }

    private void Destroy()
    {
        // We should probably get some *POP* effect here.
        QueueFree();
    }
}
