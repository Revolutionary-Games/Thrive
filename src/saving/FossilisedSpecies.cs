using System;
using System.IO;
using System.Text;
using Godot;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Directory = Godot.Directory;
using File = Godot.File;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

public class FossilisedSpecies
{
    public const string SAVE_SAVE_JSON = "save.json";

    /// <summary>
    ///   Name of this saved species on disk
    /// </summary>
    public string Name { get; set; } = "invalid";

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

                // if (!filename.EndsWith(Constants.SAVE_EXTENSION, StringComparison.Ordinal))
                //     continue;

                // Skip folders
                if (!directory.FileExists(filename))
                    continue;

                result.Add(filename);
            }

            directory.ListDirEnd();
        }

        using var file = new File();
        result = result.OrderBy(item =>
            file.GetModifiedTime(System.IO.Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, item))).ToList();

        return result;
    }

    public void SaveToFile()
    {
        WriteRawSaveDataToFile(Species, Species.StringCode, Name);
    }

    public static Species? LoadFromFile(string saveName, Action? readFinished = null)
    {
        var target = System.IO.Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, saveName);

        return LoadFromFile(target, true, readFinished);
    }

    private static void WriteRawSaveDataToFile(Species speciesInfo, string saveContent, string saveName)
    {
        FileHelpers.MakeSureDirectoryExists(Constants.FOSSILISED_SPECIES_FOLDER);
        var target = System.IO.Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, saveName);

        WriteDataToSaveFile(target, saveContent);
    }

    private static void WriteDataToSaveFile(string target, string serialized)
    {
        using var file = new File();
        if (file.Open(target, File.ModeFlags.Write) != Error.Ok)
        {
            GD.PrintErr("Cannot open file for writing: ", target);
            throw new IOException("Cannot open: " + target);
        }

        using Stream gzoStream = new GZipOutputStream(new GodotFileStream(file));
        using var tar = new TarOutputStream(gzoStream, Encoding.UTF8);

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

    private static Species? LoadFromFile(string file, bool save, Action? readFinished)
    {
        using (var directory = new Directory())
        {
            if (!directory.FileExists(file))
                throw new ArgumentException("save with the given name doesn't exist");
        }

        var saveStr = LoadDataFromFile(file);

        readFinished?.Invoke();

        Species? saveResult = null;

        if (save)
        {
            if (string.IsNullOrEmpty(saveStr))
            {
                throw new IOException("couldn't find save content in save file");
            }

            // This deserializes a huge tree of objects
            saveResult = ThriveJsonConverter.Instance.DeserializeObject<MicrobeSpecies>(saveStr!) ??
                throw new JsonException("Save data is null");
        }

        return saveResult;
    }

    private static string? LoadDataFromFile(string file)
    {
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

            if (tarEntry.Name == SAVE_SAVE_JSON)
            {
                saveStr = ReadStringEntry(tar, (int)tarEntry.Size);
            }
            else
            {
                GD.PrintErr("Unknown file in save: ", tarEntry.Name);
            }
        }

        return saveStr;
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