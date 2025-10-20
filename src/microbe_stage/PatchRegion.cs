using System;
using System.Collections.Generic;
using Godot;
using SharedBase.Archive;

/// <summary>
///   A region is a something like a continent/ocean that contains multiple patches.
/// </summary>
public class PatchRegion : IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    public PatchRegion(int id, string name, RegionType regionType, Vector2 screenCoordinates)
    {
        ID = id;
        Patches = new List<Patch>();
        Name = name;
        Height = 0;
        Width = 0;
        Type = regionType;
        ScreenCoordinates = screenCoordinates;
    }

    private PatchRegion(int id, string name, RegionType type, Vector2 screenCoordinates,
        float height, float width)
    {
        ID = id;
        Name = name;
        Type = type;
        ScreenCoordinates = screenCoordinates;
        Height = height;
        Width = width;
    }

    public enum RegionType
    {
        Sea = 0,
        Ocean = 1,
        Continent = 2,
        Predefined = 3,
    }

    public RegionType Type { get; }

    public int ID { get; }

    /// <summary>
    ///   Regions this is next to
    /// </summary>
    public ISet<PatchRegion> Adjacent { get; } = new HashSet<PatchRegion>();

    public Dictionary<int, HashSet<Patch>> PatchAdjacencies { get; private set; } = new();

    public float Height { get; set; }

    public float Width { get; set; }

    public Vector2 Size
    {
        get => new(Width, Height);
        set
        {
            Width = value.X;
            Height = value.Y;
        }
    }

    /// <summary>
    ///   The name of the region / continent
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is not translatable as this is just the output from the name generator, which isn't language-specific
    ///     currently. And even once it is a different approach than <see cref="LocalizedString"/> will be needed to
    ///     allow randomly generated names to translate.
    ///   </para>
    /// </remarks>
    public string Name { get; private set; }

    /// <summary>
    ///   Coordinates this region is to be displayed at in the GUI
    /// </summary>
    public Vector2 ScreenCoordinates { get; set; }

    /// <summary>
    ///   The patches in this region. This is last because other constructor params need to be loaded from JSON first
    ///   and also this can't be a JSON constructor parameter because the patches refer to this so we couldn't
    ///   construct anything to begin with.
    /// </summary>
    public List<Patch> Patches { get; private set; } = null!;

    public MapElementVisibility Visibility { get; set; } = MapElementVisibility.Hidden;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.PatchRegion;
    public bool CanBeReferencedInArchive => true;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.PatchRegion)
            throw new NotSupportedException();

        ((PatchRegion)obj).WriteToArchive(writer);
    }

    public static PatchRegion ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new PatchRegion(reader.ReadInt32(),
            reader.ReadString() ?? throw new NullArchiveObjectException(),
            (RegionType)reader.ReadInt32(), reader.ReadVector2(), reader.ReadFloat(), reader.ReadFloat());

        reader.ReportObjectConstructorDone(instance);

        foreach (var item in reader.ReadObject<List<PatchRegion>>())
        {
            instance.Adjacent.Add(item);
        }

        instance.PatchAdjacencies = reader.ReadObject<Dictionary<int, HashSet<Patch>>>();
        instance.Patches = reader.ReadObject<List<Patch>>();
        instance.Visibility = (MapElementVisibility)reader.ReadInt32();

        return instance;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(ID);
        writer.Write(Name);
        writer.Write((int)Type);
        writer.Write(ScreenCoordinates);
        writer.Write(Height);
        writer.Write(Width);

        writer.WriteGenericCollection(Adjacent);
        writer.WriteObject(PatchAdjacencies);
        writer.WriteObject(Patches);
        writer.Write((int)Visibility);
    }

    /// <summary>
    ///   Adds a connection to region
    /// </summary>
    /// <returns>True if this was new, false if already added</returns>
    public bool AddNeighbour(PatchRegion region)
    {
        return Adjacent.Add(region);
    }

    public void AddPatchAdjacency(PatchRegion region, Patch patch)
    {
        var id = region.ID;

        // Don't do this if the patch is in this region
        // (This check is done here to minimize repetition)
        if (id == ID)
            return;

        if (!PatchAdjacencies.TryGetValue(id, out var set))
        {
            PatchAdjacencies[id] = new HashSet<Patch> { patch };
            return;
        }

        set.Add(patch);
    }
}
