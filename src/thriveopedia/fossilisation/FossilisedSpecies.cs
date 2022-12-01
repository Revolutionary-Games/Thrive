﻿using System;
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

/// <summary>
///    A species saved by the user. Contains helper methods for saving/loading species on the disk.
/// </summary>
public class FossilisedSpecies
{
    public const string SAVE_FOSSIL_JSON = "fossil.json";
    public const string SAVE_INFO_JSON = "info.json";
    public const string SAVE_PREVIEW_IMAGE = "preview.png";

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
    ///   Creates a list of existing fossilised species names to prevent unintended overwrites.
    /// </summary>
    /// <returns>A list of names of .thrivefossil files in the user's fossils directory</returns>
    /// <param name="orderByDate">Whether the returned list is ordered by date modified</param>
    public static List<string> CreateListOfFossils(bool orderByDate)
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

        if (orderByDate)
        {
            using var file = new File();
            result = result.OrderBy(item =>
                file.GetModifiedTime(Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, item))).ToList();
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
    /// <returns>The species saved in the provided file</returns>
    public static FossilisedSpecies LoadSpeciesFromFile(string fossilName)
    {
        var target = Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, fossilName);
        var (fossilisedInfo, species, previewImage) = LoadFromFile(target);

        return new FossilisedSpecies(fossilisedInfo, species, Path.GetFileNameWithoutExtension(fossilName))
        {
            PreviewImage = previewImage,
        };
    }

    /// <summary>
    ///   Saves this species to disk.
    /// </summary>
    public void FossiliseToFile()
    {
        if (Species is not MicrobeSpecies)
        {
            throw new NotImplementedException("Saving non-microbe species is not yet implemented");
        }

        var serializedContent = ThriveJsonConverter.Instance.SerializeObject(Species);

        WriteRawFossilDataToFile(Info, serializedContent, Name + Constants.FOSSIL_EXTENSION_WITH_DOT, PreviewImage);
    }

    private static void WriteRawFossilDataToFile(FossilisedSpeciesInformation speciesInfo, string fossilContent,
        string fossilName, Image? previewImage)
    {
        FileHelpers.MakeSureDirectoryExists(Constants.FOSSILISED_SPECIES_FOLDER);
        var target = Path.Combine(Constants.FOSSILISED_SPECIES_FOLDER, fossilName);

        var justInfo = ThriveJsonConverter.Instance.SerializeObject(speciesInfo);

        WriteDataToFossilFile(target, justInfo, fossilContent, previewImage);
    }

    private static void WriteDataToFossilFile(string target, string justInfo, string serialized, Image? previewImage)
    {
        using var file = new File();
        if (file.Open(target, File.ModeFlags.Write) != Error.Ok)
        {
            GD.PrintErr("Cannot open file for writing: ", target);
            throw new IOException("Cannot open: " + target);
        }

        using var fileStream = new GodotFileStream(file);
        using Stream gzoStream = new GZipOutputStream(fileStream);
        using var tar = new TarOutputStream(gzoStream, Encoding.UTF8);

        TarHelper.OutputEntry(tar, SAVE_INFO_JSON, Encoding.UTF8.GetBytes(justInfo));
        TarHelper.OutputEntry(tar, SAVE_FOSSIL_JSON, Encoding.UTF8.GetBytes(serialized));

        if (previewImage != null)
        {
            byte[] data = previewImage.SavePngToBuffer();

            if (data.Length > 0)
                TarHelper.OutputEntry(tar, SAVE_PREVIEW_IMAGE, data);
        }
    }

    private static (FossilisedSpeciesInformation Info, Species Species, Image? PreviewImage) LoadFromFile(string file)
    {
        using (var directory = new Directory())
        {
            if (!directory.FileExists(file))
                throw new ArgumentException("fossil with the given name doesn't exist");
        }

        var (infoStr, fossilStr, previewImageData) = LoadDataFromFile(file);

        if (string.IsNullOrEmpty(infoStr))
        {
            throw new IOException("couldn't find info content in fossil");
        }

        if (string.IsNullOrEmpty(fossilStr))
        {
            throw new IOException("couldn't find fossil content in fossil file");
        }

        var infoResult = ThriveJsonConverter.Instance.DeserializeObject<FossilisedSpeciesInformation>(infoStr!) ??
            throw new JsonException("FossilisedSpeciesInformation is null");

        // Use the info file to deserialize the species to the correct type
        var speciesResult = ThriveJsonConverter.Instance.DeserializeObject<Species>(fossilStr!) ??
            throw new JsonException("Fossil data is null");

        Image? previewImage = null;

        if (previewImageData != null)
        {
            previewImage = TarHelper.ImageFromBuffer(previewImageData);
        }

        return (infoResult, speciesResult, previewImage);
    }

    private static (string? Info, string? Fossil, byte[]? PreviewImageData) LoadDataFromFile(string file)
    {
        string? infoStr = null;
        string? fossilStr = null;
        byte[]? previewImageData = null;

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
                infoStr = TarHelper.ReadStringEntry(tar, (int)tarEntry.Size);
            }
            else if (tarEntry.Name == SAVE_FOSSIL_JSON)
            {
                fossilStr = TarHelper.ReadStringEntry(tar, (int)tarEntry.Size);
            }
            else if (tarEntry.Name == SAVE_PREVIEW_IMAGE)
            {
                previewImageData = TarHelper.ReadBytesEntry(tar, (int)tarEntry.Size);
            }
            else
            {
                GD.PrintErr("Unknown file in fossil: ", tarEntry.Name);
            }
        }

        return (infoStr, fossilStr, previewImageData);
    }
}
