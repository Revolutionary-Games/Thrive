using System.IO;
using Godot;
using Directory = Godot.Directory;

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
}
