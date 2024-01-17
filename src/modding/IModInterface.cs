using DefaultEcs;
using DefaultEcs.Command;
using Godot;

/// <summary>
///   This interface provides an interface for mods to interact with the game through an API that will try to stay
///   stable between game versions
/// </summary>
/// <remarks>
///   <para>
///     Direct access to other game classes and code is allowed (and not really possible to block) from mods, but
///     the code might change drastically between versions and often break mods. As such this class collects some
///     operations mods are likely want to do and provides a way to do them in a way that won't be broken each
///     new release.
///   </para>
///   <para>
///     This interface is specially released into the public domain (or if not valid in your jurisdiction,
///     under the MIT license)
///   </para>
/// </remarks>
public interface IModInterface
{
    public delegate void OnSceneChangedHandler(Node newScene);

    public delegate void OnDamageReceivedHandler(Entity damageReceiver, float amount, bool isPlayer);

    public delegate void OnPlayerMicrobeSpawnedHandler(Entity player);

    public delegate void OnMicrobeSpawnedHandler(EntityRecord microbe);

    public delegate void OnChunkSpawnedHandler(EntityRecord chunk, bool environmental);

    public delegate void OnToxinEmittedHandler(EntityRecord toxin);

    public delegate void OnMicrobeDiedHandler(Entity microbe, bool isPlayer);

    // Game events mods can listen to
    // If something you'd want to use is missing, please request it:
    // https://github.com/Revolutionary-Games/Thrive/issues or open a pull request adding it

    public event OnSceneChangedHandler OnSceneChanged;

    public event OnDamageReceivedHandler OnDamageReceived;

    public event OnPlayerMicrobeSpawnedHandler OnPlayerMicrobeSpawned;
    public event OnMicrobeSpawnedHandler OnMicrobeSpawned;
    public event OnChunkSpawnedHandler OnChunkSpawned;
    public event OnToxinEmittedHandler OnToxinEmitted;
    public event OnMicrobeDiedHandler OnMicrobeDied;

    /// <summary>
    ///   Godot's main SceneTree
    /// </summary>
    public SceneTree SceneTree { get; }

    /// <summary>
    ///   Returns the currently active scene in the game (for example the MainMenu or the MicrobeStage)
    /// </summary>
    public Node CurrentScene { get; }
}
