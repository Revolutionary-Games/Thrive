using System;
using System.IO;
using System.Text;
using Godot;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    ///   The scene object to switch to once this save is loaded
    /// </summary>
    [JsonIgnore]
    public ILoadableGameState TargetScene
    {
        get
        {
            switch (GameState)
            {
                case MainGameState.MicrobeStage:
                    return MicrobeStage;
                case MainGameState.MicrobeEditor:
                    return MicrobeEditor;
                default:
                    throw new InvalidOperationException("specified game state has no associated scene");
            }
        }
    }

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

        try
        {
            var (info, _, _) = LoadFromFile(target, true, false, false, null);

            return info;
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to load save info from ${saveName}, error: ${e}");
            return SaveInformation.CreateInvalid();
        }
    }

    public static Save LoadInfoAndScreenshotFromSave(string saveName)
    {
        var target = SaveFileInfo.SaveNameToPath(saveName);

        var save = new Save { Name = saveName };

        try
        {
            var (info, _, screenshot) = LoadFromFile(target, true, false, true, null);

            save.Info = info;
            save.Screenshot = screenshot;
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to load save info and screenshot from ${saveName}, error: ${e}");
            save.Info = SaveInformation.CreateInvalid();
            save.Screenshot = null;
        }

        return save;
    }

    public static (SaveInformation, JObject, Image) LoadJSONStructureFromFile(string saveName)
    {
        var target = SaveFileInfo.SaveNameToPath(saveName);
        var (infoStr, saveStr, screenshotData) = LoadDataFromFile(target, true, true, true);

        if (string.IsNullOrEmpty(infoStr))
            throw new IOException("couldn't find info content in save");

        if (string.IsNullOrEmpty(saveStr))
            throw new IOException("couldn't find save content in save file");

        var infoResult = ThriveJsonConverter.Instance.DeserializeObject<SaveInformation>(infoStr);

        // Don't use the normal deserialization as we don't want to actually create the game state, instead we want
        // a JSON structure
        var saveResult = JObject.Parse(saveStr);

        var imageResult = new Image();

        if (screenshotData?.Length > 0)
        {
            imageResult.LoadPngFromBuffer(screenshotData);
        }

        return (infoResult, saveResult, imageResult);
    }

    public static void WriteSaveJSONToFile(SaveInformation saveInfo, JObject saveStructure, Image screenshot,
        string saveName)
    {
        var serialized = saveStructure.ToString(Formatting.None);

        WriteRawSaveDataToFile(saveInfo, serialized, screenshot, saveName);
    }

    /// <summary>
    ///   Writes this save to disk.
    /// </summary>
    /// <remarks>
    ///   In order to save the screenshot as png this needs to save it to a temporary file on disk.
    /// </remarks>
    public void SaveToFile()
    {
        WriteRawSaveDataToFile(Info, ThriveJsonConverter.Instance.SerializeObject(this), Screenshot, Name);
    }

    /// <summary>
    ///   Destroys the save game states. Use if not attaching the loaded save to the scene tree
    /// </summary>
    public void DestroyGameStates()
    {
        GameState = MainGameState.Invalid;

        MicrobeStage?.QueueFree();
        MicrobeStage = null;

        MicrobeEditor?.QueueFree();
        MicrobeEditor = null;
    }

    private static void WriteRawSaveDataToFile(SaveInformation saveInfo, string saveContent, Image screenshot,
        string saveName)
    {
        FileHelpers.MakeSureDirectoryExists(Constants.SAVE_FOLDER);
        var target = SaveFileInfo.SaveNameToPath(saveName);

        var justInfo = ThriveJsonConverter.Instance.SerializeObject(saveInfo);

        string tempScreenshot = null;

        if (screenshot != null)
        {
            // TODO: if in the future Godot allows converting images to in-memory PNGs that should be used here
            tempScreenshot = PathUtils.Join(Constants.SAVE_FOLDER, "tmp.png");
            if (screenshot.SavePng(tempScreenshot) != Error.Ok)
            {
                GD.PrintErr("Failed to save screenshot for inclusion in save");
                tempScreenshot = null;
            }
        }

        try
        {
            WriteDataToSaveFile(target, justInfo, saveContent, tempScreenshot);
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
        using var file = new File();
        file.Open(target, File.ModeFlags.Write);

        using Stream gzoStream = new GZipOutputStream(new GodotFileStream(file));
        using var tar = new TarOutputStream(gzoStream, Encoding.UTF8);

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

            if (data?.Length > 0)
                OutputEntry(tar, SAVE_SCREENSHOT, data);
        }

        OutputEntry(tar, SAVE_SAVE_JSON, Encoding.UTF8.GetBytes(serialized));
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

            if (screenshotData?.Length > 0)
            {
                imageResult.LoadPngFromBuffer(screenshotData);
            }

            // Not a critical error that screenshot is missing even if it was requested
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

        using var reader = new File();
        reader.Open(file, File.ModeFlags.Read);

        if (!reader.IsOpen())
            throw new ArgumentException("couldn't open the file for reading");

        using var stream = new GodotFileStream(reader);
        using Stream gzoStream = new GZipInputStream(stream);
        using var tar = new TarInputStream(gzoStream, Encoding.UTF8);

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
        {
            using var stream = new MemoryStream(buffer);
            tar.CopyEntryContents(stream);
        }

        return Encoding.UTF8.GetString(buffer);
    }

    private static byte[] ReadBytesEntry(TarInputStream tar, int length)
    {
        // Pre-allocate storage
        var buffer = new byte[length];
        {
            using var stream = new MemoryStream(buffer);
            tar.CopyEntryContents(stream);
        }

        return buffer;
    }
}
