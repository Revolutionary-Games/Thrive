using System;
using System.IO;
using System.Text;
using Godot;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;
using Directory = Godot.Directory;
using File = Godot.File;

/// <summary>
///   A class representing a single saved game
/// </summary>
public class Save
{
    public const string SAVE_SAVE_JSON = "save.json";
    public const string SAVE_INFO_JSON = "info.json";
    public const string SAVE_SCREENSHOT = "screenshot.png";

    /// <summary>
    ///   Name of this save on disk
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///   General information about this save
    /// </summary>
    public SaveInformation Info { get; set; } = new SaveInformation();

    /// <summary>
    ///   The state the game was in when it was saved
    /// </summary>
    public MainGameState GameState { get; set; } = MainGameState.Invalid;

    /// <summary>
    ///   The game properties of the saved game
    /// </summary>
    public GameProperties SavedProperties { get; set; }

    /// <summary>
    ///   Microbe stage data for the save, if currently in the microbe stage
    /// </summary>
    public MicrobeStage MicrobeStage { get; set; }

    /// <summary>
    ///   Microbe editor data for the save, if GameStateName == MicrobeEditor
    /// </summary>
    public MicrobeEditor MicrobeEditor { get; set; }

    /// <summary>
    ///   Screenshot for this save
    /// </summary>
    [JsonIgnore]
    public Image Screenshot { get; set; }

    /// <summary>
    ///   Loads a save from a file or throws an exception
    /// </summary>
    /// <param name="saveName">The name of the save. This is not the full path.</param>
    /// <param name="readFinished">
    ///   A callback that is called when reading data has finished and creating objects start.
    /// </param>
    /// <returns>The loaded save</returns>
    public static Save LoadFromFile(string saveName, Action readFinished = null)
    {
        var target = SaveFileInfo.SaveNameToPath(saveName);

        var (_, save, screenshot) = LoadFromFile(target, false, true, true, readFinished);

        // Info is already contained in save so it doesn't need to be loaded and assigned here
        save.Screenshot = screenshot;

        return save;
    }

    public static SaveInformation LoadJustInfoFromSave(string saveName)
    {
        var target = SaveFileInfo.SaveNameToPath(saveName);

        var (info, _, _) = LoadFromFile(target, true, false, false, null);

        return info;
    }

    public static Save LoadInfoAndScreenshotFromSave(string saveName)
    {
        var target = SaveFileInfo.SaveNameToPath(saveName);

        var (info, _, screenshot) = LoadFromFile(target, true, false, true, null);

        var save = new Save();
        save.Name = saveName;
        save.Info = info;
        save.Screenshot = screenshot;

        return save;
    }

    /// <summary>
    ///   Writes this save to disk.
    /// </summary>
    /// <remarks>
    ///   In order to save the screenshot as png this needs to save it to a temporary file on disk.
    /// </remarks>
    public void SaveToFile()
    {
        FileHelpers.MakeSureDirectoryExists(Constants.SAVE_FOLDER);
        var target = SaveFileInfo.SaveNameToPath(Name);

        var justInfo = ThriveJsonConverter.Instance.SerializeObject(Info);
        var serialized = ThriveJsonConverter.Instance.SerializeObject(this);

        string tempScreenshot = null;

        if (Screenshot != null)
        {
            tempScreenshot = PathUtils.Join(Constants.SAVE_FOLDER, "tmp.png");
            if (Screenshot.SavePng(tempScreenshot) != Error.Ok)
            {
                GD.PrintErr("Failed to save screenshot for inclusion in save");
                Screenshot = null;
                tempScreenshot = null;
            }
        }

        try
        {
            WriteDataToSaveFile(target, justInfo, serialized, tempScreenshot);
        }
        finally
        {
            // Remove the temp file
            if (tempScreenshot != null)
                FileHelpers.DeleteFile(tempScreenshot);
        }
    }

    private static void WriteDataToSaveFile(string target, string justInfo, string serialized, string tempScreenshot)
    {
        using (var file = new File())
        {
            file.Open(target, File.ModeFlags.Write);
            using (Stream gzoStream = new GZipOutputStream(new GodotFileStream(file)))
            {
                using (var tar = new TarOutputStream(gzoStream))
                {
                    OutputEntry(tar, SAVE_INFO_JSON, Encoding.UTF8.GetBytes(justInfo));

                    if (tempScreenshot != null)
                    {
                        byte[] data = null;

                        using (var reader = new File())
                        {
                            reader.Open(tempScreenshot, File.ModeFlags.Read);

                            if (!reader.IsOpen())
                            {
                                GD.PrintErr("Failed to open temp screenshot for writing to save");
                            }
                            else
                            {
                                data = reader.GetBuffer((int)reader.GetLen());
                            }
                        }

                        if (data != null && data.Length > 0)
                            OutputEntry(tar, SAVE_SCREENSHOT, data);
                    }

                    OutputEntry(tar, SAVE_SAVE_JSON, Encoding.UTF8.GetBytes(serialized));
                }
            }
        }
    }

