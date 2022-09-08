namespace Scripts;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class DehydratedInfo
{
    public DehydratedInfo(ISet<string> dehydratedObjects, string branch, string version, string platform,
        string buildZipHash, string archiveFile)
    {
        DehydratedObjects = dehydratedObjects;
        Branch = branch;
        Version = version;
        Platform = platform;
        BuildZipHash = buildZipHash;
        ArchiveFile = archiveFile;
    }

    public ISet<string> DehydratedObjects { get; }

    public string Branch { get; }

    public string Version { get; }

    public string Platform { get; }

    public string BuildZipHash { get; }
    public string ArchiveFile { get; }

    /// <summary>
    ///   A bit dirty to have this here but the upload script needs to know this info where this was loaded from
    /// </summary>
    [JsonIgnore]
    public string? MetaFile { get; set; }
}
