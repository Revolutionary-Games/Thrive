using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Text;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FileAccess = Godot.FileAccess;

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
    public string Name { get; set; } = "invalid";

    /// <summary>
    ///   General information about this save
    /// </summary>
    public SaveInformation Info { get; set; } = new();

    /// <summary>
    ///   The state the game was in when it was saved
    /// </summary>
    public MainGameState GameState { get; set; } = MainGameState.Invalid;

    /// <summary>
    ///   The game properties of the saved game
    /// </summary>
    public GameProperties? SavedProperties { get; set; }

    /// <summary>
    ///   Microbe stage data for the save, if currently in the microbe stage
    /// </summary>
    public MicrobeStage? MicrobeStage { get; set; }

    /// <summary>
    ///   Microbe editor data for the save, if GameStateName == MicrobeEditor
    /// </summary>
    public MicrobeEditor? MicrobeEditor { get; set; }

    /// <summary>
    ///   Screenshot for this save
    /// </summary>
    [JsonIgnore]
    public Image? Screenshot { get; set; }

    /// <summary>
    ///   The scene object to switch to once this save is loaded
    /// </summary>
    [JsonIgnore]
    public ILoadableGameState? TargetScene
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
    public static Save LoadFromFile(string saveName, Action? readFinished = null)
    {
        var target = SaveFileInfo.SaveNameToPath(saveName);

        var (_, save, screenshot) = LoadFromFile(target, false, true, true, readFinished);

        // Info is already contained in save so it doesn't need to be loaded and assigned here
        save!.Screenshot = screenshot;

        return save;
    }

    public static SaveInformation LoadJustInfoFromSave(string saveName)
    {
        var target = SaveFileInfo.SaveNameToPath(saveName);

        try
        {
            var (info, _, _) = LoadFromFile(target, true, false, false, null);

            return info!;
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

            save.Info = info!;
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

    public static (SaveInformation Info, byte[]? ScreenshotData) LoadInfoAndRawScreenshotFromSave(string saveName)
    {
        var target = SaveFileInfo.SaveNameToPath(saveName);

        try
        {
            var (info, _, screenshot) = LoadDataFromFile(target, true, false, true);

            return (ParseSaveInfo(info), screenshot);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to load save info and screenshot buffer from ${saveName}, error: ${e}");
            return (SaveInformation.CreateInvalid(), null);
        }
    }

    public static Save ConstructSaveFromInfoAndScreenshotBuffer(string saveName, SaveInformation info,
        byte[]? screenshotData)
    {
        var save = new Save { Name = saveName, Info = info };

        if (screenshotData != null)
        {
            save.Screenshot = TarHelper.ImageFromBuffer(screenshotData);
        }

        return save;
    }

    public static (SaveInformation Info, JObject SaveObject, Image? Screenshot) LoadJSONStructureFromFile(
        string saveName)
    {
        var target = SaveFileInfo.SaveNameToPath(saveName);
        var (infoStr, saveStr, screenshotData) = LoadDataFromFile(target, true, true, true);

        if (string.IsNullOrEmpty(infoStr))
            throw new IOException("couldn't find info content in save");

        if (string.IsNullOrEmpty(saveStr))
            throw new IOException("couldn't find save content in save file");

        var infoResult = ThriveJsonConverter.Instance.DeserializeObject<SaveInformation>(infoStr) ??
            throw new JsonException("SaveInformation object was deserialized as null");

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

    public static void WriteSaveJSONToFile(SaveInformation saveInfo, JObject saveStructure, Image? screenshot,
        string saveName)
    {
        var serialized = saveStructure.ToString(Formatting.None);

        WriteRawSaveDataToFile(saveInfo, serialized, screenshot, saveName);
    }

    /// <summary>
    ///   Writes this save to disk.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     In order to save the screenshot as png this needs to save it to a temporary file on disk.
    ///   </para>
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

    private static void WriteRawSaveDataToFile(SaveInformation saveInfo, string saveContent, Image? screenshot,
        string saveName)
    {
        FileHelpers.MakeSureDirectoryExists(Constants.SAVE_FOLDER);
        var target = SaveFileInfo.SaveNameToPath(saveName);

        var justInfo = ThriveJsonConverter.Instance.SerializeObject(saveInfo);

        WriteDataToSaveFile(target, justInfo, saveContent, screenshot);
    }

    private static void WriteDataToSaveFile(string target, string justInfo, string serialized, Image? screenshot)
    {
        using var file = FileAccess.Open(target, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr("Cannot open file for writing: ", target);
            throw new IOException("Cannot open: " + target);
        }

        using var fileStream = new GodotFileStream(file);
        using Stream gzoStream = new GZipStream(fileStream, CompressionLevel.Optimal);
        using var tar = new TarWriter(gzoStream, TarEntryFormat.Pax);

        // Use the size that is in most cases basically the final size for the stream to avoid storage reallocations
        // as much as possible
        using var entryContent = new MemoryStream(serialized.Length);
        using var entryWriter = new StreamWriter(entryContent, Encoding.UTF8);

        TarHelper.OutputEntry(tar, SAVE_INFO_JSON, justInfo, entryContent, entryWriter);

        if (screenshot != null)
        {
            byte[] data = screenshot.SavePngToBuffer();

            if (data.Length > 0)
                TarHelper.OutputEntry(tar, SAVE_SCREENSHOT, data);
        }

        TarHelper.OutputEntry(tar, SAVE_SAVE_JSON, serialized, entryContent, entryWriter);
    }

    private static (SaveInformation? Info, Save? Save, Image? Screenshot) LoadFromFile(string file, bool info,
        bool save, bool screenshot, Action? readFinished)
    {
        if (!FileAccess.FileExists(file))
            throw new ArgumentException("save with the given name doesn't exist");

        var (infoStr, saveStr, screenshotData) = LoadDataFromFile(file, info, save, screenshot);

        readFinished?.Invoke();

        SaveInformation? infoResult = null;
        Save? saveResult = null;
        Image? imageResult = null;

        if (info)
        {
            infoResult = ParseSaveInfo(infoStr);
        }

        if (save)
        {
            if (string.IsNullOrEmpty(saveStr))
            {
                throw new IOException("couldn't find save content in save file");
            }

            // This deserializes a huge tree of objects
            saveResult = ThriveJsonConverter.Instance.DeserializeObject<Save>(saveStr) ??
                throw new JsonException("Save data is null");
        }

        if (screenshot)
        {
            if (screenshotData != null)
                imageResult = TarHelper.ImageFromBuffer(screenshotData);

            // Not a critical error that screenshot is missing even if it was requested
        }

        return (infoResult, saveResult, imageResult);
    }

    private static SaveInformation ParseSaveInfo(string? infoStr)
    {
        if (string.IsNullOrEmpty(infoStr))
        {
            throw new IOException("couldn't find info content in save");
        }

        return ThriveJsonConverter.Instance.DeserializeObject<SaveInformation>(infoStr) ??
            throw new JsonException("SaveInformation is null");
    }

    private static (string? InfoStr, string? SaveStr, byte[]? Screenshot) LoadDataFromFile(string file, bool info,
        bool save, bool screenshot)
    {
        string? infoStr = null;
        string? saveStr = null;
        byte[]? screenshotData = null;

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

        using var reader = FileAccess.Open(file, FileAccess.ModeFlags.Read);
        if (reader == null)
            throw new ArgumentException("couldn't open the file for reading");

        using var stream = new GodotFileStream(reader);
        using Stream gzoStream = new GZipStream(stream, CompressionMode.Decompress);
        using var tar = new TarReader(gzoStream);

        while (tar.GetNextEntry(false) is { } tarEntry)
        {
            if (tarEntry.EntryType is not TarEntryType.V7RegularFile and not TarEntryType.RegularFile)
                continue;

            if (tarEntry.DataStream == null)
                continue;

            if (tarEntry.Name == SAVE_INFO_JSON)
            {
                if (!info)
                    continue;

                infoStr = TarHelper.ReadStringEntry(tarEntry);
                --itemsToRead;
            }
            else if (tarEntry.Name == SAVE_SAVE_JSON)
            {
                if (!save)
                    continue;

                saveStr = TarHelper.ReadStringEntry(tarEntry);
                --itemsToRead;
            }
            else if (tarEntry.Name == SAVE_SCREENSHOT)
            {
                if (!screenshot)
                    continue;

                screenshotData = TarHelper.ReadBytesEntry(tarEntry);
                --itemsToRead;
            }
            else
            {
                GD.Print("Unknown file in save: ", tarEntry.Name);
            }

            // Early quit if we already got as many things as we want
            if (itemsToRead <= 0)
                break;
        }

        return (infoStr, saveStr, screenshotData);
    }
}
