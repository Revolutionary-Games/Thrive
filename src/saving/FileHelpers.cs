using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        using (var directory = new Directory())
        {
            var result = directory.MakeDirRecursive(path);

            if (result != Error.AlreadyExists && result != Error.Ok)
            {
                throw new System.IO.IOException($"can't create folder: {path}");
            }
        }
    }

    /// <summary>
    ///   Attempts to delete a file
    /// </summary>
    /// <param name="path">Path to the file</param>
    /// <returns>True on success</returns>
    public static bool DeleteFile(string path)
    {
        using (var directory = new Directory())
        {
            var result = directory.Remove(path);
            return result == Error.Ok;
        }
    }

    /// <param name="filesToCheck">
    ///   The files to check. Full path needed.
    /// </param>
    /// <returns>
    ///   Returns the last modified file in the list.
    /// </returns>
    /// <summary>
    ///   Gets the last modified file.
    /// </summary>
    public static string GetLastModifiedFile(IEnumerable<string> filesToCheck)
    {
        var debug = string.Join(";", filesToCheck);

        return filesToCheck.ToDictionary(p => p, GetModifiedDate)
            .Aggregate((a, b) => a.Value > b.Value ? a : b).Key;
    }

    private static ulong GetModifiedDate(string filename)
    {
        using (var file = new File())
            return file.GetModifiedTime(filename);
    }
}
