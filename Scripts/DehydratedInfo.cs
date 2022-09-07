namespace Scripts;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class DehydratedInfo
{
    public DehydratedInfo(ISet<string> dehydratedObjects, string branch, string version, string platform)
    {
        DehydratedObjects = dehydratedObjects;
        Branch = branch;
        Version = version;
        Platform = platform;
    }

    [JsonInclude]
    public ISet<string> DehydratedObjects { get; }

    public string Branch { get; }

    public string Version { get; }

    public string Platform { get; }
}
