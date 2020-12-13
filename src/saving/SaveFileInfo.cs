using Godot;

/// <summary>
///   Info about a save file on disk
/// </summary>
public class SaveFileInfo
{
    private SaveInformation info;

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
        get
        {
            if (info == null)
            {
                // Load from file
                info = Save.LoadJustInfoFromSave(Name);
            }

            return info;
        }
        set => info = value;
    }

    public static string SaveNameToPath(string name)
    {
        return PathUtils.Join(Constants.SAVE_FOLDER, name);
    }

    /// <summary>
    ///   Refreshes the info from the file
    /// </summary>
    public void Refresh()
    {
        info = null;

        using (var file = new File())
        {
            LastModified = file.GetModifiedTime(Path);
        }
    }
}
