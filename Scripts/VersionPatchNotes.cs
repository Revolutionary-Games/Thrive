namespace Scripts;

using System.Collections.Generic;

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
