using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Godot;
using Newtonsoft.Json;
using Saving.Serializers;
using SharedBase.Archive;
using DirAccess = Godot.DirAccess;
using FileAccess = Godot.FileAccess;
using Path = System.IO.Path;

/// <summary>
///   A species saved by the user. Contains helper methods for saving/loading species on the disk.
/// </summary>
public class FossilisedSpecies : IArchivable
{
    public const int FOSSIL_VERSION = 2;

    public const ushort SERIALIZATION_VERSION = 1;

    public const string SAVE_FOSSIL_ARCHIVE = "fossil.bin";
    public const string SAVE_INFO_JSON = "info.json";
    public const string SAVE_PREVIEW_IMAGE = "preview.png";

    private static readonly object ManagerLock = new();
    private static ThriveArchiveManager? archiveManager;

    /// <summary>
    ///   A species saved by the user.
    /// </summary>
    /// <param name="info">Details about the species to save</param>
    /// <param name="species">The species to fossilise</param>
    /// <param name="name">The name of the species to use as the file name</param>
    public FossilisedSpecies(FossilisedSpeciesInformation info, Species species, string name)
    {
        Info = info;
        Species = species;
        Name = name;

        // Make sure the name is up to date in the data
        Info.FormattedName = species.FormattedName;
    }

    /// <summary>
    ///   General information about this saved species.
    /// </summary>
    public FossilisedSpeciesInformation Info { get; private set; }

    /// <summary>
    ///   The species to be saved/loaded.
    /// </summary>
    public Species Species { get; private set; }

    /// <summary>
    ///   Name of this saved species on disk.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    ///   Preview image of this fossil
    /// </summary>
    [JsonIgnore]
    public Image? PreviewImage { get; set; }

    /// <summary>
    ///   Fossil file major version. Not default initialised so that read files with no version can be detected.
    /// </summary>
    public int FossilVersion { get; set; }

    [JsonIgnore]
    public bool IsInvalidOrOutdated { get; set; }

    [JsonIgnore]
    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    [JsonIgnore]
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.FossilisedSpecies;

    [JsonIgnore]
    public bool CanBeReferencedInArchive => false;

    /// <summary>
    ///   Creates a list of existing fossilised species names to prevent unintended overwrites.
    /// </summary>
    /// <returns>A list of names of .thrivefossil files in the user's fossils directory</returns>
    /// <param name="orderByDate">Whether the returned list is ordered by date modified</param>
    public static List<string> CreateListOfFossils(bool orderByDate)
    {
        var result = new List<string>();

        using var directory = DirAccess.Open(Constants.FOSSILISED_SPECIES_FOLDER);
        if (directory == null)
            return result;

        if (directory.ListDirBegin() != Error.Ok)
        {
            GD.PrintErr("Failed to start listing fossils folder contents");
            return result;
        }

        while (true)
        {
            var filename = directory.GetNext();

            if (string.IsNullOrEmpty(filename))
                break;

            if (!filename.EndsWith(Constants.FOSSIL_EXTENSION, StringComparison.Ordinal))
                continue;

            // Skip folders
            if (!directory.FileExists(filename))
                continue;

            result.Add(filename);
        }

        directory.ListDirEnd();

        if (orderByDate)
        {
            result = result.OrderBy(s =>
                FileAccess.GetModifiedTime(Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, s))).ToList();
        }

