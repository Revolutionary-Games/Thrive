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
        using (var directory = new Directory())
        {
            var result = directory.MakeDirRecursive(path);

            if (result != Error.AlreadyExists && result != Error.Ok)
            {
                throw new IOException($"can't create folder: {path}");
            }
        }
    }
}
