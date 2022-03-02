using System;
using System.IO;
using Godot;
using Directory = Godot.Directory;
using Path = System.IO.Path;

/// <summary>
///   Helpers regarding file operations
/// </summary>
public static class FileHelpers
{
    /// <summary>
    ///   Makes sure the save directory exists
    /// </summary>
    public static void MakeSureDirectoryExists(string path)
    {
        using var directory = new Directory();
        var result = directory.MakeDirRecursive(path);

        if (result != Error.AlreadyExists && result != Error.Ok)
        {
            throw new IOException($"can't create folder: {path}");
        }
    }

    /// <summary>
    ///   Attempts to delete a file
    /// </summary>
    /// <param name="path">Path to the file</param>
    /// <returns>True on success</returns>
    public static bool DeleteFile(string path)
    {
        using var directory = new Directory();
        var result = directory.Remove(path);
        return result == Error.Ok;
    }

    /// <summary>
    ///   Returns true if file exists
    /// </summary>
    /// <param name="path">Path to check</param>
    /// <returns>True if exists, false otherwise</returns>
    public static bool Exists(string path)
    {
        using var directory = new Directory();
        return directory.FileExists(path);
    }

    /// <summary>
    ///   Returns true if file exists and the file name matches case
    /// </summary>
    /// <param name="path">Path to check</param>
    /// <returns>True if exists, false otherwise</returns>
    public static bool ExistsCaseSensitive(string path)
    {
        // Checks if it exist first before checking case
        if (!Exists(path))
        {
            return false;
        }

        var globalizedPath = ProjectSettings.GlobalizePath(path);
        string directoryPath = Path.GetDirectoryName(globalizedPath ?? string.Empty) ?? string.Empty;
        if (!string.IsNullOrEmpty(directoryPath) && !string.IsNullOrEmpty(globalizedPath))
        {
            return Array.Exists(System.IO.Directory.GetFiles(directoryPath),
                s => s == Path.GetFullPath(globalizedPath ?? string.Empty));
        }

        return false;
    }
}
