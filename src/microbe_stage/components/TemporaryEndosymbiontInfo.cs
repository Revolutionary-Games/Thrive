namespace Components;

using System.Collections.Generic;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Holds data related to organelles that have been added as temporary endosymbionts
/// </summary>
public struct TemporaryEndosymbiontInfo : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public List<Species>? EndosymbiontSpeciesPresent;

    public List<Species>? CreatedOrganelleInstancesFor;

    public bool Applied;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentTemporaryEndosymbiontInfo;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObjectOrNull(EndosymbiontSpeciesPresent);
        writer.WriteObjectOrNull(CreatedOrganelleInstancesFor);
        writer.Write(Applied);
    }
}

public static class TemporaryEndosymbiontInfoHelpers
{
    public static TemporaryEndosymbiontInfo ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > TemporaryEndosymbiontInfo.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, TemporaryEndosymbiontInfo.SERIALIZATION_VERSION);

        return new TemporaryEndosymbiontInfo
        {
            EndosymbiontSpeciesPresent = reader.ReadObjectOrNull<List<Species>>(),
            CreatedOrganelleInstancesFor = reader.ReadObjectOrNull<List<Species>>(),
            Applied = reader.ReadBool(),
        };
    }

    public static void AddSpeciesEndosymbiont(this ref TemporaryEndosymbiontInfo info, Species species)
    {
        info.EndosymbiontSpeciesPresent ??= new List<Species>();
        info.EndosymbiontSpeciesPresent.Add(species);
        info.Applied = false;
    }

    public static void Clear(this ref TemporaryEndosymbiontInfo info)
    {
        info.EndosymbiontSpeciesPresent?.Clear();
        info.CreatedOrganelleInstancesFor?.Clear();
    }

    /// <summary>
    ///   Apply progress of endosymbiosis to the target species based on the data in the component. Note that this
    ///   doesn't clear the progress, so the organelle layout the progress is from should be cleared, or
    ///   <see cref="Clear"/> called before this method is called again.
    /// </summary>
    /// <param name="info">Where to check what was engulfed to give progress on</param>
    /// <param name="species">Where to put the progress info</param>
    public static void UpdateEndosymbiosisProgress(this ref TemporaryEndosymbiontInfo info, Species species)
    {
        if (info.CreatedOrganelleInstancesFor is not { Count: > 0 })
            return;

        foreach (var progressedSpecies in info.CreatedOrganelleInstancesFor)
        {
            if (!species.Endosymbiosis.ReportEndosymbiosisProgress(progressedSpecies))
                GD.PrintErr("Couldn't update progress on endosymbiosis of: ", progressedSpecies.FormattedIdentifier);
        }
    }
}