        return result;
    }

    /// <summary>
    ///   Checks whether a species with the same name already exists.
    /// </summary>
    /// <param name="name">The species name to check</param>
    /// <param name="existingFossilNames">A cached list of fossils if appropriate</param>
    /// <returns>True if a species with this name has already been fossilised and false otherwise</returns>
    public static bool IsSpeciesAlreadyFossilised(string name, List<string>? existingFossilNames = null)
    {
        existingFossilNames ??= CreateListOfFossils(false);
        var fileName = name + Constants.FOSSIL_EXTENSION_WITH_DOT;
        return existingFossilNames.Any(n => n == fileName);
    }

    /// <summary>
    ///   Loads a fossilised species by its filename.
    /// </summary>
    /// <param name="fossilName">The name of the .thrivefossil file (including extension)</param>
    /// <returns>
    ///   The species saved in the provided file, or null if the file doesn't exist or is corrupt. The returned object
    ///   can now also mark if it is an outdated entry that cannot be fully loaded even if some info is known.
    /// </returns>
    public static FossilisedSpecies? LoadSpeciesFromFile(string fossilName)
    {
        var target = Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, fossilName);
        try
        {
            var (fossilisedInfo, species, previewImage) = LoadFromFile(target, true);

            if (species == null)
                throw new IOException("Fossil file is missing the main archive data");

            // Make sure the name is up to date
            species.Name = Path.GetFileNameWithoutExtension(fossilName);

            // And pass along the preview image
            species.PreviewImage = previewImage;

            // We assume the info in the main archive is correct, so we don't copy it here
            _ = fossilisedInfo;

            return species;
        }
        catch (Exception e)
        {
            // This fossil doesn't exist or is corrupt, so just don't bother showing it
            GD.PrintErr($"Error loading fossil: {e}");
            return null;
        }
    }

    public static (FossilisedSpeciesInformation? Info, Image? Image) LoadSpeciesInfoFromFile(string fossilName,
        out string plainName)
    {
        plainName = Path.GetFileNameWithoutExtension(fossilName);
        var target = Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, fossilName);

        try
        {
            var (fossilisedInfo, _, previewImage) = LoadFromFile(target, false);

            return (fossilisedInfo, previewImage);
        }
        catch (Exception e)
        {
            // The file is so badly formed that we can't show anything about it
            GD.PrintErr($"Error loading fossil info: {e}");
            return (null, null);
        }
    }

    /// <summary>
    ///   Deletes a fossilised species by its filename.
    /// </summary>
    /// <param name="fossilName">The name of the .thrivefossil file (including extension)</param>
    public static void DeleteFossilFile(string fossilName)
    {
        var target = Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, fossilName);

        if (!FileAccess.FileExists(target))
            throw new IOException("Fossil with the given name doesn't exist");

        if (DirAccess.RemoveAbsolute(target) != Error.Ok)
        {
            throw new IOException("Cannot delete: " + target);
        }
    }

    public static FossilisedSpecies ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new FossilisedSpecies(reader.ReadObject<FossilisedSpeciesInformation>(),
            reader.ReadObject<Species>(), reader.ReadString() ?? throw new NullArchiveObjectException())
        {
            FossilVersion = reader.ReadInt32(),
        };

        if (instance.FossilVersion < FOSSIL_VERSION)
            instance.IsInvalidOrOutdated = true;

        return instance;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(Info);
        writer.WriteObject(Species);
        writer.Write(Name);
        writer.Write(FossilVersion);
    }

    /// <summary>
    ///   Saves this species to disk.
    /// </summary>
    public void FossiliseToFile()
    {
        if (Species is not MicrobeSpecies)
        {
            // At least multicellular species should in theory work already, but it is untested
            throw new NotImplementedException("Saving non-microbe species is not yet implemented");
        }

        // Set the version just before writing
        FossilVersion = FOSSIL_VERSION;

        using var dataArchive = new MemoryStream();

        lock (ManagerLock)
        {
            archiveManager ??= new ThriveArchiveManager();

            using var writer = new SArchiveMemoryWriter(dataArchive, archiveManager, false);

            archiveManager.OnStartNewWrite(writer);

            writer.WriteArchiveHeader(ISArchiveWriter.ArchiveHeaderVersion, "thrive", Constants.VersionFull);

            writer.WriteObject(this);

            writer.WriteArchiveFooter();
            archiveManager.OnFinishWrite(writer);
        }

        dataArchive.Position = 0;

        WriteRawFossilDataToFile(Info, dataArchive, Name + Constants.FOSSIL_EXTENSION_WITH_DOT, PreviewImage);
    }

    private static void WriteRawFossilDataToFile(FossilisedSpeciesInformation speciesInfo, Stream fossilContent,
        string fossilName, Image? previewImage)
    {
        FileHelpers.MakeSureDirectoryExists(Constants.FOSSILISED_SPECIES_FOLDER);
        var target = Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, fossilName);

        var justInfo = ThriveJsonConverter.Instance.SerializeObject(speciesInfo);

        WriteDataToFossilFile(target, justInfo, fossilContent, previewImage);
    }

    private static void WriteDataToFossilFile(string target, string justInfo, Stream serialized, Image? previewImage)
    {
        using var file = FileAccess.Open(target, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            throw new IOException("Cannot open: " + target);
        }

        using var fileStream = new GodotFileStream(file);
        using Stream gzoStream = new GZipStream(fileStream, CompressionLevel.Optimal);
        using var tar = new TarWriter(gzoStream, TarEntryFormat.Pax);

        using var entryContent = new MemoryStream(justInfo.Length);
        using var entryWriter = new StreamWriter(entryContent, Encoding.UTF8);

        TarHelper.OutputEntry(tar, SAVE_INFO_JSON, justInfo, entryContent, entryWriter);

        // Image is written before the data as often just the info and image are needed
        if (previewImage != null)
        {
            byte[] data = previewImage.SavePngToBuffer();

            if (data.Length > 0)
                TarHelper.OutputEntry(tar, SAVE_PREVIEW_IMAGE, data);
        }

        TarHelper.OutputEntry(tar, SAVE_FOSSIL_ARCHIVE, serialized);
    }

    private static (FossilisedSpeciesInformation Info, FossilisedSpecies? Species, Image? PreviewImage)
        LoadFromFile(string file, bool loadFossilArchive)
    {
        if (!FileAccess.FileExists(file))
            throw new IOException("Fossil with the given name doesn't exist");

        var (infoStr, fossilData, previewImageData) = LoadDataFromFile(file, loadFossilArchive);

        if (string.IsNullOrEmpty(infoStr))
        {
            throw new IOException("Couldn't find info content in fossil");
        }

        if (fossilData == null && loadFossilArchive)
        {
            throw new IOException("Couldn't find fossil content in fossil file");
        }

        var infoResult = ThriveJsonConverter.Instance.DeserializeObject<FossilisedSpeciesInformation>(infoStr) ??
            throw new JsonException("FossilisedSpeciesInformation is null");

        FossilisedSpecies? speciesResult = null;

        if (fossilData != null)
        {
            lock (ManagerLock)
            {
                archiveManager ??= new ThriveArchiveManager();

                var manager = new ThriveArchiveManager();
                using var reader = new SArchiveMemoryReader(fossilData, manager, true);

                manager.OnStartNewRead(reader);

                reader.ReadArchiveHeader(out var version, out var program, out _);

                if (version != ISArchiveWriter.ArchiveHeaderVersion)
                    throw new IOException($"Fossil file format is incompatible: {version}");

                if (program != "thrive")
                    throw new IOException("Fossil archive is from a different program");

                speciesResult = reader.ReadObjectOrNull<FossilisedSpecies>() ??
                    throw new NullArchiveObjectException("Fossil data is null");

                reader.ReadArchiveFooter();

                manager.OnFinishRead(reader);
            }
        }

        Image? previewImage = null;

        if (previewImageData != null)
        {
            previewImage = TarHelper.ImageFromBuffer(previewImageData);
        }

        return (infoResult, speciesResult, previewImage);
    }

    private static (string? Info, MemoryStream? Fossil, byte[]? PreviewImageData) LoadDataFromFile(string file,
        bool loadFossilArchive)
    {
        string? infoStr = null;
        MemoryStream? fossilData = null;
        byte[]? previewImageData = null;

        using var reader = FileAccess.Open(file, FileAccess.ModeFlags.Read);

        if (reader == null)
            throw new IOException("Couldn't open the file for reading");

        using var stream = new GodotFileStream(reader);
        using Stream gzoStream = new GZipStream(stream, CompressionMode.Decompress);
        using var tar = new TarReader(gzoStream);

        while (tar.GetNextEntry(false) is { } tarEntry)
        {
            if (tarEntry.EntryType is not TarEntryType.V7RegularFile and not TarEntryType.RegularFile)
                continue;

            if (tarEntry.DataStream == null)
                continue;

            if (tarEntry.Name == SAVE_INFO_JSON)
            {
                infoStr = TarHelper.ReadStringEntry(tarEntry);
            }
            else if (tarEntry.Name == SAVE_FOSSIL_ARCHIVE)
            {
                if (loadFossilArchive)
                {
                    // TODO: theoretically the new archive format would allow us to create a stream for direct
                    // reading here
                    var raw = TarHelper.ReadBytesEntry(tarEntry);

                    fossilData = new MemoryStream(raw, 0, raw.Length, true, true);
                }
            }
            else if (tarEntry.Name == SAVE_PREVIEW_IMAGE)
            {
                previewImageData = TarHelper.ReadBytesEntry(tarEntry);
            }
            else
            {
                GD.Print("Unknown file in fossil: ", tarEntry.Name);
            }
        }

        return (infoStr, fossilData, previewImageData);
    }
}
