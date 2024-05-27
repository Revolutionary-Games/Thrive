using System;
using System.IO;
using DevCenterCommunication.Models.Enums;
using SharedBase.Models;

/// <summary>
///   Shared constants between native helper scripts and Thrive (as well as some helper methods to getting info
///   related to the libraries)
/// </summary>
public class NativeConstants
{
    public const int Version = 15;
    public const int EarlyCheck = 2;

    public const string LibraryFolder = "native_libs";
    public const string DistributableFolderName = "distributable";

    public const string PackagedLibraryFolder = "lib";

    public enum Library
    {
        /// <summary>
        ///   The main native side library that is pure C++ and doesn't depend on Godot
        /// </summary>
        ThriveNative,

        /// <summary>
        ///   Library for early checking that everything is fine before loading <see cref="ThriveNative"/>
        /// </summary>
        EarlyCheck,
    }

    public static string GetLibraryVersion(Library library)
    {
        switch (library)
        {
            case Library.ThriveNative:
                return Version.ToString();
            case Library.EarlyCheck:
                return EarlyCheck.ToString();
            default:
                throw new ArgumentOutOfRangeException(nameof(library), library, null);
        }
    }

    public static bool GetLibraryFromName(string name, out Library library)
    {
        if (name == "thrive_native")
        {
            library = Library.ThriveNative;
            return true;
        }

        if (name == "early_checks")
        {
            library = Library.EarlyCheck;
            return true;
        }

        // For handling simplicity the enum doesn't have an invalid value so we can't use one here, but instead need to
        // rely on all of our callers to check the return value
        library = Library.ThriveNative;
        return false;
    }

    public static string GetLibraryDllName(Library library, PackagePlatform platform, PrecompiledTag tags)
    {
        switch (library)
        {
            case Library.ThriveNative:
                switch (platform)
                {
                    case PackagePlatform.Linux:
                        if ((tags & PrecompiledTag.WithoutAvx) != 0)
                            return "libthrive_native_without_avx.so";

                        return "libthrive_native.so";
                    case PackagePlatform.Windows:
                        if ((tags & PrecompiledTag.WithoutAvx) != 0)
                            return "libthrive_native_without_avx.dll";

                        return "libthrive_native.dll";
                    case PackagePlatform.Windows32:
                        throw new NotSupportedException("32-bit support is not done currently");
                    case PackagePlatform.Mac:
                        throw new NotImplementedException("TODO: name for this");
                    default:
                        throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
                }

            case Library.EarlyCheck:
                switch (platform)
                {
                    case PackagePlatform.Linux:
                        return "libearly_checks.so";
                    case PackagePlatform.Windows:
                        return "libearly_checks.dll";
                    case PackagePlatform.Windows32:
                        throw new NotSupportedException("32-bit support is not done currently");
                    case PackagePlatform.Mac:
                        throw new NotImplementedException("TODO: name for this");
                    default:
                        throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(library), library, null);
        }
    }

    public static string GetPathToLibraryDll(Library library, PackagePlatform platform, string version,
        bool distributableVersion, PrecompiledTag tags)
    {
        var basePath = GetPathToLibrary(library, platform, version, distributableVersion, tags);

        if (platform is PackagePlatform.Windows or PackagePlatform.Windows32)
        {
            return Path.Combine(basePath, "bin", GetLibraryDllName(library, platform, tags));
        }

        // This is for Linux
        return Path.Combine(basePath, "lib", GetLibraryDllName(library, platform, tags));
    }

    /// <summary>
    ///   Path to the library's root where all version specific folders are added
    /// </summary>
    private static string GetPathToLibrary(Library library, PackagePlatform platform, string version,
        bool distributableVersion, PrecompiledTag tags)
    {
        if (distributableVersion)
        {
            return Path.Combine(LibraryFolder, DistributableFolderName, platform.ToString().ToLowerInvariant(),
                library.ToString(), version, (tags & PrecompiledTag.Debug) != 0 ? "debug" : "release");
        }

        // TODO: should the paths for the libraries include the library name? (cmake is used to compile all at once,
        // which makes this a bit difficult)

        // The paths are a bit convoluted to easily be able to install with cmake to the target
        return Path.Combine(LibraryFolder, platform.ToString().ToLowerInvariant(), version,
            (tags & PrecompiledTag.Debug) != 0 ? "debug" : "release");
    }
}
