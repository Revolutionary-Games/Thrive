using Godot;
using Newtonsoft.Json;

public class ModInfo : Node
{
    [JsonProperty("Name")]
    public string ModName;

    public string Author;
    public string Version;
    public string Description;
    public string Location;
    public string Dll;
}
