namespace Scripts;

using System;
using System.IO;
using SharedBase.Utilities;

/// <summary>
///   Finds Godot in PATH, preferring the explicit mono executable name, and caches the result for this script run
/// </summary>
public static class GodotExecutable
{
    private static readonly Lazy<string?> ExecutablePath = new(() =>
        ExecutableFinder.Which("godot-mono") ?? ExecutableFinder.Which("godot"));

    /// <summary>
    ///   The cached executable path, or null if neither supported name was found
    /// </summary>
    public static string? Path => ExecutablePath.Value;

    /// <summary>
    ///   The executable path for launching Godot when it is required
    /// </summary>
    public static string RequiredPath => Path ??
        throw new FileNotFoundException("Could not find 'godot-mono' or 'godot' executable, make sure it is in PATH");
}
