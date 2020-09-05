using Godot;
using Newtonsoft.Json;

public class ModInfo : Godot.Object
{
    public string Name;

    public string Author;
    public string Version;
    public string Description;
    public string Location;
    public string Dll;

    public bool AutoLoad = false;
}
