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
    /// <summary>
    ///   Godot's main SceneTree
    /// </summary>
    public SceneTree SceneTree { get; }

    /// <summary>
    ///   Returns the currently active scene in the game (for example the MainMenu or the MicrobeStage)
    /// </summary>
    public Node CurrentScene { get; }
}
