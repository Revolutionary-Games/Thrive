using System;
using System.Globalization;
using Godot;

/// <summary>
///   Helpers for dealing with Thrive's version number
/// </summary>
public static class VersionUtils
{
    public static readonly string[] Suffixes =
    {
        "PRE-ALPHA",
        "ALPHA",
        "BETA",
        "RC1",
        "RC2",
        "RC3",
        "RC4",
        "RC5",
        "RC6",
        "RC7",
        "RC8",
        "RC9",
    };

    /// <summary>
    ///   Compare the given version numbers.
    /// </summary>
    /// <returns>
    ///   0 if the versions are the same,
    ///   a negative integer if a is a smaller (older) version than b and
    ///   a positive integer if a is a bigger (newer) version than b
    /// </returns>
    /// <param name="a">The first version to compare.</param>
    /// <param name="b">The second version to compare.</param>
    public static int Compare(string a, string b)
    {
        if (a == b)
        {
            return 0;
        }

        char[] separator = { '-' };
        var aSplit = a.Split(separator, 2);
        var bSplit = b.Split(separator, 2);

        // Compare the numeric versions
        try
        {
            int versionDiff = Version.Parse(aSplit[0]).CompareTo(Version.Parse(bSplit[0]));
            if (versionDiff != 0)
            {
                return versionDiff;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr(e);
        }

        // If only one of the version has a suffix, that one is older
        if (aSplit.Length != bSplit.Length)
        {
            return bSplit.Length - aSplit.Length;
        }

        // Compare predefined suffixes
        int aSuffixIndex = Array.IndexOf(Suffixes, aSplit[1].ToUpperInvariant());
        int bSuffixIndex = Array.IndexOf(Suffixes, bSplit[1].ToUpperInvariant());
        if (aSuffixIndex >= 0 && bSuffixIndex >= 0)
        {
            return aSuffixIndex - bSuffixIndex;
        }

        // Fallback in case one of the suffixes is unknow
        return string.Compare(aSplit[1], bSplit[1], true, CultureInfo.InvariantCulture);
    }

    // TODO: Use actual unit tests instead
    public static bool TestCompare()
    {
        return Compare("1.2.3-pre-alpha", "1.2.3") < 0
            && Compare("1.2.3-rc1", "1.2.3-pre-alpha") > 0
            && Compare("1.2.3-rc1", "1.2.4-pre-alpha") < 0
            && Compare("3.2.1-pre-alpha", "1.2.3") > 0
            && Compare("1.2.3-alpha", "1.2.3-potato") < 0
            ;
    }
}
