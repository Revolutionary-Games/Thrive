namespace Saving.Serializers;

using SharedBase.Archive;

/// <summary>
///   Thrive-customised archive manager that registers custom types
/// </summary>
public class ThriveArchiveManager : DefaultArchiveManager
{
    public ThriveArchiveManager() : base(true)
    {
        // Register custom types for Thrive
        RegisterObjectType((ArchiveObjectType)ThriveArchiveObjectType.Save, typeof(Save), Save.ReadFromArchive);
    }
}
