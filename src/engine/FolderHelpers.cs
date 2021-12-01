using Godot;

public static class FolderHelpers
{
    /// <summary>
    ///   Open a folder in the platform native file browser
    /// </summary>
    /// <param name="path">The path to the folder</param>
    /// <returns>True on success</returns>
    /// <remarks>
    ///   <para>
    ///     TODO: this doesn't seem to currently work on mac: https://github.com/Revolutionary-Games/Thrive/issues/2775
    ///   </para>
    /// </remarks>
    public static bool OpenFolder(string path)
    {
        var result = OS.ShellOpen(ProjectSettings.GlobalizePath(path));

        return result != Error.FileNotFound;
    }
}
