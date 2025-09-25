using Arch.Buffer;
using Arch.Core;
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

    public event IModInterface.OnDamageReceivedHandler? OnDamageReceived;
    public event IModInterface.OnPlayerMicrobeSpawnedHandler? OnPlayerMicrobeSpawned;
    public event IModInterface.OnMicrobeSpawnedHandler? OnMicrobeSpawned;
    public event IModInterface.OnChunkSpawnedHandler? OnChunkSpawned;
    public event IModInterface.OnToxinEmittedHandler? OnToxinEmitted;
    public event IModInterface.OnMicrobeDiedHandler? OnMicrobeDied;

    public SceneTree SceneTree { get; }

    public Node CurrentScene => SceneTree.CurrentScene;

    public void TriggerOnSceneChanged(Node newScene)
    {
        OnSceneChanged?.Invoke(newScene);
    }

    public void TriggerOnDamageReceived(Entity damageReceiver, float amount, bool isPlayer)
    {
        OnDamageReceived?.Invoke(damageReceiver, amount, isPlayer);
    }

    public void TriggerOnPlayerMicrobeSpawned(Entity player)
    {
        OnPlayerMicrobeSpawned?.Invoke(player);
    }

    public void TriggerOnMicrobeSpawned(Entity microbe, CommandBuffer commandBuffer)
    {
        OnMicrobeSpawned?.Invoke(microbe, commandBuffer);
    }

    public void TriggerOnChunkSpawned(Entity chunk, bool environmental, CommandBuffer commandBuffer)
    {
        OnChunkSpawned?.Invoke(chunk, environmental, commandBuffer);
    }

    public void TriggerOnToxinEmitted(Entity toxin, CommandBuffer commandBuffer)
    {
        OnToxinEmitted?.Invoke(toxin, commandBuffer);
    }

    public void TriggerOnMicrobeDied(Entity microbe, bool isPlayer)
    {
        OnMicrobeDied?.Invoke(microbe, isPlayer);
    }
}
