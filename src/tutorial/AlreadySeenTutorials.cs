namespace Tutorial;

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Godot;
using Newtonsoft.Json;
using FileAccess = Godot.FileAccess;

/// <summary>
///   Helper class for saving / loading the global state of all tutorials the player has seen
/// </summary>
public static class AlreadySeenTutorials
{
    private static readonly HashSet<string> AlreadySeenTutorialsValue = new();

    private static bool dirty;

    private static double elapsedTime;

    public static IReadOnlyCollection<string> SeenTutorials => AlreadySeenTutorialsValue;

    public static void MarkSeen(string tutorialName)
    {
        if (!AlreadySeenTutorialsValue.Add(tutorialName))
            return;

        dirty = true;
    }

    public static void Process(double delta)
    {
        elapsedTime += delta;

        if (elapsedTime > 11)
        {
            if (dirty)
            {
                // Saving in the background should be fine. And good to avoid random hitching during gameplay.
                TaskExecutor.Instance.AddTask(new Task(Save));
            }

            elapsedTime = 0;
        }
    }

    public static void ResetAllSeenTutorials()
    {
        if (AlreadySeenTutorialsValue.Count < 1)
            return;

        dirty = true;
        AlreadySeenTutorialsValue.Clear();
    }

    public static void OnGameInit()
    {
        Load();
    }

    private static void Load()
    {
        dirty = false;

        using var file = FileAccess.Open(Constants.TUTORIAL_DATA_FILE, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            // If the player has not used any tutorials / played before, this is not a problem if missing
            return;
        }

        using var fileStream = new GodotFileStream(file);
        using Stream gzoStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using TextReader reader = new StreamReader(gzoStream);
        using var jsonReader = new JsonTextReader(reader);

        var serializer = JsonSerializer.Create();
        var data = serializer.Deserialize<SerializedData>(jsonReader);

        if (data == null)
        {
            GD.PrintErr("Cannot deserialize tutorial data file, won't remember which tutorials have been seen");
            return;
        }

        AlreadySeenTutorialsValue.Clear();

        foreach (var item in data.Seen)
        {
            AlreadySeenTutorialsValue.Add(item);
        }
    }

    private static void Save()
    {
        if (!dirty)
            return;

        dirty = false;

        using var file = FileAccess.Open(Constants.TUTORIAL_DATA_FILE, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr("Cannot open tutorial data file for writing, won't remember which tutorials have been seen");
            return;
        }

        using var fileStream = new GodotFileStream(file);
        using Stream gzoStream = new GZipStream(fileStream, CompressionLevel.Optimal);
        using TextWriter writer = new StreamWriter(gzoStream);

        var serializer = JsonSerializer.Create();

        serializer.Serialize(writer, new SerializedData(AlreadySeenTutorialsValue));
    }

    /// <summary>
    ///   A wrapper object for the on-disk representation of the tutorial data. This is used to allow further
    ///   extensibility to be added.
    /// </summary>
    private class SerializedData
    {
        public SerializedData(ICollection<string> seen)
        {
            Seen = seen;
        }

        public ICollection<string> Seen { get; set; }
    }
}
