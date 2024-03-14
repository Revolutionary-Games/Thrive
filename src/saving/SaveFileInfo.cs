using System;
using Godot;

/// <summary>
///   Info about a save file on disk
/// </summary>
public class SaveFileInfo
{
    private SaveInformation? info;

    public SaveFileInfo(string name)
    {
        Name = name;
        Path = SaveNameToPath(Name);
        Refresh();
    }

    /// <summary>
    ///   Path to the save file
    /// </summary>
    public string Path { get; }

    /// <summary>
    ///   Name of the the save file
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///   Last modified unix timestamp
    /// </summary>
    public ulong LastModified { get; set; }

    public SaveInformation Info
    {
        // Load from file if missing
        get => info ??= Save.LoadJustInfoFromSave(Name);
        set => info = value;
    }

    public static string SaveNameToPath(string name)
    {
        // If the name begins with the file protocol it's already a full path
        if (name.StartsWith(Constants.EXPLICIT_PATH_PREFIX, StringComparison.InvariantCulture))
            return name.Substring(Constants.EXPLICIT_PATH_PREFIX.Length);

        return System.IO.Path.Combine(Constants.SAVE_FOLDER, name);
    }

    /// <summary>
    ///   Refreshes the info from the file
    /// </summary>
    public void Refresh()
    {
        info = null;

        LastModified = FileAccess.GetModifiedTime(Path);
    }
}
