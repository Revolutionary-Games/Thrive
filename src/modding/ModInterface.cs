using Godot;

/// <summary>
///   Implementation of the default <see cref="IModInterface"/>
/// </summary>
public class ModInterface : IModInterface
{
    public ModInterface(SceneTree sceneTree)
    {
        SceneTree = sceneTree;
    }

    public event IModInterface.OnDamageReceivedHandler OnDamageReceived;

    public SceneTree SceneTree { get; }

    public Node CurrentScene => SceneTree.CurrentScene;

    public void TriggerOnDamageReceived(Node damageReceiver, float amount, bool isPlayer)
    {
        OnDamageReceived?.Invoke(damageReceiver, amount, isPlayer);
    }
}
