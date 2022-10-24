using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Godot;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;
using Directory = Godot.Directory;
using File = Godot.File;
using Path = System.IO.Path;

public class FossilisedSpecies
{
    public const string SAVE_SAVE_JSON = "save.json";
    public const string SAVE_INFO_JSON = "info.json";

    /// <summary>
    ///   Name of this saved species on disk
    /// </summary>
    public string Name { get; set; } = "invalid";

    /// <summary>
    ///   General information about this saved species
    /// </summary>
    public FossilisedSpeciesInformation Info { get; set; } = new();

    public Species Species { get; set; } = null!;

    public static List<string> CreateListOfSaves()
    {
        var result = new List<string>();

        using (var directory = new Directory())
        {
            if (!directory.DirExists(Constants.FOSSILISED_SPECIES_FOLDER))
                return result;

            directory.Open(Constants.FOSSILISED_SPECIES_FOLDER);
            directory.ListDirBegin(true, true);

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
        }

        using var file = new File();
        result = result.OrderBy(item =>
            file.GetModifiedTime(Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, item))).ToList();

        return result;
    }

    public static Species LoadSpeciesFromFile(string saveName)
    {
        var target = Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, saveName);
        var (_, species) = LoadFromFile(target);

        return species;
    }

    public void SaveToFile()
    {
        if (Species is not MicrobeSpecies)
        {
            throw new NotImplementedException("Saving non-microbe species is not yet implemented");
        }

        var speciesInfo = new FossilisedSpeciesInformation
        {
            Type = FossilisedSpeciesInformation.SpeciesType.Microbe,
        };

        WriteRawSaveDataToFile(speciesInfo, Species.StringCode, Name + Constants.FOSSIL_EXTENSION_WITH_DOT);
    }

    private static void WriteRawSaveDataToFile(FossilisedSpeciesInformation speciesInfo, string saveContent,
        string saveName)
    {
        FileHelpers.MakeSureDirectoryExists(Constants.FOSSILISED_SPECIES_FOLDER);
        var target = Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, saveName);

        var justInfo = ThriveJsonConverter.Instance.SerializeObject(speciesInfo);

        WriteDataToSaveFile(target, justInfo, saveContent);
    }

    private static void WriteDataToSaveFile(string target, string justInfo, string serialized)
    {
        using var file = new File();
        if (file.Open(target, File.ModeFlags.Write) != Error.Ok)
        {
            GD.PrintErr("Cannot open file for writing: ", target);
            throw new IOException("Cannot open: " + target);
        }

        using Stream gzoStream = new GZipOutputStream(new GodotFileStream(file));
        using var tar = new TarOutputStream(gzoStream, Encoding.UTF8);

        OutputEntry(tar, SAVE_INFO_JSON, Encoding.UTF8.GetBytes(justInfo));
        OutputEntry(tar, SAVE_SAVE_JSON, Encoding.UTF8.GetBytes(serialized));
    }

    private static void OutputEntry(TarOutputStream archive, string name, byte[] data)
    {
        var entry = TarEntry.CreateTarEntry(name);

        entry.TarHeader.Mode = Convert.ToInt32("0664", 8);

        // TODO: could fill in more of the properties

        entry.Size = data.Length;

        archive.PutNextEntry(entry);

        archive.Write(data, 0, data.Length);

        archive.CloseEntry();
    }

    private static (FossilisedSpeciesInformation Info, Species Species) LoadFromFile(string file)
    {
        using (var directory = new Directory())
        {
            if (!directory.FileExists(file))
                throw new ArgumentException("save with the given name doesn't exist");
        }

        var (infoStr, saveStr) = LoadDataFromFile(file);

        if (string.IsNullOrEmpty(infoStr))
        {
            throw new IOException("couldn't find info content in save");
        }

        if (string.IsNullOrEmpty(saveStr))
        {
            throw new IOException("couldn't find save content in save file");
        }

        var infoResult = ThriveJsonConverter.Instance.DeserializeObject<FossilisedSpeciesInformation>(infoStr!) ??
            throw new JsonException("SaveInformation is null");

        Species? speciesResult;
        switch (infoResult.Type)
        {
            case FossilisedSpeciesInformation.SpeciesType.Microbe:
                speciesResult = ThriveJsonConverter.Instance.DeserializeObject<MicrobeSpecies>(saveStr!) ??
                    throw new JsonException("Save data is null");
                break;
            default:
                throw new NotImplementedException("Unable to load non-microbe species");
        }

        return (infoResult, speciesResult);
    }

    private static (string? Info, string? Save) LoadDataFromFile(string file)
    {
        string? infoStr = null;
        string? saveStr = null;

        using var reader = new File();
        reader.Open(file, File.ModeFlags.Read);

        if (!reader.IsOpen())
            throw new ArgumentException("couldn't open the file for reading");

        using var stream = new GodotFileStream(reader);
        using Stream gzoStream = new GZipInputStream(stream);
        using var tar = new TarInputStream(gzoStream, Encoding.UTF8);

        TarEntry tarEntry;
        while ((tarEntry = tar.GetNextEntry()) != null)
        {
            if (tarEntry.IsDirectory)
                continue;

            if (tarEntry.Name == SAVE_INFO_JSON)
            {
                infoStr = ReadStringEntry(tar, (int)tarEntry.Size);
            }
            else if (tarEntry.Name == SAVE_SAVE_JSON)
            {
                saveStr = ReadStringEntry(tar, (int)tarEntry.Size);
            }
            else
            {
                GD.PrintErr("Unknown file in save: ", tarEntry.Name);
            }
        }

        return (infoStr, saveStr);
    }

    private static FossilisedSpeciesInformation ParseSaveInfo(string? infoStr)
    {
        if (string.IsNullOrEmpty(infoStr))
        {
            throw new IOException("couldn't find info content in save");
        }

        return ThriveJsonConverter.Instance.DeserializeObject<FossilisedSpeciesInformation>(infoStr!) ??
            throw new JsonException("SaveInformation is null");
    }

    private static string ReadStringEntry(TarInputStream tar, int length)
    {
        // Pre-allocate storage
        var buffer = new byte[length];
        {
            using var stream = new MemoryStream(buffer);
            tar.CopyEntryContents(stream);
        }

        return Encoding.UTF8.GetString(buffer);
    }
}
