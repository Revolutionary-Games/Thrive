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

/// <summary>
///   Handles the native C++ modules needed by Thrive
/// </summary>
public class NativeLibs
{
    private const string BuilderImageName = "localhost/thrive/native-builder:latest";
    private const string BuilderImageNameCross = "localhost/thrive/native-builder-cross:latest";
    private const string FolderToWriteDistributableBuildIn = "build/distributable_build";

    /// <summary>
    ///   Default libraries to operate on when nothing is explicitly selected. This no longer includes the early checks
    ///   library as a pure C# solution is used instead.
    /// </summary>
    private static readonly IList<NativeConstants.Library> DefaultLibraries = new[]
        { NativeConstants.Library.ThriveNative };

    private readonly Program.NativeLibOptions options;

    private readonly IList<PackagePlatform> platforms;

    private readonly SymbolHandler symbolHandler = new(null, null);

    private readonly Lazy<Task<long>> thriveNativePrecompiledId;
    private readonly Lazy<Task<long>> earlyCheckPrecompiledId;

    public NativeLibs(Program.NativeLibOptions options)
    {
        this.options = options;
        thriveNativePrecompiledId = new Lazy<Task<long>>(GetThriveNativeLibraryId);
        earlyCheckPrecompiledId = new Lazy<Task<long>>(GetEarlyCheckLibraryId);

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

    public async Task<bool> Run(CancellationToken cancellationToken)
    {
        // Make sure this base folder exists
        Directory.CreateDirectory(NativeConstants.LibraryFolder);

        await PackageTool.EnsureGodotIgnoreFileExistsInFolder(NativeConstants.LibraryFolder);

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
        var libraries = options.Libraries ?? DefaultLibraries;

        if (!Directory.Exists(releaseFolder))
        {
            ColourConsole.WriteErrorLine($"Release folder to install native library in doesn't exist: {releaseFolder}");
            ColourConsole.WriteErrorLine("Will not create / attempt to copy anyway as the release would likely " +
                "be broken due to file structure changing to what this script doesn't expect");

            return false;
        }

        var targetFolder = Path.Join(releaseFolder, NativeConstants.PackagedLibraryFolder);
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

    private PrecompiledTag GetTag(bool localBuild)
    {
        var tag = PrecompiledTag.None;

        if (options.DebugLibrary)
            tag |= PrecompiledTag.Debug;

        if (localBuild && options.DisableLocalAvx)
            tag |= PrecompiledTag.WithoutAvx;

        return tag;
    }

    private async Task<bool> RunOperation(Program.NativeLibOptions.OperationMode operation,
        CancellationToken cancellationToken)
    {
        ColourConsole.WriteNormalLine($"Performing operation {operation}");

        switch (operation)
        {
            case Program.NativeLibOptions.OperationMode.Check:
                return await OperateOnAllLibraries(
                    (library, platform, token) => CheckLibrary(library, platform, false, GetTag(true), token),
                    cancellationToken);
            case Program.NativeLibOptions.OperationMode.CheckDistributable:
                return await OperateOnAllLibraries(
                    (library, platform, token) => CheckLibrary(library, platform, true, GetTag(false), token),
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
                if (await OperateOnAllLibrariesWithResult(CheckAndUpload, cancellationToken) == true)
                {
                    ColourConsole.WriteNormalLine("Checking for potential symbols to upload after library upload");
                    return await UploadMissingSymbolsToServer(cancellationToken);
                }

                ColourConsole.WriteNormalLine("Skipping symbol upload check as the check upload operation " +
                    "didn't return true");

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
        Func<NativeConstants.Library, PackagePlatform, CancellationToken, Task<bool>> operation,
        CancellationToken cancellationToken)
    {
        var libraries = options.Libraries ?? DefaultLibraries;

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
        Func<NativeConstants.Library, PackagePlatform, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var libraries = options.Libraries ?? DefaultLibraries;

        bool resultSet = false;
        T result = default!;

        foreach (var library in libraries)
        {
            ColourConsole.WriteDebugLine($"Operating on library: {library}");

            var currentResult = await OperateOnAllPlatformsWithResult(library, operation, cancellationToken);
            if (!resultSet)
            {
                // Only update result when there is something to set, null implies skipping updating the result
                if (currentResult is not null)
                {
                    result = currentResult;
                    resultSet = true;
                }
            }
            else if (currentResult is false)
            {
                // TODO: not the cleanest way to specially overload this for false like this
                result = currentResult;
            }
        }

        return result;
    }

    private async Task<bool> OperateOnAllPlatforms(NativeConstants.Library library,
        Func<NativeConstants.Library, PackagePlatform, CancellationToken, Task<bool>> operation,
        CancellationToken cancellationToken)
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

    private async Task<T> OperateOnAllPlatformsWithResult<T>(NativeConstants.Library library,
        Func<NativeConstants.Library, PackagePlatform, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
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

    private Task<bool> CheckLibrary(NativeConstants.Library library, PackagePlatform platform,
        bool distributableVersion, PrecompiledTag tags, CancellationToken cancellationToken)
    {
        var file = NativeConstants.GetPathToLibraryDll(library, platform, NativeConstants.GetLibraryVersion(library),
            distributableVersion, tags);

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

    private Task<bool> InstallLibraryForEditor(NativeConstants.Library library, PackagePlatform platform,
        CancellationToken cancellationToken)
    {
        ColourConsole.WriteWarningLine("This is a deprecated operation. Godot 4 now allows directly loading " +
            "from the library from its storage path so no install is required.");

        return Task.FromResult(true);
    }

    private bool CopyLibraryFiles(NativeConstants.Library library, PackagePlatform platform,
        bool useDistributableLibraries, string target)
    {
        // TODO: other files?
        var file = NativeConstants.GetPathToLibraryDll(library, platform, NativeConstants.GetLibraryVersion(library),
            useDistributableLibraries, GetTag(!useDistributableLibraries));

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

    private async Task<bool> BuildLocally(NativeConstants.Library library, PackagePlatform platform,
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
            ExecutableFinder.PrintPathInfo(Console.Out);
            ColourConsole.WriteErrorLine("cmake not found. CMake is required for this build to work. " +
                "Make sure it is installed and added to PATH before trying again.");

            return false;
        }

        var buildFolder = GetNativeBuildFolder();

        // TODO: flag for clean builds
        Directory.CreateDirectory(buildFolder);

        // Ensure Godot doesn't try to import anything funny from the build folder
        await PackageTool.EnsureGodotIgnoreFileExistsInFolder(buildFolder);

        var installPath =
            Path.GetFullPath(GetLocalCMakeInstallTarget(platform, NativeConstants.GetLibraryVersion(library)));

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

        if (options.DisableLocalAvx)
        {
            startInfo.ArgumentList.Add("-DTHRIVE_AVX=OFF");
        }
        else
        {
            startInfo.ArgumentList.Add("-DTHRIVE_AVX=ON");
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
            ColourConsole.WriteErrorLine($"CMake configuration failed (exit: {result.ExitCode}). " +
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

        // When building Thrive native and the early check, those will conflict with each other and install each other
        // as well to their version files. This tries to remove the extra files.
        foreach (var file in Directory.EnumerateFiles(installPath, "*.*", SearchOption.AllDirectories))
        {
            foreach (var otherLibrary in Enum.GetValues<NativeConstants.Library>())
            {
                if (otherLibrary == library)
                    continue;

                // TODO: skip libraries not compiled at the same time if any are added in the future

                var name = NativeConstants.GetLibraryDllName(otherLibrary, platform, GetTag(true));
                var nameSecondary = NativeConstants.GetLibraryDllName(otherLibrary, platform, GetTag(false));

                if (file.Contains(name) || file.Contains(nameSecondary))
                {
                    // Don't delete unrelated type
                    if (!options.DebugLibrary && file.Contains("debug"))
                        continue;

                    if (options.DebugLibrary && file.Contains("release"))
                        continue;

                    File.Delete(file);
                    ColourConsole.WriteNormalLine($"Deleting likely duplicate install of a different library: {file}");
                }
            }
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

        var thriveContainerFolder = "/thrive";

        var startInfo = new ProcessStartInfo("podman");

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--rm");
        startInfo.ArgumentList.Add("-t");

        startInfo.ArgumentList.Add($"--volume={Path.GetFullPath(".")}:{thriveContainerFolder}:ro,z");
        startInfo.ArgumentList.Add($"--volume={Path.GetFullPath(compileInstallFolder)}:/install-target:rw,z");

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
        shCommandBuilder.Append($"cmake {thriveContainerFolder} ");
        shCommandBuilder.Append("-G Ninja ");

        // ReSharper disable StringLiteralTypo
        var buildType = "Distribution";

        if (options.DebugLibrary)
        {
            ColourConsole.WriteDebugLine("Creating a debug version of the distributable");
            buildType = "Debug";
        }
        else
        {
            shCommandBuilder.Append("-DTHRIVE_DISTRIBUTION=ON ");
        }

        shCommandBuilder.Append($"-DCMAKE_BUILD_TYPE={buildType} ");

        shCommandBuilder.Append("-DTHRIVE_AVX=ON ");

        // We explicitly enable LTO with compiler flags when we want as CMake when testing LTO seems to ignore a bunch
        // of flags
        // TODO: figure out how to get the Jolt interprocedural check to work (now fails with trying to link the wrong
        // standard library). Might be related to the compiler checks that fail below.
        shCommandBuilder.Append("-DINTERPROCEDURAL_OPTIMIZATION=ON ");

        shCommandBuilder.Append("-DCMAKE_INSTALL_PREFIX=/install-target ");

        // ReSharper disable once CommentTypo
        // Specify the CPU type to tune for and make available instructions for checking the available instructions
        // (_xgetbv)
        shCommandBuilder.Append("-DCMAKE_CXX_FLAGS=-march=sandybridge ");

        switch (platform)
        {
            case PackagePlatform.Linux:
            {
                shCommandBuilder.Append("-DCMAKE_CXX_COMPILER=clang++ ");
                shCommandBuilder.Append("-DCMAKE_C_COMPILER=clang ");
                shCommandBuilder.Append("-DCMAKE_CXX_COMPILER_AR=/usr/bin/llvm-ar ");

                break;
            }

            case PackagePlatform.Windows:
            {
                // Cross compiling to windows
                shCommandBuilder.Append("-DCMAKE_SYSTEM_NAME=Windows ");
                shCommandBuilder.Append("-DCMAKE_SYSTEM_PROCESSOR=x86 ");

                shCommandBuilder.Append($"-DCMAKE_CXX_COMPILER={ContainerTool.CrossCompilerClangName} ");
                shCommandBuilder.Append($"-DCMAKE_C_COMPILER={ContainerTool.CrossCompilerClangName} ");

                shCommandBuilder.Append("-DCMAKE_SHARED_LINKER_FLAGS='-static -lc++ -lc++abi' ");

                break;
            }

            case PackagePlatform.Windows32:
            {
                // TODO: it's not really tested but it should work the same (with slightly tweaked tool names as the
                // 64-bit version)
                shCommandBuilder.Append("-DCMAKE_SYSTEM_NAME=Windows ");
                shCommandBuilder.Append("-DCMAKE_SYSTEM_PROCESSOR=x86 ");

                shCommandBuilder.Append($"-DCMAKE_CXX_COMPILER={ContainerTool.CrossCompilerClangName32Bit} ");
                shCommandBuilder.Append($"-DCMAKE_C_COMPILER={ContainerTool.CrossCompilerClangName32Bit} ");

                shCommandBuilder.Append("-DCMAKE_SHARED_LINKER_FLAGS='-static -lc++ -lc++abi' ");

                throw new NotImplementedException("TODO: test (this was written based on the 64-bit windows version)");
            }

            case PackagePlatform.Mac:
                throw new NotSupportedException("Mac builds must be made natively on a mac");
            case PackagePlatform.Web:
                throw new NotImplementedException("This is not done yet");
            default:
                throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
        }

        // ReSharper restore StringLiteralTypo

        // Cmake build inside the container, once without AVX and once with
        for (int i = 0; i < 2; ++i)
        {
            if (i > 0)
            {
                // No AVX build
                shCommandBuilder.Append($"&& cmake {thriveContainerFolder} ");
                shCommandBuilder.Append("-DTHRIVE_AVX=OFF ");
            }

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
            shCommandBuilder.Append(' ');
        }

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
        var libraries = options.Libraries ?? DefaultLibraries;

        var baseTag = options.DebugLibrary ? PrecompiledTag.Debug : PrecompiledTag.None;

        if (!options.DebugLibrary)
        {
            ColourConsole.WriteInfoLine(
                "Extracting symbols (requires compiled Breakpad on the host) and stripping binaries");

            foreach (var library in libraries)
            {
                foreach (var tag in new[] { baseTag, baseTag | PrecompiledTag.WithoutAvx })
                {
                    var source = GetPathToDistributableTempDll(library, platform, tag);

                    ColourConsole.WriteDebugLine($"Performing extraction on library: {source}");

                    if (!await symbolHandler.ExtractSymbols(source, "./",
                            platform is PackagePlatform.Windows or PackagePlatform.Windows32, cancellationToken))
                    {
                        ColourConsole.WriteErrorLine(
                            "Symbol extraction failed. Are breakpad tools installed at the expected path?");

                        return false;
                    }
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
            var version = NativeConstants.GetLibraryVersion(library);

            foreach (var tag in new[] { baseTag, baseTag | PrecompiledTag.WithoutAvx })
            {
                var target = NativeConstants.GetPathToLibraryDll(library, platform, version, true, tag);

                var targetFolder = Path.GetDirectoryName(target);

                if (string.IsNullOrEmpty(targetFolder))
                    throw new Exception("Couldn't get folder from library install path");

                Directory.CreateDirectory(targetFolder);

                var source = GetPathToDistributableTempDll(library, platform, tag);

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
        }

        ColourConsole.WriteSuccessLine("Successfully prepared native libraries for distribution");

        return true;
    }

    private async Task<bool?> CheckAndUpload(NativeConstants.Library library, PackagePlatform platform,
        CancellationToken cancellationToken)
    {
        var version = NativeConstants.GetLibraryVersion(library);

        var baseTag = options.DebugLibrary ? PrecompiledTag.Debug : PrecompiledTag.None;

        bool uploaded = false;

        foreach (var tag in new[] { baseTag, baseTag | PrecompiledTag.WithoutAvx })
        {
            var file = NativeConstants.GetPathToLibraryDll(library, platform, version, true, tag);

            if (!File.Exists(file))
            {
                ColourConsole.WriteWarningLine($"Skip checking uploading file that is missing locally: {file}");

                // For now this is not considered an error
                return null;
            }

            if (string.IsNullOrEmpty(options.Key))
            {
                ColourConsole.WriteErrorLine("Key to access ThriveDevCenter is a required parameter");

                // Explicit fail to stop trying
                return false;
            }

            if ((tag & PrecompiledTag.Debug) != 0)
            {
                ColourConsole.WriteNormalLine("Uploading a debug version of the library");
            }

            var result = await UploadLocalLibrary(library, platform, version, tag, file, cancellationToken);

            if (result == false)
                return false;

            if (result == true)
                uploaded = true;
        }

        return uploaded ? uploaded : null;
    }

    private async Task<bool?> UploadLocalLibrary(NativeConstants.Library library, PackagePlatform platform,
        string version, PrecompiledTag tags, string file,
        CancellationToken cancellationToken)
    {
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
            // No preference to change the result
            return null;
        }

        if (response.StatusCode == HttpStatusCode.OK)
        {
            ColourConsole.WriteInfoLine(
                $"About to start uploading {library} for {platform} (file: {file}) that is missing from the server");

            ColourConsole.WriteNormalLine("Library uncompressed size is: " + new FileInfo(file).Length.BytesToMiB(3));

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

    private async Task<Uri> GetBaseRemoteUrlForLibrary(NativeConstants.Library library)
    {
        switch (library)
        {
            case NativeConstants.Library.ThriveNative:
            {
                var nativeId = await thriveNativePrecompiledId.Value;

                return new Uri(new Uri(options.Url), $"api/v1/PrecompiledObject/{nativeId}/versions/");
            }

            case NativeConstants.Library.EarlyCheck:
            {
                var nativeId = await earlyCheckPrecompiledId.Value;

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

    private async Task<long> GetEarlyCheckLibraryId()
    {
        using var httpClient = GetDevCenterClient();

        var data = await httpClient.GetFromJsonAsync<PrecompiledObjectDTO>(
            "api/v1/PrecompiledObject/byName/ThriveEarlyCheck");

        if (data == null)
            throw new NullDecodedJsonException();

        ColourConsole.WriteDebugLine($"Determined that Early Check's precompiled ID is {data.Id}");

        return data.Id;
    }

    private async Task<bool> UploadLibraryToServer(NativeConstants.Library library, PackagePlatform platform,
        string filePath,
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

    private async Task<bool> DownloadLibraryIfMissing(NativeConstants.Library library, PackagePlatform platform,
        CancellationToken cancellationToken)
    {
        var version = NativeConstants.GetLibraryVersion(library);

        var baseTag = options.DebugLibrary ? PrecompiledTag.Debug : PrecompiledTag.None;

        foreach (var tag in new[] { baseTag, baseTag | PrecompiledTag.WithoutAvx })
        {
            var file = NativeConstants.GetPathToLibraryDll(library, platform, version, true, tag);

            if (File.Exists(file))
            {
                ColourConsole.WriteNormalLine($"Library already exists, skipping download of: {file}");
                return true;
            }

            var directory = Path.GetDirectoryName(file);

            if (string.IsNullOrEmpty(directory))
                throw new Exception("Failed to determine the folder the library should be in");

            Directory.CreateDirectory(directory);

            if ((tag & PrecompiledTag.Debug) != 0)
            {
                ColourConsole.WriteNormalLine("Will try to download a debug version of the library");
            }

            if (await DownloadRemoteLibrary(library, platform, version, tag, file, cancellationToken) == false)
                return false;
        }

        return true;
    }

    private async Task<bool> DownloadRemoteLibrary(NativeConstants.Library library, PackagePlatform platform,
        string version, PrecompiledTag tags, string targetFile, CancellationToken cancellationToken)
    {
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

        ColourConsole.WriteInfoLine($"Downloading {library} for {platform} version: {version}");

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
                await using var writer = File.Create(targetFile);

                await using var readStream = await download.Content.ReadAsStreamAsync(cancellationToken);

                // And also compress while downloading as a compressed form of the data is not needed on disk for
                // anything so it would just take up unnecessary space
                await using var decompressor = new BrotliStream(readStream, CompressionMode.Decompress);

                await decompressor.CopyToAsync(writer, cancellationToken);

                await writer.FlushAsync(cancellationToken);

                var size = new FileInfo(targetFile).Length;
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

    private string GetLocalCMakeInstallTarget(PackagePlatform platform, string version)
    {
        return Path.Combine(NativeConstants.LibraryFolder, platform.ToString().ToLowerInvariant(), version);
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

    private string GetPathToDistributableTempDll(NativeConstants.Library library, PackagePlatform platform,
        PrecompiledTag tags)
    {
        var basePath = Path.Combine(GetDistributableBuildFolderBase(platform),
            options.DebugLibrary ? "debug" : "release");

        if (platform is PackagePlatform.Windows or PackagePlatform.Windows32)
        {
            return Path.Combine(basePath, "bin", NativeConstants.GetLibraryDllName(library, platform, tags));
        }

        // This is for Linux
        return Path.Combine(basePath, "lib", NativeConstants.GetLibraryDllName(library, platform, tags));
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
