using System;
using Godot;

/// <summary>
///   Helpers for scene loading and instantiation with retry handling.
/// </summary>
public static class SceneLoadHelpers
{
    private const int DefaultAttempts = 3;

    /// <summary>
    ///   Loads and instantiates a scene, retrying with cache bypass if the first attempt fails.
    /// </summary>
    /// <param name="scenePath">Resource path to scene</param>
    /// <param name="context">Context text for error messages</param>
    /// <param name="attempts">How many attempts to make</param>
    /// <typeparam name="T">Expected type from instantiation</typeparam>
    /// <returns>Instanced scene, or null if all attempts fail</returns>
    public static T? LoadAndInstantiate<T>(string scenePath, string context, int attempts = DefaultAttempts)
        where T : class
    {
        if (attempts < 1)
            throw new ArgumentOutOfRangeException(nameof(attempts), "Attempts must be at least 1");

        for (int attempt = 1; attempt <= attempts; ++attempt)
        {
            try
            {
                var cacheMode = attempt == 1 ? ResourceLoader.CacheMode.Reuse : ResourceLoader.CacheMode.Ignore;
                var scene = ResourceLoader.Load<PackedScene>(scenePath, string.Empty, cacheMode);

                if (scene == null)
                {
                    GD.PrintErr(
                        $"Failed to load scene {scenePath} for {context} on attempt {attempt}/{attempts}");
                    continue;
                }

                var instanced = scene.Instantiate<T>();

                if (instanced != null)
                    return instanced;

                GD.PrintErr(
                    $"Failed to instantiate scene {scenePath} for {context} on attempt {attempt}/{attempts}");
            }
            catch (Exception e)
            {
                GD.PrintErr(
                    $"Exception while instantiating scene {scenePath} for {context} on attempt {attempt}/{attempts}: {e}");
            }
        }

        return null;
    }
}
