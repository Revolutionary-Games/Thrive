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

    public event IModInterface.OnSceneChangedHandler? OnSceneChanged;

    // TODO: redo these
    // public event IModInterface.OnDamageReceivedHandler? OnDamageReceived;
    // public event IModInterface.OnPlayerMicrobeSpawnedHandler? OnPlayerMicrobeSpawned;
    // public event IModInterface.OnMicrobeSpawnedHandler? OnMicrobeSpawned;
    // public event IModInterface.OnChunkSpawnedHandler? OnChunkSpawned;
    // public event IModInterface.OnToxinEmittedHandler? OnToxinEmitted;
    // public event IModInterface.OnMicrobeDiedHandler? OnMicrobeDied;

    public SceneTree SceneTree { get; }

    public Node CurrentScene => SceneTree.CurrentScene;

    public void TriggerOnSceneChanged(Node newScene)
    {
        OnSceneChanged?.Invoke(newScene);
    }

    /*public void TriggerOnDamageReceived(Node damageReceiver, float amount, bool isPlayer)
    {
        OnDamageReceived?.Invoke(damageReceiver, amount, isPlayer);
    }

    public void TriggerOnPlayerMicrobeSpawned(Microbe player)
    {
        OnPlayerMicrobeSpawned?.Invoke(player);
    }

    public void TriggerOnMicrobeSpawned(Microbe microbe)
    {
        OnMicrobeSpawned?.Invoke(microbe);
    }

    public void TriggerOnChunkSpawned(FloatingChunk chunk, bool environmental)
    {
        OnChunkSpawned?.Invoke(chunk, environmental);
    }

    public void TriggerOnToxinEmitted(AgentProjectile toxin)
    {
        OnToxinEmitted?.Invoke(toxin);
    }

    public void TriggerOnMicrobeDied(Microbe microbe, bool isPlayer)
    {
        OnMicrobeDied?.Invoke(microbe, isPlayer);
    }*/
}
