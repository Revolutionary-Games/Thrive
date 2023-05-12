namespace Scripts;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Utilities;
using SharedBase.Models;

public static class SteamBuild
{
    private const string STEAM_CLIENT_FILE_PATH = @"src\steam\SteamClient.cs";
    private const string STEAMWORKS_REFERENCE_START = @"<Reference Include=""Steamworks.NET"">";

    private const string CSPROJ_COMPILE_LINE = "<Compile Include=";
    private const string CSPROJ_SYSTEM_REFERENCE = @"<Reference Include=""System""";

    private const string THRIVE_CSPROJ = "Thrive.csproj";

    public enum SteamPlatform
    {
        Linux,
        Windows,
    }

    public static string PathToSteamAssemblyForPlatform(SteamPlatform platform)
    {
        switch (platform)
        {
            case SteamPlatform.Linux:
                return @"third_party\linux\Steamworks.NET.dll";
            case SteamPlatform.Windows:
                return @"third_party\windows\Steamworks.NET.dll";
            default:
                throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
        }
    }

    public static string SteamAssemblyReference(SteamPlatform platform)
    {
        return
            $"{STEAMWORKS_REFERENCE_START}<HintPath>{PathToSteamAssemblyForPlatform(platform)}</HintPath></Reference>";
    }

    public static async Task<bool> IsSteamBuildEnabled(CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(THRIVE_CSPROJ, Encoding.UTF8, cancellationToken);

        return content.Contains(STEAM_CLIENT_FILE_PATH);
    }

    public static async Task<bool> IsSteamworksReferenced(SteamPlatform platform, CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(THRIVE_CSPROJ, Encoding.UTF8, cancellationToken);

        return content.Contains(SteamAssemblyReference(platform));
    }

    public static async Task<bool> SetBuildMode(bool enabled, bool verbose, CancellationToken cancellationToken,
        SteamPlatform? platform = null)
    {
        if (platform == null)
        {
            if (OperatingSystem.IsWindows())
            {
                platform = SteamPlatform.Windows;
            }
            else if (OperatingSystem.IsLinux())
            {
                platform = SteamPlatform.Linux;
            }
            else
            {
                throw new Exception("Unknown platform for auto picking Steam platform");
            }
        }

        var alreadyEnabled = await IsSteamBuildEnabled(cancellationToken);

        if (enabled == alreadyEnabled)
        {
            if (enabled)
            {
                if (!await IsSteamworksReferenced(platform.Value, cancellationToken))
                {
                    // We have a partially enabled build so we need to disable and re-enable
                    if (!await SetBuildMode(false, verbose, cancellationToken, platform))
                        throw new Exception("Failed to disable Steam build in order to put it back on");

                    return await SetBuildMode(true, verbose, cancellationToken, platform);
                }
            }

            return true;
        }

        var tempFile = $"{THRIVE_CSPROJ}.tmp";

        IEnumerable<string> content = await File.ReadAllLinesAsync(THRIVE_CSPROJ, Encoding.UTF8, cancellationToken);

        if (enabled)
        {
            content = ProcessAddingClientLine(platform.Value, content, verbose);
        }
        else
        {
            content = ProcessRemovingClientLine(content, verbose);
        }

        // Important to not emit the BOM here
        await File.WriteAllLinesAsync(tempFile, content, new UTF8Encoding(false), cancellationToken);

        File.Move(tempFile, THRIVE_CSPROJ, true);

        return true;
    }

    public static SteamPlatform ConvertPackagePlatformToSteam(PackagePlatform platform)
    {
        switch (platform)
        {
            case PackagePlatform.Linux:
                return SteamPlatform.Linux;
            case PackagePlatform.Windows32:
            case PackagePlatform.Windows:
                return SteamPlatform.Windows;
            case PackagePlatform.Mac:
                throw new Exception("Steam not implemented for mac yet");
            case PackagePlatform.Web:
                throw new Exception("Web platform can't have Steam");
            default:
                throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
        }
    }

    private static IEnumerable<string> ProcessAddingClientLine(SteamPlatform platform, IEnumerable<string> lines,
        bool verbose)
    {
        bool added = false;
        bool foundCompile = false;

        bool addedSteamworks = false;

        int lineNumber = 0;

        foreach (var line in lines)
        {
            ++lineNumber;

            if (added && addedSteamworks)
            {
                yield return line;

                continue;
            }

            if (foundCompile && !line.Contains(CSPROJ_COMPILE_LINE) && !added)
            {
                if (verbose)
                    ColourConsole.WriteInfoLine($"Inserting special file file after line {lineNumber}");

                yield return $"    <Compile Include=\"{STEAM_CLIENT_FILE_PATH}\" />";

                added = true;
            }
            else if (line.Contains(CSPROJ_COMPILE_LINE) && !foundCompile)
            {
                if (verbose)
                    ColourConsole.WriteNormalLine($"Found first compile file reference on line {lineNumber}");
                foundCompile = true;
            }

            if (!addedSteamworks && line.Contains(CSPROJ_SYSTEM_REFERENCE))
            {
                if (verbose)
                {
                    ColourConsole.WriteInfoLine(
                        $"Found system reference on line {lineNumber} adding steamworks reference");
                }

                yield return $"    {SteamAssemblyReference(platform)}";

                addedSteamworks = true;
            }

            yield return line;
        }

        if (!added)
            throw new Exception("Could not add Steam handler compile directive");

        if (!addedSteamworks)
            throw new Exception("Could not add Steamworks reference");
    }

    private static IEnumerable<string> ProcessRemovingClientLine(IEnumerable<string> lines, bool verbose)
    {
        int lineNumber = 0;

        foreach (var line in lines)
        {
            ++lineNumber;

            if (line.Contains(STEAM_CLIENT_FILE_PATH))
            {
                if (verbose)
                    ColourConsole.WriteInfoLine($"Removed Steam client compile reference on line {lineNumber}");

                continue;
            }

            if (line.Contains(STEAMWORKS_REFERENCE_START))
            {
                if (verbose)
                    ColourConsole.WriteInfoLine($"Removed Steamworks assembly reference on line {lineNumber}");

                continue;
            }

            yield return line;
        }
    }
}
