namespace Scripts;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ScriptsBase.Utilities;
using SharedBase.Models;
using SharedBase.Utilities;

/// <summary>
///   Handles the native C++ modules needed by Thrive
/// </summary>
public class NativeLibs
{
    private const string LibraryFolder = "native_libs";
    private const string DistributableFolderName = "distributable";
    private const string GodotEditorLibraryFolder = ".mono/temp/bin/Debug";
    private const string GodotReleaseLibraryFolder = ".mono/assemblies/Release";

    private readonly Program.NativeLibOptions options;

    private readonly IList<PackagePlatform> platforms;

    public NativeLibs(Program.NativeLibOptions options)
    {
        this.options = options;

        if (options.Libraries is { Count: < 1 })
        {
            options.Libraries = null;
        }
        else if (options.Libraries != null)
        {
            ColourConsole.WriteNormalLine("Only processing following libraries:");

            foreach (var library in options.Libraries)
            {
                ColourConsole.WriteNormalLine(" > " + library);
            }
        }

        if (options.DebugLibrary)
        {
            ColourConsole.WriteNormalLine("Using debug versions of libraries (these are not available " +
                "for download usually)");
        }

        // Explicitly selected platforms override defaults
        if (this.options.Platforms is { Count: > 0 })
        {
            platforms = this.options.Platforms;
            return;
        }

        // Set sensible default platform definitions
        if (this.options.Operations.Any(o => o == Program.NativeLibOptions.OperationMode.Install))
        {
            // Install is just for current platform as it doesn't make sense to try to make the editor work on other
            // platforms on one computer
            platforms = new List<PackagePlatform> { PlatformUtilities.GetCurrentPlatform() };
        }
        else if (OperatingSystem.IsMacOS())
        {
            // Mac stuff only can be done on a mac
            platforms = new List<PackagePlatform> { PackagePlatform.Mac };
        }
        else if (this.options.Operations.Any(o => o == Program.NativeLibOptions.OperationMode.Build))
        {
            // Other platforms have cross compile but build defaults to just current platform
            platforms = new List<PackagePlatform> { PlatformUtilities.GetCurrentPlatform() };
        }
        else
        {
            platforms = new List<PackagePlatform> { PackagePlatform.Linux, PackagePlatform.Windows };
        }
    }

    public enum Library
    {
        /// <summary>
        ///   The main native side library that is pure C++ and doesn't depend on Godot
        /// </summary>
        ThriveNative,
    }

    public async Task<bool> Run(CancellationToken cancellationToken)
    {
        // Make sure this base folder exists
        Directory.CreateDirectory(LibraryFolder);

        await PackageTool.EnsureGodotIgnoreFileExistsInFolder(LibraryFolder);

        foreach (var operation in options.Operations)
        {
            if (!await RunOperation(operation, cancellationToken))
                return false;
        }

        return true;
    }

    /// <summary>
    ///   Copies required native library files to a Thrive release
    /// </summary>
    /// <param name="releaseFolder">The root of the release folder (with Thrive.pck and other files)</param>
    /// <param name="platform">The platform this release is for</param>
    /// <param name="useDistributableLibraries">
    ///   If true then only distributable libraries (with symbols extracted and stripped) are used. Otherwise normally
    ///   built local libraries can be used.
    /// </param>
    public bool CopyToThriveRelease(string releaseFolder, PackagePlatform platform, bool useDistributableLibraries)
    {
        var libraries = options.Libraries ?? Enum.GetValues<Library>();

        if (!Directory.Exists(releaseFolder))
        {
            ColourConsole.WriteErrorLine($"Release folder to install native library in doesn't exist: {releaseFolder}");
            ColourConsole.WriteErrorLine("Will not create / attempt to copy anyway as the release would likely " +
                "be broken due to file structure changing to what this script doesn't expect");

            return false;
        }

        // Godot doesn't by default put anything in the mono folder so we need to create it
        var targetFolder = Path.Join(releaseFolder, GodotReleaseLibraryFolder);
        Directory.CreateDirectory(targetFolder);

        foreach (var library in libraries)
        {
            ColourConsole.WriteDebugLine($"Copying native library: {library} to {targetFolder}");

            if (!CopyLibraryFiles(library, platform, useDistributableLibraries, targetFolder))
            {
                ColourConsole.WriteErrorLine($"Error copying library {library}");
                return false;
            }

            ColourConsole.WriteNormalLine($"Copied library {library}");
        }

        ColourConsole.WriteSuccessLine($"Native libraries for {platform} copied to {releaseFolder}");
        return true;
    }

