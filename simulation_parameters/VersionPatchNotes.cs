using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

/// <summary>
///   Patch notes for a Thrive version. This doesn't contain the version number as this is meant to be stored in a
///   dictionary.
/// </summary>
/// <remarks>
///   <para>
///     TODO: merge this and the equivalent file in the Scripts folder once there's a common module for Thrive
///   </para>
/// </remarks>
public class VersionPatchNotes : IRegistryType
{
    [JsonConstructor]
    public VersionPatchNotes(string introductionText, List<string> patchNotes, string releaseLink)
    {
        IntroductionText = introductionText;
        PatchNotes = patchNotes;
        ReleaseLink = releaseLink;
    }

    /// <summary>
    ///   Constructor for YAML reading
    /// </summary>
    public VersionPatchNotes()
    {
        IntroductionText = null!;
        PatchNotes = null!;
        ReleaseLink = null!;
    }

    [YamlMember]
    [JsonProperty]
    public string IntroductionText { get; private set; }

    [YamlMember]
    [JsonProperty]
    public List<string> PatchNotes { get; private set; }

    [YamlMember]
    [JsonProperty]
    public string ReleaseLink { get; private set; }

    [YamlIgnore]
    [JsonIgnore]
    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (string.IsNullOrWhiteSpace(IntroductionText))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing introduction text");

        // Seems like YAML very easily has trailing space, so just trim that always
        IntroductionText = IntroductionText.TrimEnd();

        if (IntroductionText.TrimStart() != IntroductionText)
            throw new InvalidRegistryDataException(name, GetType().Name, "Introduction has preceding whitespace");

        if (string.IsNullOrWhiteSpace(ReleaseLink))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing release link");

        if (!Uri.TryCreate(ReleaseLink, UriKind.Absolute, out _))
            throw new InvalidRegistryDataException(name, GetType().Name, "Release link is not valid uri");

        if (PatchNotes.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing patch notes list");

        foreach (var patchNote in PatchNotes)
        {
            if (string.IsNullOrWhiteSpace(patchNote))
            {
                throw new InvalidRegistryDataException(name, GetType().Name, "A patch note entry is empty");
            }

            if (patchNote.Trim() != patchNote)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    $"A patch note entry ({patchNote}) has trailing or preceding whitespace");
            }
        }
    }

    public void ApplyTranslations()
    {
    }
}
