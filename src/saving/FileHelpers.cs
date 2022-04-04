﻿using System.IO;
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
    ///   Tests if it is possible to create/write a file at the given path
    /// </summary>
    /// <param name="path">Path to file</param>
    /// <returns>File write access status, <see cref="Error.Ok"/> if successful</returns>
    /// <remarks>
    ///   <para>
    ///     THIS FUNCTION IS DESTRUCTIVE! MAKE SURE THE FILE TO TEST DOESN'T EXIST!
    ///   </para>
    /// </remarks>
    public static Error TryWriteFile(string path)
    {
        using var file = new File();
        var error = file.Open(path, File.ModeFlags.Write);
        if (error != Error.Ok)
            return error;

        file.Close();
        DeleteFile(path);

        return Error.Ok;
    }
}
