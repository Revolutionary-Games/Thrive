namespace Scripts;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevCenterCommunication.Models;
using DevCenterCommunication.Models.Enums;
using ScriptsBase.Utilities;
using SharedBase.Models;
using SharedBase.Utilities;
using ThriveDevCenter.Shared.Forms;

/// <summary>
///   Handles the native C++ modules needed by Thrive
/// </summary>
public class NativeLibs
{
    private const string LibraryFolder = "native_libs";
    private const string DistributableFolderName = "distributable";
    private const string GodotEditorLibraryFolder = ".mono/temp/bin/Debug";
    private const string GodotReleaseLibraryFolder = ".mono/assemblies/Release";

    private const string BuilderImageName = "localhost/thrive/native-builder:latest";
    private const string BuilderImageNameCross = "localhost/thrive/native-builder-cross:latest";
    private const string FolderToWriteDistributableBuildIn = "build/distributable_build";

    private readonly Program.NativeLibOptions options;

    private readonly IList<PackagePlatform> platforms;

    private readonly SymbolHandler symbolHandler = new(null, null);

    private readonly Lazy<Task<long>> thriveNativePrecompiledId;

    public NativeLibs(Program.NativeLibOptions options)
    {
        this.options = options;
        thriveNativePrecompiledId = new Lazy<Task<long>>(GetThriveNativeLibraryId);

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
            ColourConsole.WriteNormalLine("Using debug versions of libraries (these are not always " +
                "available for download)");
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
                return await OperateOnAllLibraries(DownloadLibraryIfMissing, cancellationToken);
            case Program.NativeLibOptions.OperationMode.Build:
                return await OperateOnAllLibraries(BuildLocally, cancellationToken);
            case Program.NativeLibOptions.OperationMode.Package:
                if (!OperatingSystem.IsMacOS())
                {
                    ColourConsole.WriteInfoLine("Making distributable package and symbols for Linux and Windows");

                    return await OperateOnAllPlatforms(BuildPackageWithPodman, cancellationToken);
                }

                throw new NotImplementedException("Creating release packages for mac is not done");

            case Program.NativeLibOptions.OperationMode.Upload:
                if (await OperateOnAllLibrariesWithResult(CheckAndUpload, cancellationToken))
                {
                    ColourConsole.WriteNormalLine("Checking for potential symbols to upload after library upload");
                    return await UploadMissingSymbolsToServer(cancellationToken);
                }

                // Check and upload step failed / or didn't upload anything
                return false;

            case Program.NativeLibOptions.OperationMode.Symbols:
                ColourConsole.WriteNormalLine("Checking for any symbols missing from the server");
                return await UploadMissingSymbolsToServer(cancellationToken);

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

            if (!await OperateOnAllPlatforms(library, operation, cancellationToken))
            {
                ColourConsole.WriteErrorLine($"Operation failed for {library}");
                return false;
            }

            ColourConsole.WriteSuccessLine($"Successfully performed operation on library: {library}");
        }