    private static (SaveInformation info, Save save, Image screenshot) LoadFromFile(string file, bool info,
        bool save, bool screenshot, Action readFinished)
    {
        using (var directory = new Directory())
        {
            if (!directory.FileExists(file))
                throw new ArgumentException("save with the given name doesn't exist");
        }

        var (infoStr, saveStr, screenshotData) = LoadDataFromFile(file, info, save, screenshot);

        readFinished?.Invoke();

        SaveInformation infoResult = null;
        Save saveResult = null;
        Image imageResult = null;

        if (info)
        {
            if (string.IsNullOrEmpty(infoStr))
            {
                throw new IOException("couldn't find info content in save");
            }

            infoResult = ThriveJsonConverter.Instance.DeserializeObject<SaveInformation>(infoStr);
        }

        if (save)
        {
            if (string.IsNullOrEmpty(saveStr))
            {
                throw new IOException("couldn't find save content in save file");
            }

            // This deserializes a huge tree of objects
            saveResult = ThriveJsonConverter.Instance.DeserializeObject<Save>(saveStr);
        }

        if (screenshot)
        {
            imageResult = new Image();

            if (screenshotData != null && screenshotData.Length > 0)
            {
                imageResult.LoadPngFromBuffer(screenshotData);
            }
            else
            {
                // Not a critical error
            }
        }

        return (infoResult, saveResult, imageResult);
    }

    private static (string infoStr, string saveStr, byte[] screenshot) LoadDataFromFile(string file, bool info,
        bool save, bool screenshot)
    {
        string infoStr = null;
        string saveStr = null;
        byte[] screenshotData = null;

        // Used for early stop in reading
        int itemsToRead = 0;

        if (info)
            ++itemsToRead;

        if (save)
            ++itemsToRead;

        if (screenshot)
            ++itemsToRead;

        if (itemsToRead < 1)
        {
            throw new ArgumentException("no things to load specified from save");
        }

        using (var reader = new File())
        {
            reader.Open(file, File.ModeFlags.Read);
            if (!reader.IsOpen())
                throw new ArgumentException("couldn't open the file for reading");

            using (Stream gzoStream = new GZipInputStream(new GodotFileStream(reader)))
            {
                using (var tar = new TarInputStream(gzoStream))
                {
                    TarEntry tarEntry;
                    while ((tarEntry = tar.GetNextEntry()) != null)
                    {
                        if (tarEntry.IsDirectory)
                            continue;

                        if (tarEntry.Name == SAVE_INFO_JSON)
                        {
                            if (!info)
                                continue;

                            infoStr = ReadStringEntry(tar, (int)tarEntry.Size);
                            --itemsToRead;
                        }
                        else if (tarEntry.Name == SAVE_SAVE_JSON)
                        {
                            if (!save)
                                continue;

                            saveStr = ReadStringEntry(tar, (int)tarEntry.Size);
                            --itemsToRead;
                        }
                        else if (tarEntry.Name == SAVE_SCREENSHOT)
                        {
                            if (!screenshot)
                                continue;

                            screenshotData = ReadBytesEntry(tar, (int)tarEntry.Size);
                            --itemsToRead;
                        }
                        else
                        {
                            GD.PrintErr("Unknown file in save: ", tarEntry.Name);
                        }

                        // Early quit if we already got as many things as we want
                        if (itemsToRead <= 0)
                            break;
                    }
                }
            }
        }

        return (infoStr, saveStr, screenshotData);
    }

    private static void OutputEntry(TarOutputStream archive, string name, byte[] data)
    {
        var entry = TarEntry.CreateTarEntry(name);

        entry.TarHeader.Mode = Convert.ToInt32("0664", 8);

        // TODO: could fill in more of the properties

        entry.Size = data.Length;

        archive.PutNextEntry(entry);

        archive.Write(data, 0, data.Length);

        archive.CloseEntry();
    }

    private static string ReadStringEntry(TarInputStream tar, int length)
    {
        // Pre-allocate storage
        var buffer = new byte[length];
        using (var stream = new MemoryStream(buffer))
        {
            tar.CopyEntryContents(stream);
        }

        return Encoding.UTF8.GetString(buffer);
    }

    private static byte[] ReadBytesEntry(TarInputStream tar, int length)
    {
        // Pre-allocate storage
        var buffer = new byte[length];
        using (var stream = new MemoryStream(buffer))
        {
            tar.CopyEntryContents(stream);
        }

        return buffer;
    }
}

/// <summary>
///   Info embedded in a save file
/// </summary>
public class SaveInformation
{
    public enum SaveType
    {
        /// <summary>
        ///   Player initiated save
        /// </summary>
        Manual,

        /// <summary>
        ///   Automatic save
        /// </summary>
        AutoSave,

        /// <summary>
        ///   Quick save, separate from manual to make it easier to keep a fixed number of quick saves
        /// </summary>
        QuickSave,
    }

    /// <summary>
    ///   Version of the game the save was made with, used to detect incompatible versions
    /// </summary>
    public string ThriveVersion { get; set; } = Constants.Version;

    public string Platform { get; set; } = FeatureInformation.GetOS();

    public string Creator { get; set; } = System.Environment.UserName;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    ///   An extended description for this save
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///   Unique ID of this save
    /// </summary>
    public Guid ID { get; set; } = Guid.NewGuid();

    public SaveType Type { get; set; } = SaveType.Manual;
}
