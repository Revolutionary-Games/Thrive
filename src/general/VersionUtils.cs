/// <summary>
///   Helpers for dealing with Thrive's version number
/// </summary>
public static class VersionUtils
{
    /// <summary>
    /// Compare the given version numbers.
    /// </summary>
    /// <returns>
    /// 0 if the versions are the same,
    /// a negative integer if a is a smaller (older) version than b and
    /// a positive integer if a is a bigger (newer) version than b
    /// </returns>
    /// <param name="a">The first version to compare.</param>
    /// <param name="b">The second version to compare.</param>
    public static int Compare(string a, string b)
    {
        if (a == b)
        {
            return 0;
        }

        var aSplit = a.Split('.');
        var bSplit = b.Split('.');

        for (int i = 0; i < aSplit.Length; i++)
        {
            int aNumber = int.Parse(aSplit[i]);
            int bNumber = int.Parse(bSplit[i]);

            int diff = aNumber - bNumber;

            if (diff == 0)
            {
                continue;
            }

            return diff;
        }

        return 0;
    }
}
