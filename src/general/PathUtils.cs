/// <summary>
///   Helpers for dealing with paths
/// </summary>
public static class PathUtils
{
    public const char PATH_SEPARATOR = '/';

    public static string Join(string first, string second)
    {
        if (string.IsNullOrEmpty(first))
            return second;
        if (string.IsNullOrEmpty(second))
            return first;

        bool firstSeparator = first[first.Length - 1] == PATH_SEPARATOR;
        bool secondSeparator = second[0] == PATH_SEPARATOR;

        if (firstSeparator && secondSeparator)
        {
            return first.Substring(0, first.Length - 1) + second;
        }

        if (firstSeparator || secondSeparator)
        {
            return first + second;
        }

        return first + PATH_SEPARATOR + second;
    }
}