        return true;
    }

    /// <summary>
    ///   Variant that always operates on all libraries and returns a result value
    /// </summary>
    /// <param name="operation">Operation to perform</param>
    /// <param name="cancellationToken">Cancellation for the operation</param>
    /// <returns>The result value</returns>
    private async Task<T> OperateOnAllLibrariesWithResult<T>(
        Func<Library, PackagePlatform, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var libraries = options.Libraries ?? Enum.GetValues<Library>();

        bool resultSet = false;
        T result = default!;

        foreach (var library in libraries)
        {
            ColourConsole.WriteDebugLine($"Operating on library: {library}");

            var currentResult = await OperateOnAllPlatformsWithResult(library, operation, cancellationToken);
            if (!resultSet)
            {
                result = currentResult;
                resultSet = true;
            }
            else if (currentResult is false)
            {
                // TODO: not the cleanest way to specially overload this for false like this
                result = currentResult;
            }
        }

        return result;
    }

    private async Task<bool> OperateOnAllPlatforms(Library library,
        Func<Library, PackagePlatform, CancellationToken, Task<bool>> operation, CancellationToken cancellationToken)
    {
        foreach (var platform in platforms)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ColourConsole.WriteDebugLine($"Operating on platform: {platform}");

            if (!await operation.Invoke(library, platform, cancellationToken))
            {
                ColourConsole.WriteErrorLine($"Operation failed on library: {library} for platform: {platform}");
                return false;
            }
        }

        return true;
    }

    private async Task<T> OperateOnAllPlatformsWithResult<T>(Library library,
        Func<Library, PackagePlatform, CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        bool resultSet = false;
        T result = default!;

        foreach (var platform in platforms)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ColourConsole.WriteDebugLine($"Operating on platform: {platform}");

            var currentResult = await operation.Invoke(library, platform, cancellationToken);
            if (!resultSet)
            {
                result = currentResult;
                resultSet = true;
            }
            else if (currentResult is false)
            {
                // TODO: not the cleanest way to specially overload this for false like this
                result = currentResult;
            }
        }

        return result;
    }

    private async Task<bool> OperateOnAllPlatforms(Func<PackagePlatform, CancellationToken, Task<bool>> operation,
        CancellationToken cancellationToken)
    {
        foreach (var platform in platforms)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ColourConsole.WriteDebugLine($"Operating on platform: {platform}");

            if (!await operation.Invoke(platform, cancellationToken))
            {
                ColourConsole.WriteErrorLine($"Operation failed for platform: {platform}");
                return false;
            }
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

        ColourConsole.WriteDebugLine("Trying to install locally compiled version first");
        var linkTo = GetPathToLibraryDll(library, platform, GetLibraryVersion(library), false);
        var originalLinkTo = linkTo;

        if (!File.Exists(linkTo))
        {
            // Fall back to distributable version
            ColourConsole.WriteNormalLine("Falling back to attempting distributable version");
            linkTo = GetPathToLibraryDll(library, platform, GetLibraryVersion(library), true);

            if (!File.Exists(linkTo))
            {
                ColourConsole.WriteErrorLine(
                    $"Expected library doesn't exist (please 'Fetch' or 'Build' first): {originalLinkTo}");
                ColourConsole.WriteNormalLine("Distributable version also didn't exist");
                return Task.FromResult(false);
            }

            ColourConsole.WriteSuccessLine("Distributable version of library detected");
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

        // Give a nicer error message with missing cmake
        if (string.IsNullOrEmpty(ExecutableFinder.Which("cmake")))
        {
            ColourConsole.WriteErrorLine("cmake not found. CMake is required for this build to work. " +
                "Make sure it is installed and added to PATH before trying again.");
            return false;
        }

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

    private async Task<bool> BuildPackageWithPodman(PackagePlatform platform, CancellationToken cancellationToken)
    {
        var image = platform is PackagePlatform.Windows or PackagePlatform.Windows32 ?
            BuilderImageNameCross :
            BuilderImageName;

        ColourConsole.WriteNormalLine($"Using builder image: {image} hopefully it has been built");

        var compileInstallFolder = GetDistributableBuildFolderBase(platform);

        Directory.CreateDirectory(compileInstallFolder);

        var startInfo = new ProcessStartInfo("podman");

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--rm");
        startInfo.ArgumentList.Add("-t");

        startInfo.ArgumentList.Add($"--volume={Path.GetFullPath(".")}:/thrive:ro,z");
        startInfo.ArgumentList.Add(
            $"--volume={Path.GetFullPath(compileInstallFolder)}:/install-target:rw,z");

        if (options.Verbose)
        {
            startInfo.ArgumentList.Add("-e=VERBOSE=1");
        }

        startInfo.ArgumentList.Add(image);

        startInfo.ArgumentList.Add("sh");
        startInfo.ArgumentList.Add("-c");

        var shCommandBuilder = new StringBuilder();

        shCommandBuilder.Append("mkdir /build && cd build && ");

        // Cmake configure inside the container
        shCommandBuilder.Append("cmake /thrive ");
        shCommandBuilder.Append("-G Ninja ");

        // ReSharper disable StringLiteralTypo
        var buildType = "Distribution";

        if (options.DebugLibrary)
        {
            ColourConsole.WriteDebugLine("Creating a debug version of the distributable");
            buildType = "Debug";
        }

        shCommandBuilder.Append($"-DCMAKE_BUILD_TYPE={buildType} ");

        // We explicitly enable LTO with compiler flags when we want as CMake when testing LTO seems to ignore a bunch
        // of flags
        // TODO: figure out how to get the Jolt interprocedural check to work (now fails with trying to link the wrong
        // standard library)
        shCommandBuilder.Append("-DINTERPROCEDURAL_OPTIMIZATION=OFF ");

        shCommandBuilder.Append("-DCMAKE_INSTALL_PREFIX=/install-target ");

        switch (platform)
        {
            case PackagePlatform.Linux:
            {
                // Linux is all setup by default to use clang
                // But we need to ensure the runtime is set correctly (the default podman image should have this but
                // better be safe here)
                // var target = "x86_64-unknown-linux-llvm";

                var target = "x86_64-unknown-linux-gnu";

                shCommandBuilder.Append("-DCMAKE_CXX_COMPILER=clang++ ");
                shCommandBuilder.Append("-DCMAKE_C_COMPILER=clang ");

                // ReSharper disable once CommentTypo
                // -flto=thin specified here reduces the binary size a bit, not sure what's up with that other than
                // maybe the cmake default LTO is slightly more conservative option
                shCommandBuilder.Append(
                    $"-DCMAKE_C_FLAGS='-target {target} --rtlib=compiler-rt' ");
                shCommandBuilder.Append(
                    $"-DCMAKE_CXX_FLAGS='-target {target} -stdlib=libc++' ");

                shCommandBuilder.Append("-DCMAKE_EXE_LINKER_FLAGS='-L/usr/lib64/x86_64-unknown-linux-gnu' ");

                // Need to specify the standard library like this to prevent linker errors
                shCommandBuilder.Append("-DCMAKE_SHARED_LINKER_FLAGS='-L/usr/lib64/x86_64-unknown-linux-gnu  ");

                // Suppress normal standard library includes as they seem to end up being wrong
                shCommandBuilder.Append("-nostdlib ");

                // These aren't necessary for the compile to succeed but maybe specifying these before libc++ makes
                // it prefer symbols from these?

                // This first one requires a PIC linked llvm libc otherwise this will fail to link (which doesn't seem
                // to just stick with any argument flags in the Dockerfile)
                // ReSharper disable once CommentTypo
                // shCommandBuilder.Append("/usr/lib64/x86_64-unknown-linux-gnu/libllvmlibc.a ");

                shCommandBuilder.Append("/usr/lib64/x86_64-unknown-linux-gnu/libunwind.a ");
                shCommandBuilder.Append("/usr/lib64/x86_64-unknown-linux-gnu/libc++abi.a ");

                // This is necessary to compile
                shCommandBuilder.Append("/usr/lib64/x86_64-unknown-linux-gnu/libc++.a");
                shCommandBuilder.Append("' ");

                break;
            }

            case PackagePlatform.Windows:
            {
                // Cross compiling to windows
                shCommandBuilder.Append("-DCMAKE_SYSTEM_NAME=Windows ");
                shCommandBuilder.Append("-DCMAKE_SYSTEM_PROCESSOR=x86 ");

                shCommandBuilder.Append($"-DCMAKE_CXX_COMPILER={ContainerTool.CrossCompilerClangName} ");
                shCommandBuilder.Append($"-DCMAKE_C_COMPILER={ContainerTool.CrossCompilerClangName} ");

                shCommandBuilder.Append("-DCMAKE_C_FLAGS='' ");
                shCommandBuilder.Append("-DCMAKE_CXX_FLAGS='' ");

                shCommandBuilder.Append("-DCMAKE_SHARED_LINKER_FLAGS='-static -lc++ -lc++abi' ");

                break;
            }

            case PackagePlatform.Windows32:
            {
                // TODO: it's not really tested but it should work the same (with slightly tweaked tool names as the
                // 64-bit version)
                shCommandBuilder.Append($"-DCMAKE_CXX_COMPILER={ContainerTool.CrossCompilerClangName32Bit} ");
                shCommandBuilder.Append($"-DCMAKE_C_COMPILER={ContainerTool.CrossCompilerClangName32Bit} ");

                throw new NotImplementedException(
                    "TODO: implement the rest of this based on the 64-bit windows version");
            }

            case PackagePlatform.Mac:
                throw new NotSupportedException("Mac builds must be made natively on a mac");
            case PackagePlatform.Web:
                throw new NotImplementedException("This is not done yet");
            default:
                throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
        }

        // Switching to using the clang standard library here as well as setting the target

        shCommandBuilder.Append("-DCLANG_DEFAULT_CXX_STDLIB=libc++ ");
        shCommandBuilder.Append("-DCLANG_DEFAULT_RTLIB=compiler-rt ");

        // ReSharper restore StringLiteralTypo

        // Cmake build inside the container
        shCommandBuilder.Append("&& cmake ");
        shCommandBuilder.Append("--build ");
        shCommandBuilder.Append(". ");
        shCommandBuilder.Append("--config ");

        shCommandBuilder.Append(buildType);

        shCommandBuilder.Append(" --target ");
        shCommandBuilder.Append("install ");

        shCommandBuilder.Append("-j ");

        // TODO: add option to not use all cores
        shCommandBuilder.Append(Environment.ProcessorCount.ToString());

        startInfo.ArgumentList.Add(shCommandBuilder.ToString());

        ColourConsole.WriteNormalLine("Compiling inside the container...");

        var result = await ProcessRunHelpers.RunProcessAsync(startInfo, cancellationToken, false);

        if (result.ExitCode != 0)
        {
            ColourConsole.WriteErrorLine($"Failed to run compile in container (exit: {result.ExitCode}). " +
                "Is the container built and available or did an unexpected error happen inside the container?");

            return false;
        }

        ColourConsole.WriteSuccessLine("Build inside container succeeded");
        var libraries = options.Libraries ?? Enum.GetValues<Library>();

        if (!options.DebugLibrary)
        {
            ColourConsole.WriteInfoLine(
                "Extracting symbols (requires compiled Breakpad on the host) and stripping binaries");

            foreach (var library in libraries)
            {
                var source = GetPathToDistributableTempDll(library, platform);

                ColourConsole.WriteDebugLine($"Performing extraction on library: {source}");

                if (!await symbolHandler.ExtractSymbols(source, "./",
                        platform is PackagePlatform.Windows or PackagePlatform.Windows32, cancellationToken))
                {
                    ColourConsole.WriteErrorLine(
                        "Symbol extraction failed. Are breakpad tools installed at the expected path? " +
                        "And is the 'strip' tool available in PATH?");

                    return false;
                }
            }
        }
        else
        {
            ColourConsole.WriteNormalLine("Skipping symbol extraction for debug build");
        }

        ColourConsole.WriteInfoLine("Copying built libraries to the right folder");

        foreach (var library in libraries)
        {
            var version = GetLibraryVersion(library);

            var source = GetPathToDistributableTempDll(library, platform);
            var target = GetPathToLibraryDll(library, platform, version, true);

            var targetFolder = Path.GetDirectoryName(target);

            if (string.IsNullOrEmpty(targetFolder))
                throw new Exception("Couldn't get folder from library install path");

            Directory.CreateDirectory(targetFolder);

            File.Copy(source, target, true);

            ColourConsole.WriteDebugLine($"Copied {source} -> {target}");

            if (!options.DebugLibrary)
            {
                await BinaryHelpers.Strip(target, cancellationToken);

                ColourConsole.WriteNormalLine($"Stripped installed library at {target}");
            }
            else
            {
                ColourConsole.WriteDebugLine("Not stripping a debug library");
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        ColourConsole.WriteSuccessLine("Successfully prepared native libraries for distribution");

        return true;
    }

    private async Task<bool> CheckAndUpload(Library library, PackagePlatform platform,
        CancellationToken cancellationToken)
    {
        var version = GetLibraryVersion(library);
        var file = GetPathToLibraryDll(library, platform, version, true);

        if (!File.Exists(file))
        {
            ColourConsole.WriteWarningLine($"Skip checking uploading file that is missing locally: {file}");

            // For now this is not considered an error
            return true;
        }

        bool debug = options.DebugLibrary;

        if (string.IsNullOrEmpty(options.Key))
        {
            ColourConsole.WriteErrorLine("Key to access ThriveDevCenter is a required parameter");
            return false;
        }

        PrecompiledTag tags = 0;

        if (debug)
        {
            ColourConsole.WriteNormalLine("Uploading a debug version of the library");
            tags = PrecompiledTag.Debug;
        }

        using var httpClient = GetDevCenterClient();

        ColourConsole.WriteDebugLine($"Checking if we should upload {library}:{version}:{platform}:{tags}");

        var checkUrl = new Uri(await GetBaseRemoteUrlForLibrary(library), "offerVersion");

        var precompiledInitialData = new PrecompiledObjectVersionDTO
        {
            Version = version,
            Platform = platform,
            Tags = tags,
        };

        // Check if the library already exists
        ColourConsole.WriteDebugLine($"Checking if should upload, requesting: {checkUrl}");
        var response = await httpClient.PostAsJsonAsync(checkUrl, precompiledInitialData, cancellationToken);

        response.EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            ColourConsole.WriteDebugLine("Server already has this");
            return false;
        }

        if (response.StatusCode == HttpStatusCode.OK)
        {
            ColourConsole.WriteInfoLine(
                $"About to start uploading {library} for {platform} (file: {file}) that is missing from the server");

            if (!await ConsoleHelpers.WaitForInputToContinue(cancellationToken))
            {
                ColourConsole.WriteNormalLine("Canceling upload");
                return false;
            }

            ColourConsole.WriteNormalLine("Upload accepted");

            if (!await UploadLibraryToServer(library, platform, file, precompiledInitialData, cancellationToken))
            {
                throw new Exception("Uploading library to the server failed");
            }

            return true;
        }

        throw new Exception($"Unknown result code from offer response: {response.StatusCode}");
    }

    private async Task<Uri> GetBaseRemoteUrlForLibrary(Library library)
    {
        switch (library)
        {
            case Library.ThriveNative:
            {
                var nativeId = await thriveNativePrecompiledId.Value;

                return new Uri(new Uri(options.Url), $"api/v1/PrecompiledObject/{nativeId}/versions/");
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(library), library, null);
        }
    }

    private async Task<long> GetThriveNativeLibraryId()
    {
        using var httpClient = GetDevCenterClient();

        var data = await httpClient.GetFromJsonAsync<PrecompiledObjectDTO>(
            "api/v1/PrecompiledObject/byName/ThriveNative");

        if (data == null)
            throw new NullDecodedJsonException();

        ColourConsole.WriteDebugLine($"Determined that ThriveNative's precompiled ID is {data.Id}");

        return data.Id;
    }

    private async Task<bool> UploadLibraryToServer(Library library, PackagePlatform platform, string filePath,
        PrecompiledObjectVersionDTO precompiledData, CancellationToken cancellationToken)
    {
        // Prepare a compressed version for upload
        ColourConsole.WriteInfoLine("Preparing a compressed version of the library for upload");

        var compressedLocation = filePath + ".br";

        await CompressLibrary(filePath, compressedLocation, cancellationToken);

        precompiledData.Size = new FileInfo(compressedLocation).Length;
        ColourConsole.WriteDebugLine($"Size of compressed library: {precompiledData.Size}");

        ColourConsole.WriteWarningLine("Beginning upload... Canceling is no longer possible, otherwise a " +
            "non-uploaded library will persist on the DevCenter");

        ColourConsole.WriteNormalLine("Deleting such an item requires going to the DevCenter web app and " +
            "doing it manually");

        ColourConsole.WriteNormalLine("Fetching an upload URL");

        using var devCenterClient = GetDevCenterClient();

        var baseUrl = await GetBaseRemoteUrlForLibrary(library);
        var uploadRequestUrl = new Uri(baseUrl, "startUpload");

        cancellationToken.ThrowIfCancellationRequested();

        // We no longer want to cancel once we have reserved the precompiled object version
        // ReSharper disable once MethodSupportsCancellation
        var response = await devCenterClient.PostAsJsonAsync(uploadRequestUrl, precompiledData);

        // ReSharper disable once MethodSupportsCancellation
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            ColourConsole.WriteErrorLine($"Failed to request upload start for a library, response: {content}");
            ColourConsole.WriteNormalLine(
                "No precompiled object version should have been created yet so retry is possible");
            return false;
        }

        var uploadResponse = JsonSerializer.Deserialize<UploadRequestResponse>(content,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new NullDecodedJsonException();

        ColourConsole.WriteNormalLine("Got URL to upload to");

        ColourConsole.WriteNormalLine("Uploading data, this may take a while if the precompiled object is large");

        using var normalClient = new HttpClient();

        bool uploaded = false;

        for (int i = 0; i < 10; ++i)
        {
            if (i > 0)
            {
                ColourConsole.WriteNormalLine("Will retry upload in 15 seconds...");

                // Again, don't want to pass cancellation token to not cancel out of the retry
                // ReSharper disable once MethodSupportsCancellation
                await Task.Delay(TimeSpan.FromSeconds(i * 15));
            }

            // We have created the item already, do not cancel
            // ReSharper disable once MethodSupportsCancellation
            response = await normalClient.PutAsync(uploadResponse.UploadUrl,
                new StreamContent(File.OpenRead(compressedLocation)));

            if (!response.IsSuccessStatusCode)
            {
                ColourConsole.WriteErrorLine("Failed to send file to remote storage. Will retry a few times");

                // We want to read the error without canceling out of the retry process
                // ReSharper disable once MethodSupportsCancellation
                var responseContent = await response.Content.ReadAsStringAsync();

                ColourConsole.WriteWarningLine(
                    $"Put of file content to URL failed: {uploadResponse.UploadUrl}, {response.StatusCode}, " +
                    $"response: {responseContent}");

                continue;
            }

            uploaded = true;
            ColourConsole.WriteNormalLine("Uploaded to storage. Reporting success...");
            break;
        }

        if (!uploaded)
        {
            ColourConsole.WriteErrorLine("Ran out of retries to upload to remote storage. Precompiled object is " +
                "already reserved and requires manual action on the DevCenter to clear for a retry.");
            return false;
        }

        // Report upload success
        ColourConsole.WriteInfoLine("Reporting our upload success to the DevCenter");
        var successReportUrl = new Uri(baseUrl, "finishUpload");

        // Definitely don't cancel after uploading to the remote storage
        // ReSharper disable once MethodSupportsCancellation
        response = await devCenterClient.PostAsJsonAsync(successReportUrl, new TokenForm
        {
            Token = uploadResponse.VerifyToken,
        });

        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            ColourConsole.WriteWarningLine($"Response: {responseContent}");

            ColourConsole.WriteErrorLine("The DevCenter didn't accept our report of upload success. As the object " +
                "is already created retrying request manual deleting of the previous attempt on the DevCenter");

            return false;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            ColourConsole.WriteSuccessLine("Upload succeeded, but cancellation was requested, stopping now");
            cancellationToken.ThrowIfCancellationRequested();
        }

        ColourConsole.WriteSuccessLine(
            $"Successfully uploaded {library} for {platform} with size of: {precompiledData.Size.BytesToMiB()}");
        return true;
    }

    private async Task CompressLibrary(string filePath, string compressedLocation, CancellationToken cancellationToken)
    {
        var folder = Path.GetDirectoryName(compressedLocation);

        if (!string.IsNullOrEmpty(folder))
        {
            ColourConsole.WriteDebugLine($"Compressing {filePath} to a location in folder: {folder}");
            Directory.CreateDirectory(folder);
        }

        await using var writer = File.Create(compressedLocation);

        await using var reader = File.OpenRead(filePath);

        await using var compressor = new BrotliStream(writer, CompressionLevel.Optimal);

        await reader.CopyToAsync(compressor, cancellationToken);

        ColourConsole.WriteSuccessLine($"Created compressed file: {compressedLocation}");
    }

    private Task<bool> UploadMissingSymbolsToServer(CancellationToken cancellationToken)
    {
        var uploader = new SymbolUploader(options, "./");

        return uploader.Run(cancellationToken);
    }

    private async Task<bool> DownloadLibraryIfMissing(Library library, PackagePlatform platform,
        CancellationToken cancellationToken)
    {
        var version = GetLibraryVersion(library);
        var file = GetPathToLibraryDll(library, platform, version, true);

        if (File.Exists(file))
        {
            ColourConsole.WriteNormalLine($"Library already exists, skipping download of: {file}");
            return true;
        }

        var directory = Path.GetDirectoryName(file);

        if (string.IsNullOrEmpty(directory))
            throw new Exception("Failed to determine the folder the library should be in");

        Directory.CreateDirectory(directory);

        bool debug = options.DebugLibrary;

        PrecompiledTag tags = 0;

        if (debug)
        {
            ColourConsole.WriteNormalLine("Will try to download a debug version of the library");
            tags = PrecompiledTag.Debug;
        }

        using var httpClient = GetDevCenterClient();

        ColourConsole.WriteNormalLine(
            $"Checking if the precompiled object {library}:{version}:{platform}:{tags} is available");

        // Use the link endpoint to not get auto redirect
        var fetchUrl = new Uri(await GetBaseRemoteUrlForLibrary(library),
            $"{version}/{(int)platform}/{(int)tags}/link");

        var response = await httpClient.GetAsync(fetchUrl, cancellationToken);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            ColourConsole.WriteErrorLine(
                $"Library is not available for download, or a server error occurred, response: {content}");
            return false;
        }

        var downloadUrl = content;

        ColourConsole.WriteInfoLine($"Downloading {downloadUrl}");

        // Download the file without sending the authentication headers used for the DevCenter
        using var normalClient = new HttpClient();

        // Retry a few times in case the storage is not available
        for (int i = 0; i < 10; ++i)
        {
            if (i > 0)
            {
                ColourConsole.WriteNormalLine("Will retry download in 15 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(i * 15), cancellationToken);
            }

            try
            {
                using var download = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                download.EnsureSuccessStatusCode();

                // Write asynchronously while downloading data to not need to store the entire thing in memory
                await using var writer = File.Create(file);

                await using var readStream = await download.Content.ReadAsStreamAsync(cancellationToken);

                // And also compress while downloading as a compressed form of the data is not needed on disk for
                // anything so it would just take up unnecessary space
                await using var decompressor = new BrotliStream(readStream, CompressionMode.Decompress);

                await decompressor.CopyToAsync(writer, cancellationToken);

                await writer.FlushAsync(cancellationToken);

                var size = new FileInfo(file).Length;
                if (size < 1)
                    throw new Exception("Downloaded file is empty");

                ColourConsole.WriteSuccessLine(
                    $"Successfully downloaded and decompressed the file (size: {size.BytesToMiB()})");
                return true;
            }
            catch (Exception e)
            {
                ColourConsole.WriteErrorLine($"Error downloading, will retry a few times: {e}");
            }
        }

        ColourConsole.WriteErrorLine("Ran out of download retries");
        return false;
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

    private string GetPathToDistributableTempDll(Library library, PackagePlatform platform)
    {
        var basePath = Path.Combine(GetDistributableBuildFolderBase(platform),
            options.DebugLibrary ? "debug" : "release");

        if (platform is PackagePlatform.Windows or PackagePlatform.Windows32)
        {
            return Path.Combine(basePath, "bin", GetLibraryDllName(library, platform));
        }

        // This is for Linux
        return Path.Combine(basePath, "lib", GetLibraryDllName(library, platform));
    }

    private string GetDistributableBuildFolderBase(PackagePlatform platform)
    {
        return Path.Join(FolderToWriteDistributableBuildIn, platform.ToString().ToLowerInvariant());
    }

    private void CreateLinkTo(string linkFile, string linkTo)
    {
        if (File.Exists(linkFile))
        {
            ColourConsole.WriteDebugLine($"Removing existing link file: {linkFile}");
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

        ColourConsole.WriteDebugLine($"Created link {linkFile} -> {linkTo}");
    }

    private string GetNativeBuildFolder()
    {
        if (options.DebugLibrary)
            return "build-debug";

        return "build";
    }

    private HttpClient GetDevCenterClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(options.Url),
            Timeout = TimeSpan.FromMinutes(1),
        };

        if (!string.IsNullOrEmpty(options.Key))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Key);
        }

        return client;
    }

    private static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool CreateHardLink(string lpFileName, string lpExistingFileName,
            IntPtr lpSecurityAttributes);
    }
}
