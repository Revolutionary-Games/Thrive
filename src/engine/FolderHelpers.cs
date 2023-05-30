using Godot;

public static class FolderHelpers
{
    /// <summary>
    ///   Open a folder in the platform native file browser
    /// </summary>
    /// <param name="path">The path to the folder</param>
    /// <returns>True on success</returns>
    public static bool OpenFolder(string path)
    {
        var result = OS.ShellOpen("file://" + ProjectSettings.GlobalizePath(path));

        return result != Error.FileNotFound;
    }

    public static bool OpenFile(string path)
    {
        // Opening files currently works the exact same way as folders
        return OpenFolder(path);
    }
}
