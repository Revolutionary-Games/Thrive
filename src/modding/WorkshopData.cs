using System.Collections.Generic;
using System.IO;
using Godot;
using Newtonsoft.Json;
using File = Godot.File;

/// <summary>
///   Holds data that we need to interact with the Steam workshop
/// </summary>
public class WorkshopData
{
    [JsonProperty]
    public Dictionary<string, ulong> KnownModWorkshopIds { get; private set; } = new();

    [JsonProperty]
    public Dictionary<string, WorkshopItemData> PreviouslyUploadedItemData { get; private set; } = new();

    public static WorkshopData Load()
    {
        using var file = new File();
        if (!file.FileExists(Constants.WORKSHOP_DATA_FILE))
            return new WorkshopData();

        if (file.Open(Constants.WORKSHOP_DATA_FILE, File.ModeFlags.Read) != Error.Ok)
            throw new IOException("Can't read workshop data file even though it exists");

        var data = file.GetAsText();

        return JsonSerializer.Create().Deserialize<WorkshopData>(new JsonTextReader(new StringReader(data)));
    }

    public void Save()
    {
        using var file = new File();

        if (file.Open(Constants.WORKSHOP_DATA_FILE, File.ModeFlags.Write) != Error.Ok)
            throw new IOException("Can't open workshop data file for writing");

        var serialized = new StringWriter();

        JsonSerializer.Create().Serialize(serialized, this);

        file.StoreString(serialized.ToString());
    }
}
