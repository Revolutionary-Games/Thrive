/// <summary>
///   Base interface for all mods to implement
/// </summary>
/// <remarks>
///   <para>
///     This interface is specially released into the public domain (or if not valid in your jurisdiction,
///     under the MIT license)
///   </para>
/// </remarks>
public interface IMod
{
    /// <summary>
    ///   Called when the mod should be loaded
    /// </summary>
    /// <param name="modInterface">The mod interface the mod can access to interact with the game</param>
    /// <param name="currentModInfo">
    ///   Info for the current mod. This is provided so that the mod doesn't need to read its own "mod.json" file
    /// </param>
    /// <returns>
    ///   True on success. If returns false a popup is shown telling the player that an error occurred.
    ///   On failure extra info should be printed to logs (GD.PrintErr) for the user to see what the problem is.
    /// </returns>
    bool Initialize(ModInterface modInterface, ModInfo currentModInfo);

    /// <summary>
    ///   Called when the mod should be unloaded. Note that code assemblies can't really be unloaded well so the mod
    ///   should unregister its callbacks, undo the changes it made to the game, and dispose any of its own objects
    ///   to clean up as much as possible.
    /// </summary>
    /// <returns>True if successful, false if an error should be reported to the user</returns>
    bool Unload();
}
