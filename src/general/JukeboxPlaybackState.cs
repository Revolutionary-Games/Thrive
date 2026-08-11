using System.Collections.Generic;
using SharedBase.Archive;

/// <summary>
///   Saved state for music playback that needs to survive save loading.
/// </summary>
public class JukeboxPlaybackState : IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public Dictionary<string, Dictionary<string, float>> TrackPositionsByCategory { get; } = new();

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.JukeboxPlaybackState;

    public bool CanBeReferencedInArchive => false;

    public static JukeboxPlaybackState ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var state = new JukeboxPlaybackState();
        var trackPositions = reader.ReadObjectOrNull<Dictionary<string, Dictionary<string, float>>>();

        if (trackPositions != null)
        {
            foreach (var (category, positions) in trackPositions)
            {
                state.TrackPositionsByCategory[category] = positions;
            }
        }

        return state;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(TrackPositionsByCategory);
    }
}
