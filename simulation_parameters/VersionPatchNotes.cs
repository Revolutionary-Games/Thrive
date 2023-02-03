using System.Collections.Generic;

/// <summary>
///   Patch notes for a Thrive version. This doesn't contain the version number as this is meant to be stored in a
///   dictionary.
/// </summary>
/// <remarks>
///   <para>
///     TODO: merge this and the equivalent file in the Scripts folder once there's a common module for Thrive
///   </para>
/// </remarks>
public class VersionPatchNotes
{
    public VersionPatchNotes(string introductionText, List<string> patchNotes, string releaseLink)
    {
        IntroductionText = introductionText;
        PatchNotes = patchNotes;
        ReleaseLink = releaseLink;
    }

    public string IntroductionText { get; }

    public List<string> PatchNotes { get; }

    public string ReleaseLink { get; }
}
