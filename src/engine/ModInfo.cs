using Godot;
using Newtonsoft.Json;

/// <summary>
///   Class that holds the info of a mod from a 'mod_info.json' file
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class ModInfo : Resource
{
    [JsonProperty]
    public string Name;

    [JsonProperty("Display Name")]
    public string DisplayName;

    [JsonProperty("Compatible Versions")]
    public string[] CompatibleVersion;

    [JsonProperty("Incompatible Versions")]
    public string[] IncompatibleVersion;

    [JsonProperty]
    public string[] Dependencies;

    [JsonProperty("Incompatible Mods")]
    public string[] IncompatibleMods;

    [JsonProperty("Load Before")]
    public string[] LoadBefore;

    [JsonProperty("Load After")]
    public string[] LoadAfter;

    [JsonProperty]
    public string Author { get; set; }

    [JsonProperty]
    public string Version { get; set; }

    [JsonProperty]
    public string Description { get; set; }

    [JsonProperty]
    public string Location { get; set; }

    [JsonProperty]
    public string Dll { get; set; }

    [JsonProperty]
    public bool AutoLoad { get; set; }

    public ImageTexture IconImage { get; set; }

    public ImageTexture PreviewImage { get; set; }

    // 1 = Compatible, 0 = Unknown, -1 = Might Not Be Compatible, -2 = Not Compatible
    public int IsCompatibleVersion { get; set; }

    // The index of the mod is loaded in
    public int LoadPosition { get; set; }

    // This uses the same numbering scheme as the load function in the ModLoader
    public int Status { get; set; }

    public override bool Equals(object other)
    {
        var item = other as ModInfo;

        if (item == null)
        {
            return false;
        }

        return Name == item.Name && Location == item.Location && Version == item.Version && Author == item.Author;
    }

    public override int GetHashCode()
    {
        return (Name, Location, Version, Author).GetHashCode();
    }
}