    private string GetLibraryVersion(Library library)
    {
        switch (library)
        {
            case Library.ThriveNative:
                return NativeConstants.Version.ToString();
            default:
                throw new ArgumentOutOfRangeException(nameof(library), library, null);
        }
    }

    private string GetLibraryDllName(Library library, PackagePlatform platform)
    {
        switch (library)
        {
            case Library.ThriveNative:
                switch (platform)
                {
                    case PackagePlatform.Linux:
                        return "libthrive_native.so";
                    case PackagePlatform.Windows:
                        return "libthrive_native.dll";
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

    private async Task<bool> RunOperation(Program.NativeLibOptions.OperationMode operation,
        CancellationToken cancellationToken)
    {
        ColourConsole.WriteNormalLine($"Performing operation {operation}");

        switch (operation)
        {
            case Program.NativeLibOptions.OperationMode.Check:
                return await OperateOnAllLibraries(
                    (library, platform, token) => CheckLibrary(library, platform, false, token),
                    cancellationToken);
            case Program.NativeLibOptions.OperationMode.CheckDistributable:
                return await OperateOnAllLibraries(
                    (library, platform, token) => CheckLibrary(library, platform, true, token),
                    cancellationToken);

            case Program.NativeLibOptions.OperationMode.Install:
                return await OperateOnAllLibraries(InstallLibraryForEditor, cancellationToken);
            case Program.NativeLibOptions.OperationMode.Fetch:
                throw new NotImplementedException("TODO: implement downloading");
            case Program.NativeLibOptions.OperationMode.Build:
                return await OperateOnAllLibraries(BuildLocally, cancellationToken);
            case Program.NativeLibOptions.OperationMode.Package:
                throw new NotImplementedException("TODO: implement packaging");
            case Program.NativeLibOptions.OperationMode.Upload:
                throw new NotImplementedException("TODO: implement uploading (and package / symbol extract)");
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task<bool> OperateOnAllLibraries(
        Func<Library, PackagePlatform, CancellationToken, Task<bool>> operation,
        CancellationToken cancellationToken)
    {
        var libraries = options.Libraries ?? Enum.GetValues<Library>();

        foreach (var library in libraries)
        {
            ColourConsole.WriteDebugLine($"Operating on library: {library}");

            foreach (var platform in platforms)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ColourConsole.WriteDebugLine($"Operating on platform: {platform}");

                if (!await operation.Invoke(library, platform, cancellationToken))
                {
                    ColourConsole.WriteErrorLine($"Operation failed on: {library} for platform: {platform}");
                    return false;
                }
            }

            ColourConsole.WriteSuccessLine($"Successfully performed operation on library: {library}");
        }

        return true;
    }

    private Task<bool> CheckLibrary(Library library, PackagePlatform platform, bool distributableVersion,
        CancellationToken cancellationToken)
    {
        var file = GetPathToLibraryDll(library, platform, GetLibraryVersion(library), distributableVersion);

        if (File.Exists(file))
        {
            ColourConsole.WriteSuccessLine($"Library exists at: {file}");
            return Task.FromResult(true);
        }

        // TODO: more library files per library?
        cancellationToken.ThrowIfCancellationRequested();

        ColourConsole.WriteErrorLine($"Library does not exist: {file}");
        return Task.FromResult(false);
    }

    private Task<bool> InstallLibraryForEditor(Library library, PackagePlatform platform,
        CancellationToken cancellationToken)
    {
        var target = GodotEditorLibraryFolder;

        if (!Directory.Exists(target))
        {
            ColourConsole.WriteWarningLine($"Target folder to install native library in doesn't exist: {target}");
            ColourConsole.WriteInfoLine(
                "Trying to install anyway but the install location might be wrong and Godot might not " +
                "find the library");

            Directory.CreateDirectory(target);
        }

        var linkTo = GetPathToLibraryDll(library, platform, GetLibraryVersion(library), false);
        var originalLinkTo = linkTo;

        if (!File.Exists(linkTo))
        {
            // Fall back to distributable version
            linkTo = GetPathToLibraryDll(library, platform, GetLibraryVersion(library), true);

            if (!File.Exists(linkTo))
            {
                ColourConsole.WriteErrorLine(
                    $"Expected library doesn't exist (please 'Fetch' or 'Build' first): {originalLinkTo}");
                ColourConsole.WriteNormalLine("Distributable version also didn't exist");
                return Task.FromResult(false);
            }
        }

        var linkFile = Path.Join(target, GetLibraryDllName(library, platform));

        CreateLinkTo(linkFile, linkTo);

        if (platform != PlatformUtilities.GetCurrentPlatform())
        {
            ColourConsole.WriteWarningLine(
                "Linking non-current platform library, this is likely not what's desired for the Godot Editor");
        }

        ColourConsole.WriteSuccessLine($"Successfully linked {library} on {platform}");
        return Task.FromResult(true);
    }

    private bool CopyLibraryFiles(Library library, PackagePlatform platform, bool useDistributableLibraries,
        string target)
    {
        // TODO: other files?
        var file = GetPathToLibraryDll(library, platform, GetLibraryVersion(library), useDistributableLibraries);

        if (!File.Exists(file))
        {
            ColourConsole.WriteErrorLine($"Expected file doesn't exist at: {file}");
            ColourConsole.WriteNormalLine("Have the native libraries been built / downloaded?");
            return false;
        }

        var targetFile = Path.Join(target, Path.GetFileName(file));

        File.Copy(file, targetFile, true);

        ColourConsole.WriteNormalLine($"Copied {file} -> {targetFile} for {platform}");

        return true;
    }

    private async Task<bool> BuildLocally(Library library, PackagePlatform platform,
        CancellationToken cancellationToken)
    {
        if (platform != PlatformUtilities.GetCurrentPlatform())
        {
            ColourConsole.WriteErrorLine("Building for non-current platform without podman is not supported");
            return false;
        }

        ColourConsole.WriteInfoLine(
            $"Building {library} for local use ({platform}) with CMake (hopefully all native dependencies " +
            "are installed)");

        var buildFolder = GetNativeBuildFolder();

        // TODO: flag for clean builds
        Directory.CreateDirectory(buildFolder);

        // Ensure Godot doesn't try to import anything funny from the build folder
        await PackageTool.EnsureGodotIgnoreFileExistsInFolder(buildFolder);

        var installPath = Path.GetFullPath(GetLocalCMakeInstallTarget(platform, GetLibraryVersion(library)));

        var startInfo = new ProcessStartInfo("cmake")
        {
            WorkingDirectory = buildFolder,
        };

        // ReSharper disable StringLiteralTypo
        startInfo.ArgumentList.Add("-DCMAKE_EXPORT_COMPILE_COMMANDS=ON");

        if (options.DebugLibrary)
        {
            startInfo.ArgumentList.Add("-DCMAKE_BUILD_TYPE=Debug");
        }
        else
        {
            startInfo.ArgumentList.Add("-DCMAKE_BUILD_TYPE=RelWithDebInfo");
        }

        if (!string.IsNullOrEmpty(options.Compiler) || !string.IsNullOrEmpty(options.CCompiler))
        {
            ColourConsole.WriteNormalLine(
                $"Using custom specified compiler: CXX: {options.Compiler}, C: {options.CCompiler}");

            if (!string.IsNullOrEmpty(options.Compiler))
            {
                startInfo.ArgumentList.Add($"-DCMAKE_CXX_COMPILER={options.Compiler}");
            }

            if (!string.IsNullOrEmpty(options.CCompiler))
            {
                startInfo.ArgumentList.Add($"-DCMAKE_C_COMPILER={options.CCompiler}");
            }
        }
        else
        {
            if (OperatingSystem.IsLinux())
            {
                // Use clang by default
                startInfo.ArgumentList.Add("-DCMAKE_CXX_COMPILER=clang++");
                startInfo.ArgumentList.Add("-DCMAKE_C_COMPILER=clang");
            }
        }

        if (!string.IsNullOrEmpty(options.CmakeGenerator))
        {
            startInfo.ArgumentList.Add("-G");
            startInfo.ArgumentList.Add(options.CmakeGenerator);
        }

        // TODO: add support for non-visual studio builds on windows
        // When not using visual studio using ninja would be needed to avoid a dependency on it
        // if (string.IsNullOrEmpty(ExecutableFinder.Which("ninja")))
        // {
        //     ColourConsole.WriteErrorLine(
        //         "Could not find ninja executable, generating probably will fail. Please install it " +
        //         "and add to PATH or specify another generator system");
        // }
        //
        // startInfo.ArgumentList.Add("-G");
        // startInfo.ArgumentList.Add("Ninja");

        startInfo.ArgumentList.Add($"-DCMAKE_INSTALL_PREFIX={installPath}");

        startInfo.ArgumentList.Add("../");

        // ReSharper restore StringLiteralTypo
        var result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, false);

        if (result.ExitCode != 0)
        {
            ColourConsole.WriteErrorLine(
                $"CMake configuration failed (exit: {result.ExitCode}). " +
                "Do you have the required build tools installed?");

            return false;
        }

        ColourConsole.WriteInfoLine("Succeeded in configuring cmake project");

        ColourConsole.WriteNormalLine("Compiling...");

        startInfo = new ProcessStartInfo("cmake")
        {
            WorkingDirectory = buildFolder,
        };

        startInfo.ArgumentList.Add("--build");
        startInfo.ArgumentList.Add(".");
        startInfo.ArgumentList.Add("--config");

        if (OperatingSystem.IsWindows())
        {
            ColourConsole.WriteWarningLine("TODO: Windows Jolt build with MSVC only supports Release mode, " +
                "building Thrive in release mode as well, there won't be debug symbols");

            startInfo.ArgumentList.Add(options.DebugLibrary ? "Debug" : "Release");
        }
        else
        {
            startInfo.ArgumentList.Add(options.DebugLibrary ? "Debug" : "RelWithDebInfo");
        }

        startInfo.ArgumentList.Add("--target");
        startInfo.ArgumentList.Add("install");

        startInfo.ArgumentList.Add("-j");

        // TODO: add option to not use all cores
        startInfo.ArgumentList.Add(Environment.ProcessorCount.ToString());

        result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, false);

        if (result.ExitCode != 0)
        {
            ColourConsole.WriteErrorLine($"Failed to run compiler through CMake (exit: {result.ExitCode}). " +
                "See above for build output");

            return false;
        }

        if (!Directory.Exists(installPath))
        {
            ColourConsole.WriteErrorLine($"Expected compile target folder doesn't exist: {installPath}");
            return false;
        }

        ColourConsole.WriteSuccessLine($"Successfully compiled library {library}");
        return true;
    }

    /// <summary>
    ///   Path to the library's root where all version specific folders are added
    /// </summary>
    private string GetPathToLibrary(Library library, PackagePlatform platform, string version,
        bool distributableVersion)
    {
        if (distributableVersion)
        {
            return Path.Combine(LibraryFolder, DistributableFolderName, platform.ToString().ToLowerInvariant(),
                library.ToString(), version, options.DebugLibrary ? "debug" : "release");
        }

        // TODO: should the paths for the libraries include the library name? (cmake is used to compile all at once)

        // The paths are a bit convoluted to easily be able to install with cmake to the target
        return Path.Combine(LibraryFolder, platform.ToString().ToLowerInvariant(), version,
            options.DebugLibrary ? "debug" : "release");
    }

    private string GetLocalCMakeInstallTarget(PackagePlatform platform, string version)
    {
        return Path.Combine(LibraryFolder, platform.ToString().ToLowerInvariant(), version);
    }

    private string GetPathToLibraryDll(Library library, PackagePlatform platform, string version,
        bool distributableVersion)
    {
        var basePath = GetPathToLibrary(library, platform, version, distributableVersion);

        if (platform is PackagePlatform.Windows or PackagePlatform.Windows32)
        {
            return Path.Combine(basePath, "bin", GetLibraryDllName(library, platform));
        }

        // This is for Linux
        return Path.Combine(basePath, "lib", GetLibraryDllName(library, platform));
    }

    private void CreateLinkTo(string linkFile, string linkTo)
    {
        if (File.Exists(linkFile))
        {
            ColourConsole.WriteDebugLine($"Removing existing library file: {linkFile}");
            File.Delete(linkFile);
        }

        if (!OperatingSystem.IsWindows() || options.UseSymlinks)
        {
            File.CreateSymbolicLink(linkFile, Path.GetFullPath(linkTo));
        }
        else
        {
            bool fallback = true;

            if (OperatingSystem.IsWindows())
            {
                ColourConsole.WriteWarningLine("Not using symbolic link to install for editor. Any newly " +
                    "compiled library versions may not be visible to the editor without re-running " +
                    "this install command");
                ColourConsole.WriteNormalLine("Symbolic links can be specifically enabled but require " +
                    "administrator privileges on Windows");

                try
                {
                    if (NativeMethods.CreateHardLink(linkFile, Path.GetFullPath(linkTo), IntPtr.Zero))
                    {
                        fallback = false;
                    }
                }
                catch (Exception e)
                {
                    ColourConsole.WriteWarningLine($"Failed to call hardlink creation: {e}");
                }
            }

            if (fallback)
            {
                ColourConsole.WriteWarningLine("Copying library instead of linking. The library won't update " +
                    "without re-running this tool!");
                File.Copy(linkTo, linkFile);
            }
        }
    }

    private string GetNativeBuildFolder()
    {
        if (options.DebugLibrary)
            return "build-debug";

        return "build";
    }

    private static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool CreateHardLink(string lpFileName, string lpExistingFileName,
            IntPtr lpSecurityAttributes);
    }
}
