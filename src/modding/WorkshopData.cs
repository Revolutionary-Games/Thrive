using System.Collections.Generic;
using System.IO;
using Godot;
using Newtonsoft.Json;
using FileAccess = Godot.FileAccess;

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
        using var file = FileAccess.Open(Constants.WORKSHOP_DATA_FILE, FileAccess.ModeFlags.Read);
        if (file == null)
            return new WorkshopData();

        var data = file.GetAsText();

        return JsonSerializer.Create().Deserialize<WorkshopData>(new JsonTextReader(new StringReader(data))) ??
            new WorkshopData();
    }

    public void Save()
    {
        using var file = FileAccess.Open(Constants.WORKSHOP_DATA_FILE, FileAccess.ModeFlags.Write);

        if (file == null)
            throw new IOException("Can't open workshop data file for writing");

        var serialized = new StringWriter();

        JsonSerializer.Create().Serialize(serialized, this);

        file.StoreString(serialized.ToString());
    }

    /// <summary>
    ///   Removes all data regarding a mod
    /// </summary>
    /// <param name="mod">The internal name of the mod</param>
    public void RemoveDataForMod(string mod)
    {
        KnownModWorkshopIds.Remove(mod);
        PreviouslyUploadedItemData.Remove(mod);
    }
}
