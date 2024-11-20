using System;
using System.IO;
using Godot;
using DirAccess = Godot.DirAccess;
using FileAccess = Godot.FileAccess;

/// <summary>
///   Helpers regarding file operations
/// </summary>
public static class FileHelpers
{
    /// <summary>
    ///   Makes sure the save directory exists. Note that path shouldn't be relative.
    /// </summary>
    public static void MakeSureDirectoryExists(string path)
    {
        var result = DirAccess.MakeDirRecursiveAbsolute(path);

        if (result != Error.AlreadyExists && result != Error.Ok)
        {
            throw new IOException($"can't create folder: {path}");
        }
    }

    /// <summary>
    ///   Makes sure that a parent directory exists for a path. Differs from <see cref="MakeSureDirectoryExists"/> is
    ///   that this expects a path to a file and not directly to the folder to create. The path should be absolute.
    /// </summary>
    public static void MakeSureParentDirectoryExists(string filePath)
    {
        bool seen = false;
        int lastSeparator = -1;
        for (int i = 0; i < filePath.Length; ++i)
        {
            if (filePath[i] == '/' || filePath[i] == '\\')
            {
                seen = true;
                lastSeparator = i;
            }
        }

        if (!seen)
            throw new ArgumentException("File path doesn't have a parent folder in it");

        MakeSureDirectoryExists(filePath.Substring(0, filePath.Length - lastSeparator));
    }

    /// <summary>
    ///   Attempts to delete a file
    /// </summary>
    /// <param name="path">Path to the file</param>
    /// <returns>True on success</returns>
    public static bool DeleteFile(string path)
    {
        var result = DirAccess.RemoveAbsolute(path);
        return result == Error.Ok;
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
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
            return FileAccess.GetOpenError();

        file.Close();
        DeleteFile(path);

        return Error.Ok;
    }
}
