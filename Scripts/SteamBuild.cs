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
    public const string THRIVE_CSPROJ = "Thrive.csproj";

    private const string IGNORE_STEAM_CLIENT_MARKER = @"<Compile Remove=""src\steam\SteamClient.cs""";
    private const string STEAM_CLIENT_INSERT_ENABLE = "Steam build needs to";
    private const string DISABLE_STEAM_LINE = "    " + IGNORE_STEAM_CLIENT_MARKER + " />";
    private const string ITEM_GROUP = "<ItemGroup>";

    private const string STEAM_ENABLED_COMMENT = "<!-- Steam build enabled -->";

    private const string STEAMWORKS_REFERENCE_START = @"<Reference Include=""Steamworks.NET";

    private const string CONDITION_MAC_ARM64 =
        @"Condition=""'$(RuntimeIdentifier)' == 'osx-arm64' Or ('$(RuntimeIdentifier)' == '' And " +
        "$([MSBuild]::IsOSPlatform('OSX')) And " +
        @"'$([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture)' == 'Arm64')""";

    private const string CONDITION_MAC_X64 =
        @"Condition=""'$(RuntimeIdentifier)' == 'osx-x64' Or ('$(RuntimeIdentifier)' == '' And " +
        "$([MSBuild]::IsOSPlatform('OSX')) And " +
        @"'$([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture)' == 'X64')""";

    public enum SteamPlatform
    {
        Linux,
        Windows,
        Mac,
    }

    public enum LibraryArchitecture
    {
        X86,
        X64,
        Arm,
        Arm64,
    }

    public static string PathToSteamAssemblyForPlatform(SteamPlatform platform, LibraryArchitecture architecture)
    {
        switch (platform)
        {
            case SteamPlatform.Linux:
                return $@"third_party\linux\{SteamAssemblyNameForPlatform(platform, architecture)}";
            case SteamPlatform.Windows:
                return $@"third_party\windows\{SteamAssemblyNameForPlatform(platform, architecture)}";

            case SteamPlatform.Mac:
                // Can't use LIPO on a DLL, so this needs to be split like this
                switch (architecture)
                {
                    case LibraryArchitecture.X86:
                    case LibraryArchitecture.Arm:
                        throw new NotSupportedException("Non-supported architecture for Mac");

                    case LibraryArchitecture.X64:
                        return $@"third_party\mac\{SteamAssemblyNameForPlatform(platform, architecture)}";
                    case LibraryArchitecture.Arm64:
                        return $@"third_party\mac\{SteamAssemblyNameForPlatform(platform, architecture)}";

                    default:
                        throw new ArgumentOutOfRangeException(nameof(architecture), architecture, null);
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
        }
    }

    public static string SteamAssemblyNameForPlatform(SteamPlatform platform, LibraryArchitecture architecture)
    {
        if (platform == SteamPlatform.Mac)
        {
            switch (architecture)
            {
                case LibraryArchitecture.X86:
                case LibraryArchitecture.Arm:
                    throw new NotSupportedException("Non-supported architecture for Mac");
                case LibraryArchitecture.X64:
                    return "Steamworks.NET.x64.dll";
                case LibraryArchitecture.Arm64:
                    return "Steamworks.NET.arm64.dll";
                default:
                    throw new ArgumentOutOfRangeException(nameof(architecture), architecture, null);
            }
        }

        return "Steamworks.NET.dll";
    }

    public static string SteamAssemblyReference(SteamPlatform platform)
    {
        if (platform == SteamPlatform.Mac)
        {
            // Mac requires architecture specific references

            return
                $"{STEAMWORKS_REFERENCE_START}.arm64\" {CONDITION_MAC_ARM64}><HintPath>{PathToSteamAssemblyForPlatform(platform, LibraryArchitecture.Arm64)}</HintPath></Reference>\n" +
                $"    {STEAMWORKS_REFERENCE_START}.x64\" {CONDITION_MAC_X64}><HintPath>{PathToSteamAssemblyForPlatform(platform, LibraryArchitecture.X64)}</HintPath></Reference>";
        }

        // Other platforms don't use architecture-specific references
        return
            $"{STEAMWORKS_REFERENCE_START}\"><HintPath>{PathToSteamAssemblyForPlatform(platform, LibraryArchitecture.X64)}</HintPath></Reference>";
    }

    public static async Task<bool> IsSteamBuildEnabled(CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(THRIVE_CSPROJ, Encoding.UTF8, cancellationToken);

        return !content.Contains(IGNORE_STEAM_CLIENT_MARKER);
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
            else if (OperatingSystem.IsMacOS())
            {
                platform = SteamPlatform.Mac;
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
                    // We have a partially enabled build, so we need to disable and re-enable
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
            content = ProcessEnablingSteam(platform.Value, content, verbose);
        }
        else
        {
            content = ProcessDisablingSteam(content, verbose);
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
                return SteamPlatform.Mac;
            case PackagePlatform.Web:
                throw new Exception("Web platform can't have Steam");
            default:
                throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
        }
    }

    private static IEnumerable<string> ProcessEnablingSteam(SteamPlatform platform, IEnumerable<string> lines,
        bool verbose)
    {
        bool removed = false;

        bool addedSteamworks = false;

        int lineNumber = 0;

        foreach (var line in lines)
        {
            ++lineNumber;

            if (removed && addedSteamworks)
            {
                yield return line;

                continue;
            }

            if (line.Contains(IGNORE_STEAM_CLIENT_MARKER))
            {
                if (verbose)
                    ColourConsole.WriteInfoLine($"Removed Steam client suppress reference on line {lineNumber}");

                // Replace it with the assembly reference
                yield return $"    {STEAM_ENABLED_COMMENT}";

                if (verbose)
                    ColourConsole.WriteInfoLine("Adding steamworks reference");

                yield return $"    {SteamAssemblyReference(platform)}";

                addedSteamworks = true;
                removed = true;

                continue;
            }

            yield return line;
        }

        if (!removed)
            throw new Exception("Could not remove Steam handler ignore compile line");

        if (!addedSteamworks)
            throw new Exception("Could not add Steamworks reference");
    }

    private static IEnumerable<string> ProcessDisablingSteam(IEnumerable<string> lines, bool verbose)
    {
        int lineNumber = 0;
        bool addedClientIgnore = false;
        bool clientIgnorePrimed = false;

        foreach (var line in lines)
        {
            ++lineNumber;

            if (!addedClientIgnore)
            {
                if (line.Contains(STEAM_CLIENT_INSERT_ENABLE))
                    clientIgnorePrimed = true;
            }

            if (line.Contains(STEAMWORKS_REFERENCE_START))
            {
                if (verbose)
                    ColourConsole.WriteInfoLine($"Removed Steamworks assembly reference on line {lineNumber}");

                continue;
            }

            if (line.Contains(STEAM_ENABLED_COMMENT))
            {
                if (verbose)
                    ColourConsole.WriteInfoLine($"Removing Steam enabled comment on line {lineNumber}");

                continue;
            }

            yield return line;

            if (clientIgnorePrimed && line.Contains(ITEM_GROUP))
            {
                if (verbose)
                    ColourConsole.WriteInfoLine($"Adding Steam client suppress reference after line {lineNumber}");

                yield return DISABLE_STEAM_LINE;

                clientIgnorePrimed = false;
                addedClientIgnore = true;
            }
        }
    }
}
