using System;
using System.Globalization;
using Godot;

/// <summary>
///   Helpers for dealing with Thrive's version number
/// </summary>
public static class VersionUtils
{
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

        char[] separators = { '.', '-' };
        var aSplit = a.Split(separators);
        var bSplit = b.Split(separators);

        for (int i = 0; i < aSplit.Length; i++)
        {
            try
            {
                int aNumber = int.Parse(aSplit[i], CultureInfo.CurrentCulture);
                int bNumber = int.Parse(bSplit[i], CultureInfo.CurrentCulture);

                int diff = aNumber - bNumber;
                if (diff != 0)
                {
                    return diff;
                }
            }
            catch (Exception e)
            {
                GD.Print(e.Message);
                GD.Print($"Probably non-numeric version difference: {a} vs. {b}");
            }
        }

        return 0;
    }
}
