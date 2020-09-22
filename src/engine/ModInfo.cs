using Godot;
using Newtonsoft.Json;

/// <summary>
///   Class that holds the info of a mod from a 'mod_info.json' file
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class ModInfo : Object
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

    public override bool Equals(object other)
    {
        var item = other as ModInfo;

        if (item == null)
        {
            return false;
        }

        return Name == item.Name && Location == item.Location && Version == item.Version;
    }

    public override int GetHashCode()
    {
        return (Name, Location, Version).GetHashCode();
    }
}
