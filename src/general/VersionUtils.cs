﻿using System;
using System.Globalization;
using Godot;

/// <summary>
///   Helpers for dealing with Thrive's version number
/// </summary>
public static class VersionUtils
{
    public static readonly string[] Suffixes =
    {
        "pre-alpha",
        "alpha",
        "beta",
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
            return bSplit.Length - aSplit.Length;
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
            return aSuffixIndex - bSuffixIndex;
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

    // TODO: Use actual unit tests instead
    // https://github.com/Revolutionary-Games/Thrive/issues/1571
    public static bool TestCompare()
    {
        return Compare("1.2.3-pre-alpha", "1.2.3") < 0
            && Compare("1.2.3-rc1", "1.2.3-pre-alpha") > 0
            && Compare("1.2.3-rc1", "1.2.4-pre-alpha") < 0
            && Compare("3.2.1-pre-alpha", "1.2.3") > 0
            && Compare("1.2.3-alpha", "1.2.3-potato") < 0
            && Compare("1.2.3", "1.2.3.0") == 0
            && Compare("1.2.3.1", "1.2.3.0") > 0
            && Compare("0.5.3.1-alpha", "0.5.3.1") < 1
            && Compare("0.5.3.1-alpha", "0.5.3") > 0
            ;
    }
}
