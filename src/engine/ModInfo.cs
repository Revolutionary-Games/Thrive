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
