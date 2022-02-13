using System.IO;
using Godot;
using Directory = Godot.Directory;
using File = Godot.File;

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
    ///   Try to create a file with write access
    /// </summary>
    /// <param name="path">Path to file</param>
    /// <returns>File status</returns>
    public static Error TryCreateWrite(string path)
    {
        using var file = new File();
        var e = file.Open(path, File.ModeFlags.Write);
        if (e != Error.Ok)
            return e;

        file.Close();
        DeleteFile(path);

        return Error.Ok;
    }
}
