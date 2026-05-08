using System;
using System.Collections.Generic;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Playthrough-specific data for the Thriveopedia stored inside a <see cref="GameProperties"/> for storing in saves
/// </summary>
public class ThriveopediaGameData : IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public delegate void OnPinnedPageChanged(string page, bool pinned);

    public event OnPinnedPageChanged? PinnedPageChanged;

    public HashSet<string> PinnedPages { get; set; } = new();

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.ThriveopediaGameData;
    public bool CanBeReferencedInArchive => true;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.ThriveopediaGameData)
            throw new NotSupportedException();

        writer.WriteObject((ThriveopediaGameData)obj);
    }

    public static ThriveopediaGameData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new ThriveopediaGameData();
        reader.ReportObjectConstructorDone(instance, referenceId);

        var pinnedPages = reader.ReadObject<HashSet<string>>();
        foreach (var page in pinnedPages)
        {
            instance.PinnedPages.Add(page);
        }

        return instance;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(PinnedPages);
    }

    public bool IsPagePinned(IThriveopediaPage page)
    {
        return PinnedPages.Contains(page.PageName);
    }

    public void SetPagePinned(IThriveopediaPage page, bool pinned)
    {
        if (pinned)
        {
            if (PinnedPages.Add(page.PageName))
            {
                PinnedPageChanged?.Invoke(page.PageName, true);
            }
        }
        else
        {
            if (PinnedPages.Remove(page.PageName))
            {
                PinnedPageChanged?.Invoke(page.PageName, false);
            }
        }
    }

    public IEnumerable<uint> CalculatePinnedSpecies()
    {
        foreach (var pinnedPage in PinnedPages)
        {
            if (pinnedPage.StartsWith("species:"))
            {
                var specialName = pinnedPage.Split("species:", 2)[1];
                if (uint.TryParse(specialName, out var speciesId))
                {
                    yield return speciesId;
                }
                else
                {
                    GD.PrintErr("Unexpected pinned species name: " + pinnedPage);
                }
            }
        }
    }
}
