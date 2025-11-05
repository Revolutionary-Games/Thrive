using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Saving.Serializers;
using SharedBase.Archive;
using FileAccess = Godot.FileAccess;

/// <summary>
///   A class representing a single saved game
/// </summary>
public sealed class Save : IArchivable, IDisposable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public const string SAVE_SAVE_ARCHIVE = "save.bin";
    public const string SAVE_INFO_JSON = "info.json";
    public const string SAVE_SCREENSHOT = "screenshot.png";

    // Temporary data for saving the archive data
    private MemoryStream? saveArchiveData;

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
    ///   Multicellular editor data for the save, if GameStateName == MulticellularEditor
    /// </summary>
    public MulticellularEditor? MulticellularEditor { get; set; }

    /// <summary>
    ///   Screenshot for this save
    /// </summary>
    public Image? Screenshot { get; set; }

    /// <summary>
    ///   The scene-object to switch to once this save is loaded
    /// </summary>
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
                case MainGameState.MulticellularEditor:
                    return MulticellularEditor;
                default:
                    throw new InvalidOperationException("specified game state has no associated scene");
            }
        }
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.Save;
    public bool CanBeReferencedInArchive => false;

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

        // Info is already contained in save, so it doesn't need to be loaded and assigned here
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
        var (infoStr, saveArchive, screenshotData) = LoadDataFromFile(target, true, true, true);

        if (string.IsNullOrEmpty(infoStr))
            throw new IOException("couldn't find info content in save");

        if (saveArchive == null)
            throw new IOException("couldn't find save archive content in save file");

        var infoResult = ThriveJsonConverter.Instance.DeserializeObject<SaveInformation>(infoStr) ??
            throw new JsonException("SaveInformation object was deserialized as null");

        _ = infoResult;

        var imageResult = new Image();

        if (screenshotData?.Length > 0)
        {
            imageResult.LoadPngFromBuffer(screenshotData);
        }

        // TODO: reimplementing save upgrading
        throw new NotSupportedException("Loading JSON structure from save is not supported anymore");

        // return (infoResult, saveResult, imageResult);
    }

    public static void WriteSaveJSONToFile(SaveInformation saveInfo, JObject saveStructure, Image? screenshot,
        string saveName)
    {
        throw new NotImplementedException("Save upgrading is not reimplemented");
    }

    /// <summary>
    ///   This is the archive interface implementation; not really meant to be called directly.
    ///   Use <see cref="SerializeData"/> and <see cref="SaveToFile"/> instead.
    /// </summary>
    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(Name);
        writer.WriteObject(Info);
        writer.Write((int)GameState);

        writer.WriteObjectOrNull(SavedProperties);
        writer.WriteObjectOrNull(MicrobeStage);
        writer.WriteObjectOrNull(MicrobeEditor);
        writer.WriteObjectOrNull(MulticellularEditor);

        // Other properties are not saved
    }

    /// <summary>
    ///   Serializes the main data into an archive, must be called before saving this to a file.
    /// </summary>
    /// <param name="archiveManager">Archive manager to perform the serialization with</param>
    public void SerializeData(ThriveArchiveManager archiveManager)
    {
        if (saveArchiveData == null)
        {
            saveArchiveData = new MemoryStream();
        }
        else
        {
            saveArchiveData.Position = 0;
            saveArchiveData.SetLength(0);
        }

        using var writer = new SArchiveMemoryWriter(saveArchiveData, archiveManager, false);

        archiveManager.OnStartNewWrite(writer);

        writer.WriteArchiveHeader(ISArchiveWriter.ArchiveHeaderVersion, "thrive", Constants.VersionFull);

        writer.WriteObject(this);

        writer.WriteArchiveFooter();
        archiveManager.OnFinishWrite(writer);

        saveArchiveData.Position = 0;
    }

    public string GetArchiveHash()
    {
        if (saveArchiveData == null)
            throw new InvalidOperationException("Archive data is not set, call SerializeData first");

        saveArchiveData.Position = 0;
        var result = SHA1.HashData(saveArchiveData);
        saveArchiveData.Position = 0;

        return Convert.ToHexStringLower(result);
    }

    /// <summary>
    ///   Writes this save to disk.
    /// </summary>
    public void SaveToFile()
    {
        if (saveArchiveData == null)
            throw new InvalidOperationException("Archive data is not set, call SerializeData first");

        saveArchiveData.Position = 0;

        WriteRawSaveDataToFile(Info, saveArchiveData, Screenshot, Name);
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

        MulticellularEditor?.QueueFree();
        MulticellularEditor = null;
    }

    public void Dispose()
    {
        saveArchiveData?.Dispose();
    }

    internal static Save ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new Save();

        instance.Name = reader.ReadString() ?? throw new NullArchiveObjectException();
        instance.Info = reader.ReadObject<SaveInformation>();
        instance.GameState = (MainGameState)reader.ReadInt32();

        instance.SavedProperties = reader.ReadObjectOrNull<GameProperties>();

        instance.MicrobeStage = reader.ReadObjectOrNull<MicrobeStage>();
        instance.MicrobeEditor = reader.ReadObjectOrNull<MicrobeEditor>();
        instance.MulticellularEditor = reader.ReadObjectOrNull<MulticellularEditor>();

        return instance;
    }

    private static void WriteRawSaveDataToFile(SaveInformation saveInfo, Stream saveContent, Image? screenshot,
        string saveName)
    {
        FileHelpers.MakeSureDirectoryExists(Constants.SAVE_FOLDER);
        var target = SaveFileInfo.SaveNameToPath(saveName);

        var justInfo = ThriveJsonConverter.Instance.SerializeObject(saveInfo);

        WriteDataToSaveFile(target, justInfo, saveContent, screenshot);
    }

    private static void WriteDataToSaveFile(string target, string justInfo, Stream serialized, Image? screenshot)
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
        using var entryContent = new MemoryStream(justInfo.Length);
        using var entryWriter = new StreamWriter(entryContent, Encoding.UTF8);

        TarHelper.OutputEntry(tar, SAVE_INFO_JSON, justInfo, entryContent, entryWriter);

        if (screenshot != null)
        {
            byte[] data = screenshot.SavePngToBuffer();

            if (data.Length > 0)
                TarHelper.OutputEntry(tar, SAVE_SCREENSHOT, data);
        }

        TarHelper.OutputEntry(tar, SAVE_SAVE_ARCHIVE, serialized);
    }

    private static (SaveInformation? Info, Save? Save, Image? Screenshot) LoadFromFile(string file, bool info,
        bool save, bool screenshot, Action? readFinished)
    {
        if (!FileAccess.FileExists(file))
            throw new ArgumentException("save with the given name doesn't exist");

        var (infoStr, saveArchive, screenshotData) = LoadDataFromFile(file, info, save, screenshot);

        readFinished?.Invoke();

        SaveInformation? infoResult = null;
        Save? saveResult = null;
        Image? imageResult = null;

        if (info)
        {
            infoResult = ParseSaveInfo(infoStr);
        }

        if (screenshot)
        {
            if (screenshotData != null)
                imageResult = TarHelper.ImageFromBuffer(screenshotData);

            // Not a critical error that screenshot is missing even if it was requested
        }

        if (save)
        {
            if (saveArchive == null)
            {
                throw new IOException("couldn't find save archive content in the save file (is it a really old file?)");
            }

            // Loading is not as time-sensitive as writing, so we just crudely make a new manager here
            var manager = new ThriveArchiveManager();
            using var reader = new SArchiveMemoryReader(saveArchive, manager, true);

            manager.OnStartNewRead(reader);

            reader.ReadArchiveHeader(out var version, out var program, out var programVersion);

            if (version != ISArchiveWriter.ArchiveHeaderVersion)
                throw new IOException($"Save file format is incompatible: {version}");

            if (program != "thrive")
                throw new IOException("Save archive is from a different program");

            // The higher level code does version checks, so just print the full version
            GD.Print("Save file program version: ", programVersion);

            // This deserializes a huge tree of objects!
            saveResult = reader.ReadObjectOrNull<Save>() ?? throw new NullArchiveObjectException("Save data is null");

            reader.ReadArchiveFooter();

            manager.OnFinishRead(reader);
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

    private static (string? InfoStr, MemoryStream? SaveArchive, byte[]? Screenshot) LoadDataFromFile(string file,
        bool info, bool save, bool screenshot)
    {
        string? infoStr = null;
        MemoryStream? archiveData = null;
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
            else if (tarEntry.Name == SAVE_SAVE_ARCHIVE)
            {
                if (!save)
                    continue;

                // TODO: theoretically the new archive format would allow us to create a stream for direct reading here
                var raw = TarHelper.ReadBytesEntry(tarEntry);

                // We need to expose the buffer for efficient copies, so we need to specify all the parameters like
                // this. In case we implement operations that allow updating a save and then writing it, we set the
                // writable flag here.
                archiveData = new MemoryStream(raw, 0, raw.Length, true, true);
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

        return (infoStr, archiveData, screenshotData);
    }
}
