using System;
using System.Globalization;
using Godot;

/// <summary>
///   Helpers for dealing with Thrive version number
/// </summary>
public static class VersionUtils
{
    public static readonly string[] Suffixes =
    {
        "pre-alpha",
        "alpha",

        // These alpha and beta suffixes are provided to make save upgrader operations possible to define without
        // needing a much more major version bump while the next stable version is being developed
        "alpha2",
        "alpha3",
        "alpha4",
        "alpha5",
        "alpha6",
        "alpha7",
        "alpha8",
        "alpha9",
        "beta",
        "beta2",
        "beta3",
        "beta4",
        "beta5",
        "beta6",
        "beta7",
        "beta8",
        "beta9",
        "rc1",
        "rc2",
        "rc3",
        "rc4",
        "rc5",
        "rc6",
        "rc7",
        "rc8",
        "rc9",
    };

    /// <summary>
    ///   Compare the given version numbers.
    /// </summary>
    /// <returns>
    ///   0 if the versions are the same,
    ///   a negative integer if a is a smaller (older) version than b and
    ///   a positive integer if a is a bigger (newer) version than b
    ///   int.MaxValue if comparison fails
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
            // parse and make a trailing zero missing insignificant
            var parsedA = NormalizeVersion(Version.Parse(aSplit[0]));
            var parsedB = NormalizeVersion(Version.Parse(bSplit[0]));

            int versionDiff = parsedA.CompareTo(parsedB);
            if (versionDiff != 0)
            {
                return versionDiff;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to compare versions {a} and {b}.");
            GD.PrintErr(e);
            return int.MaxValue;
        }

        // If only one of the version has a suffix, that one is older
        if (aSplit.Length != bSplit.Length)
        {
            if (bSplit.Length - aSplit.Length > 0)
                return 1;

            return -1;
        }

        // Versions are equal if the splits are the same (or if either had no suffix)
        if (aSplit.Equals(bSplit) || aSplit.Length < 2 || bSplit.Length < 2)
        {
            return 0;
        }

        // Compare predefined suffixes
        int aSuffixIndex = Array.IndexOf(Suffixes, aSplit[1].ToLowerInvariant());
        int bSuffixIndex = Array.IndexOf(Suffixes, bSplit[1].ToLowerInvariant());
        if (aSuffixIndex >= 0 && bSuffixIndex >= 0)
        {
            var difference = aSuffixIndex - bSuffixIndex;

            if (difference < 0)
                return -1;

            if (difference > 0)
                return 1;

            return 0;
        }

        // Fallback in case one of the suffixes is unknown
        return string.Compare(aSplit[1], bSplit[1], CultureInfo.InvariantCulture, CompareOptions.OrdinalIgnoreCase);
    }

    /// <summary>
    ///   Normalizes version so that the revision number is 0 if there weren't enough numbers
    /// </summary>
    public static Version NormalizeVersion(Version version)
    {
        if (version.Revision == -1)
        {
            return new Version(version.Major, version.Minor, version.Build, 0);
        }

        return version;
    }
}
