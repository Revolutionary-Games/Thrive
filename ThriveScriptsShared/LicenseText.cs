namespace ThriveScriptsShared;

using System.Text.RegularExpressions;

public static class LicenseText
{
    public const string LICENSE_FILE = "LICENSE.txt";
    public const string STEAM_LICENSE_FILE = "doc/steam_license_readme.txt";

    /// <summary>
    ///   Loads the Steam version specific readme by combining the special Steam file and the normal libraries list
    /// </summary>
    /// <returns>The license text</returns>
    public static string LoadSteamLicenseFile(bool insideGodot, Func<string, string> loadFile)
    {
        var pathPrefix = string.Empty;

        if (insideGodot)
        {
            pathPrefix = "res://";
        }

        var steam = loadFile(pathPrefix + STEAM_LICENSE_FILE);
        var normal = loadFile(pathPrefix + LICENSE_FILE);

        var regex = new Regex(".*(In addition to Godot Engine, Thrive uses the following.+)$",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        var match = regex.Match(normal);

        string extraText;
        if (match.Success)
        {
            extraText = match.Groups[1].Value;
        }
        else
        {
            throw new Exception("Failed to insert Steam version used library licenses list");
        }

        return steam + extraText;
    }

    public static string LoadNormalLicenseText(bool insideGodot, Func<string, string> loadFile)
    {
        var pathPrefix = string.Empty;

        if (insideGodot)
        {
            pathPrefix = "res://";
        }

        return loadFile(pathPrefix + LICENSE_FILE);
    }

    public static string LoadFileStandard(string path)
    {
        return File.ReadAllText(path);
    }
}
