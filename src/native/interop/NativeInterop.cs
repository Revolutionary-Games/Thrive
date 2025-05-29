// #define DEBUG_LIBRARY_LOAD

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using DevCenterCommunication.Models.Enums;
using Godot;
using SharedBase.Models;
using SharedBase.Utilities;

/// <summary>
///   Calling interface from C# to the native code side of things for the native module. For the GDExtension stuff see
///   <see cref="ExtensionInterop"/>
/// </summary>
public static class NativeInterop
{
    private const string GODOT_INTERNAL_PATH_LIKELY_MARKER = "data_Thrive_";

    // Need these delegate holders to keep delegates alive
    private static readonly NativeMethods.OnLogMessage LogMessageCallback = ForwardMessage;

    private static readonly Dictionary<string, string> FoundFolderLibraries = new();

    private static bool disableAvx;

    private static bool loadCalled;
    private static bool nativeLoadSucceeded;
    private static bool cpuIsInsufficient;

    private static bool printedDistributableNotice;
    private static bool printedErrorAboutExecutablePath;

    private static int version = -1;

    private static bool printedDistributableWarning;

#if DEBUG
    private static bool printedSteamLibName;
#endif

    /// <summary>
    ///   Performs any initialization needed by the native library (note has to be called after the library is loaded)
    /// </summary>
    /// <param name="settings">Current game settings</param>
    public static void Init(Settings settings)
    {
        if (!nativeLoadSucceeded)
        {
            GD.PrintErr("Native library init should not be called as the library load failed");
            throw new InvalidOperationException("Library must be loaded first");
        }

        // Settings are passed as probably in the future something needs to be setup right in the native side of
        // things for the initial settings
        _ = settings;

        NativeMethods.SetLogForwardingCallback(LogMessageCallback);

        var result = NativeMethods.InitThriveLibrary();

        if (result != 0)
        {
            throw new InvalidOperationException($"Failed to initialize Thrive native library, code: {result}");
        }

        // TaskExecutor sets the number of used background threads on the native side

#if DEBUG
        CheckSizesOfInteropTypes();
#endif
    }

    /// <summary>
    ///   Loads and checks the native library is good to use
    /// </summary>
    /// <exception cref="Exception">If the library is not fine (wrong version)</exception>
    /// <exception cref="DllNotFoundException">If finding the library failed</exception>
    public static void Load()
    {
        if (loadCalled)
            throw new InvalidOperationException("Load has been called already");

        // Ensure this is not true if load fails partway through
        nativeLoadSucceeded = false;

        loadCalled = true;

        if (cpuIsInsufficient)
        {
            GD.PrintErr("Native library load was called even though current CPU is not capable of running it");
            throw new InvalidOperationException("Native library is detected as incompatible");
        }

        version = NativeMethods.CheckAPIVersion();

        if (version != NativeConstants.Version)
        {
            GD.PrintErr("Thrive native library is the wrong version! " +
                "Please verify game files or if you are developing Thrive, recompile the native library.");

            throw new Exception($"Failed to initialize Thrive native library, unexpected version {version} " +
                $"is not the required: {NativeConstants.Version}");
        }

        GD.Print("Loaded native Thrive library version ", version);
        nativeLoadSucceeded = true;

        // Enable debug logging if this is being debugged
#if DEBUG
        NativeMethods.SetLogLevel(NativeMethods.LogLevel.Debug);
#endif
    }

    /// <summary>
    ///   Sets the custom import resolver that understands where Thrive libraries are installed to.
    /// </summary>
    /// <param name="forAssembly">The assembly to set the resolver for, defaults to the calling assembly</param>
    public static void SetDllImportResolver(Assembly? forAssembly = null)
    {
        forAssembly ??= Assembly.GetCallingAssembly();

        NativeLibrary.SetDllImportResolver(forAssembly, DllImportResolver);
    }

    /// <summary>
    ///   Checks that current CPU is sufficiently new (has the required instruction set extensions) for running the
    ///   Thrive native module
    /// </summary>
    /// <returns>True if everything is fine and load can proceed</returns>
    public static bool CheckCPU()
    {
        try
        {
            var result = CheckCPUFeaturesFull();

            // Apple doesn't support x86 extensions so we nudge things on the right track here
            if (OperatingSystem.IsMacOS())
            {
                GD.Print("Mac detected so skipping CPU check and not trying to use AVX");
                disableAvx = true;
                return true;
            }

            // If the CPU can support the full speed library all is well
            if (result == CPUCheckResult.CPUCheckSuccess)
            {
                // Ensure AVX is on to look for the right library
                disableAvx = false;
                return true;
            }

            // Try the compatibility library
            var originalResult = result;

            result = CheckCPUFeaturesCompatibility();

            if (result == CPUCheckResult.CPUCheckSuccess)
            {
                GD.Print("Cannot use full-speed Thrive native library due to: " +
                    GetMissingFeatureList(originalResult));

                GD.Print("Using slower Thrive native library that doesn't rely on as new CPU instructions");

                disableAvx = true;
                return true;
            }

            cpuIsInsufficient = true;

            GD.PrintErr("Current CPU detected as not sufficient for Thrive");

            GD.PrintErr(GetMissingFeatureList(result));

            GD.PrintErr("Current CPU: ", OS.GetProcessorName());

            return false;
        }
        catch (DllNotFoundException e)
        {
            if (Engine.IsEditorHint())
            {
                GD.Print("Cannot load early check library within the editor concept due to it missing");
                return false;
            }

            GD.PrintErr("Cannot load early check library to check CPU features: ", e);
            return false;
        }
    }

    /// <summary>
    ///   Releases all native resources and prepares the library for process exit
    /// </summary>
    public static void Shutdown()
    {
        if (!nativeLoadSucceeded)
        {
            GD.Print("Skipping native library shutdown as it was not fully loaded");
            return;
        }

        nativeLoadSucceeded = false;

        NativeMethods.ShutdownThriveLibrary();
    }

    /// <summary>
    ///   Disable loading avx-enabled libraries even if AVX was detected as being available
    /// </summary>
    public static void DisableAvx()
    {
        disableAvx = true;
    }

    /// <summary>
    ///   Gets the intercommunication interface for the native libraries
    /// </summary>
    /// <returns>IntPtr put into the variant on success, 0 on failure</returns>
    public static Variant GetIntercommunication(out int libraryVersion)
    {
        libraryVersion = version;

        if (!nativeLoadSucceeded)
        {
            GD.PrintErr("Native load hasn't succeeded, cannot get intercommunication");
            return Variant.CreateFrom(0);
        }

        return NativeMethods.GetIntercommunicationBridge().ToInt64();
    }

    public static void NotifyWantedThreadCountChanged(int threads)
    {
        if (!nativeLoadSucceeded)
            return;

        NativeMethods.SetNativeExecutorThreads(threads);
    }

    private static CPUCheckResult CheckCPUFeaturesFull()
    {
        var result = CPUCheckResult.CPUCheckSuccess;

        // TODO: should this check Avx2.X64.IsSupported instead or also?
        if (!Avx2.IsSupported)
            result |= CPUCheckResult.CPUCheckMissingAvx2;

        if (!Avx.IsSupported)
            result |= CPUCheckResult.CPUCheckMissingAvx;

        if (!Lzcnt.IsSupported)
            result |= CPUCheckResult.CPUCheckMissingLzcnt;

        // For TZCNT instruction
        if (!Bmi1.IsSupported)
            result |= CPUCheckResult.CPUCheckMissingBmi1;

        if (!Fma.IsSupported)
            result |= CPUCheckResult.CPUCheckMissingFma;

        // F16C cannot be checked easily, so for now assume it is present if the other instruction checks pass

        return result | CheckCPUFeaturesCompatibility();
    }

    private static CPUCheckResult CheckCPUFeaturesCompatibility()
    {
        var result = CPUCheckResult.CPUCheckSuccess;

        return result;
    }

    private static string GetMissingFeatureList(CPUCheckResult result)
    {
        var builder = new StringBuilder();

        // ReSharper disable StringLiteralTypo

        if ((result & CPUCheckResult.CPUCheckMissingAvx) != 0)
        {
            if (builder.Length > 0)
                builder.Append('\n');
            builder.Append("CPU is missing AVX 1 extension instruction support");
        }

        if ((result & CPUCheckResult.CPUCheckMissingAvx2) != 0)
        {
            if (builder.Length > 0)
                builder.Append('\n');
            builder.Append("CPU is missing AVX 2 extension instruction support");
        }

        if ((result & CPUCheckResult.CPUCheckMissingSse41) != 0)
        {
            if (builder.Length > 0)
                builder.Append('\n');
            builder.Append("CPU is missing SSE 4.1 support");
        }

        if ((result & CPUCheckResult.CPUCheckMissingSse42) != 0)
        {
            if (builder.Length > 0)
                builder.Append('\n');
            builder.Append("CPU is missing SSE 4.2 support");
        }

        if ((result & CPUCheckResult.CPUCheckMissingLzcnt) != 0)
        {
            if (builder.Length > 0)
                builder.Append('\n');
            builder.Append("CPU is missing LZCNT support");
        }

        if ((result & CPUCheckResult.CPUCheckMissingBmi1) != 0)
        {
            if (builder.Length > 0)
                builder.Append('\n');
            builder.Append("CPU is missing BMI 1 (TZCNT) support");
        }

        if ((result & CPUCheckResult.CPUCheckMissingFma) != 0)
        {
            if (builder.Length > 0)
                builder.Append('\n');

            builder.Append("CPU is missing FMA support");
        }

        if ((result & CPUCheckResult.CPUCheckMissingF16C) != 0)
        {
            if (builder.Length > 0)
                builder.Append('\n');
            builder.Append("CPU is missing F16C support");
        }

        // ReSharper restore StringLiteralTypo

        if (builder.Length < 1)
            builder.Append("Unknown problem with CPU check");

        return builder.ToString();
    }

    private static void ForwardMessage(IntPtr messageData, int messageLength, NativeMethods.LogLevel level)
    {
        var message = Marshal.PtrToStringAnsi(messageData, messageLength);

#if DEBUG

        // Pause debugger when detecting a native assertion fail to give some idea as to what's going on
        if (message.Contains("assert failed"))
        {
            if (Debugger.IsAttached)
                Debugger.Break();
        }
#endif

        if (level <= NativeMethods.LogLevel.Info)
        {
            GD.Print("[NATIVE] ", message);
        }
        else if (level <= NativeMethods.LogLevel.Warning)
        {
            // TODO: something different for warning level?
            GD.Print("[NATIVE] WARNING:", message);
        }
        else
        {
            GD.PrintErr("[NATIVE] ", message);
        }
    }

    private static void CheckSizesOfInteropTypes()
    {
        CheckSizeOfType<JVec3>(3 * 8);
        CheckSizeOfType<JVecF3>(3 * 4);
        CheckSizeOfType<JQuat>(4 * 4);
        CheckSizeOfType<JColour>(4 * 4);

        CheckSizeOfType<PhysicsCollision>(48);
        CheckSizeOfType<SubShapeDefinition>(40);
    }

    private static void CheckSizeOfType<T>(int expected)
    {
        var size = Marshal.SizeOf<T>();
        if (size != expected)
        {
            throw new Exception(
                $"Unexpected size for type {typeof(T).FullName}, expected size to be: {expected} but it is {size}");
        }
    }

    private static PrecompiledTag GetTag(bool debug)
    {
        if (disableAvx)
            return debug ? (PrecompiledTag.Debug | PrecompiledTag.WithoutAvx) : PrecompiledTag.WithoutAvx;

        return debug ? PrecompiledTag.Debug : PrecompiledTag.None;
    }

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // Would be complicated to inline due to the conditional compilation
        // ReSharper disable once InlineOutVariableDeclaration
        IntPtr loaded;

        // TODO: caching once loaded? This method is called again for each native method that is used so this gets
        // called some extra 50 or so times.

        if (libraryName == "steam_api")
        {
            var steamName = "libsteam_api.so";

            if (FeatureInformation.IsWindows())
            {
                steamName = "steam_api64.dll";
            }
            else if (FeatureInformation.IsMac())
            {
                steamName = "libsteam_api.dylib";
            }

#if DEBUG
            if (!printedSteamLibName)
            {
                GD.Print("Searching for Steam library: ", steamName);
                printedSteamLibName = true;
            }
#endif

            if (LookForLibraryUpInFolders(steamName, out loaded))
                return loaded;

            if (LoadLibraryIfExists(Path.Join(GetExecutableFolder(), steamName), out loaded))
                return loaded;

            GD.PrintErr("Steam API library seems to be missing");
            return NativeLibrary.Load(libraryName, assembly, searchPath);
        }

        if (!NativeConstants.GetLibraryFromName(libraryName, out var library))
        {
#if DEBUG
            GD.Print("Loading non-thrive library: ", libraryName);
#endif
            return NativeLibrary.Load(libraryName, assembly, searchPath);
        }

        if (library == NativeConstants.Library.ThriveExtension)
        {
            if (!OperatingSystem.IsMacOS())
            {
                // Special GDExtension handling, we assume Godot has already loaded it

                var modules = Process.GetCurrentProcess().Modules;
                var count = modules.Count;

                for (var i = 0; i < count; ++i)
                {
                    var module = modules[i];

                    if (module.ModuleName.Contains("thrive_extension"))
                    {
#if DEBUG_LIBRARY_LOAD
                        GD.Print($"Trying to use already loaded module path: {module.FileName}");
#endif

                        return NativeLibrary.Load(module.FileName);
                    }
                }

                GD.PrintErr("GDExtension was not loaded by Godot, falling back to default library load but this " +
                    "will likely fail");
            }
            else
            {
                // For some reason the modules list stays at one item on a Mac so we need to do some special assuming
                // here
                // TODO: this will be problematic in the future if debug specific version of libraries are added as
                // supported in the .gdextension files

                var file = NativeConstants.GetLibraryDllName(NativeConstants.Library.ThriveExtension,
                    PackagePlatform.Mac, PrecompiledTag.WithoutAvx);

                if (File.Exists(file))
                {
#if DEBUG_LIBRARY_LOAD
                    GD.Print($"Loading Mac GDExtension from: {file}");
#endif

                    return NativeLibrary.Load(file);
                }

                var adjustedFile = Path.Combine("lib", file);

                if (File.Exists(adjustedFile))
                {
#if DEBUG_LIBRARY_LOAD
                    GD.Print($"Loading Mac GDExtension from: {adjustedFile}");
#endif

                    return NativeLibrary.Load(adjustedFile);
                }

                // Special case needed for .app packaging
                var location = GetExecutableFolder();

                adjustedFile = Path.Combine(location, file);

                if (File.Exists(adjustedFile))
                {
#if DEBUG_LIBRARY_LOAD
                    GD.Print($"Loading Mac GDExtension from: {adjustedFile}");
#endif

                    return NativeLibrary.Load(adjustedFile);
                }

#if DEBUG_LIBRARY_LOAD
                GD.Print($"Attempted Mac GDExtension: {adjustedFile}");
#endif

                adjustedFile = Path.Combine(location, "lib", file);

                if (File.Exists(adjustedFile))
                {
#if DEBUG_LIBRARY_LOAD
                    GD.Print($"Loading Mac GDExtension from: {adjustedFile}");
#endif

                    return NativeLibrary.Load(adjustedFile);
                }

#if DEBUG_LIBRARY_LOAD
                GD.Print($"Last attempted Mac GDExtension: {adjustedFile}");
#endif

                GD.PrintErr(
                    "Mac GDExtension special load logic failed, falling back to default library load but this " +
                    "will likely fail");
            }

            return NativeLibrary.Load(libraryName, assembly, searchPath);
        }

        var currentPlatform = PlatformUtilities.GetCurrentPlatform();

        // TODO: add a flag / some kind of option to skip loading the debug library

#if DEBUG
        if (LoadLibraryIfExists(NativeConstants.GetPathToLibraryDll(library, currentPlatform,
                NativeConstants.GetLibraryVersion(library), false, GetTag(true)), out loaded))
        {
            return loaded;
        }

        if (LoadLibraryIfExists(NativeConstants.GetPathToLibraryDll(library, currentPlatform,
                NativeConstants.GetLibraryVersion(library), true, GetTag(true)), out loaded))
        {
            if (!printedDistributableWarning)
            {
                GD.Print("Loaded a distributable debug library, this is not optimal but likely works");
                printedDistributableWarning = true;
            }

            return loaded;
        }
#endif

        if (!Engine.IsEditorHint())
        {
            // Load from libs directory, needed when the game is packaged
            if (LoadLibraryIfExists(Path.Join(NativeConstants.PackagedLibraryFolder,
                    NativeConstants.GetLibraryDllName(library, currentPlatform, GetTag(false))), out loaded))
            {
                return loaded;
            }
        }

        if (LoadLibraryIfExists(NativeConstants.GetPathToLibraryDll(library, currentPlatform,
                NativeConstants.GetLibraryVersion(library), false, GetTag(false)), out loaded))
        {
            return loaded;
        }

        if (!printedDistributableNotice)
        {
            GD.Print("Library not found yet at expected paths, trying a distributable version");
            printedDistributableNotice = true;
        }

        if (LoadLibraryIfExists(NativeConstants.GetPathToLibraryDll(library, currentPlatform,
                NativeConstants.GetLibraryVersion(library), true, GetTag(false)), out loaded))
        {
            return loaded;
        }

        GD.PrintErr("Couldn't find library at any expected path, falling back to default load behaviour, " +
            "which is unlikely to find anything");

        return NativeLibrary.Load(libraryName, assembly, searchPath);
    }

    /// <summary>
    ///   Tries to get a sensible executable folder. Due to Godot this may not always work but this tries to be right
    /// </summary>
    /// <returns>Executable path or empty string</returns>
    private static string GetExecutableFolder()
    {
        try
        {
            var location = AppDomain.CurrentDomain.BaseDirectory;

            if (location == null)
            {
                throw new Exception("Entry assembly location is empty");
            }

            location = RemoveFilePrefix(location);

            // Remove one folder level if this is likely an internal Godot path (when packaged)
            if (location.Contains(GODOT_INTERNAL_PATH_LIKELY_MARKER))
            {
                // On Mac handle .app folder usage
                if (Path.GetFullPath(location).Contains(".app/"))
                {
                    return Path.Join(location, "../../MacOS");
                }

                return Path.Join(location, "..");
            }

            return location;
        }
        catch (Exception e)
        {
            // Cannot detect
            if (!printedErrorAboutExecutablePath)
            {
                GD.PrintErr("Cannot determine current location of the running executable: ", e);
                printedErrorAboutExecutablePath = true;
            }

            return string.Empty;
        }
    }

    private static string RemoveFilePrefix(string location)
    {
        if (location.StartsWith("file://"))
        {
            location = location.Substring("file://".Length);
        }

        return location;
    }

    private static bool LoadLibraryIfExists(string libraryPath, out IntPtr loaded)
    {
        var executableFolder = GetExecutableFolder();

        var executableRelative = Path.Join(executableFolder, libraryPath);

        if (File.Exists(libraryPath) || File.Exists(executableRelative))
        {
            string full;
            if (File.Exists(libraryPath))
            {
#if DEBUG_LIBRARY_LOAD
                GD.Print("Loading library: ", libraryPath);
#endif
                full = Path.GetFullPath(libraryPath);
            }
            else
            {
#if DEBUG_LIBRARY_LOAD
                GD.Print("Loading library relative to executable: ", executableRelative);
#endif
                full = Path.GetFullPath(executableRelative);
            }

            loaded = NativeLibrary.Load(full);
            return true;
        }

#if DEBUG_LIBRARY_LOAD
        GD.Print("Candidate library path doesn't exist: ", libraryPath);
        GD.Print("Executable relative variant: ", executableRelative);
#endif

        loaded = IntPtr.Zero;
        return false;
    }

    private static bool LookForLibraryUpInFolders(string libraryName, out IntPtr loaded)
    {
        // If library was already looked for, return that instead of doing the lookup again assuming that the libraries
        // won't be moved or deleted during runtime
        if (FoundFolderLibraries.TryGetValue(libraryName, out var knownPath))
        {
            return LoadLibraryIfExists(knownPath, out loaded);
        }

        var folders = Path.GetFullPath(".").Split(Path.DirectorySeparatorChar);

        loaded = IntPtr.Zero;

        if (folders.Length < 2)
        {
            GD.PrintErr("Cannot determine parts of current path to search a library in");
            return false;
        }

        for (int take = folders.Length; take > 0; --take)
        {
            try
            {
                var testPath = Path.Join(string.Join(Path.DirectorySeparatorChar, folders.Take(take)), libraryName);

                if (LoadLibraryIfExists(testPath, out loaded))
                {
                    FoundFolderLibraries[libraryName] = testPath;

#if DEBUG_LIBRARY_LOAD
                    GD.Print($"Remembering that library {libraryName} exists at path: {testPath}");
#endif
                    return true;
                }
            }
            catch (Exception e)
            {
                GD.Print($"Cannot test for {libraryName} existing at search depth {take}: {e}");
                break;
            }
        }

        FoundFolderLibraries[libraryName] = "NOT_FOUND_LIBRARY";
        return false;
    }
}

/// <summary>
///   Thrive native library general methods / things needed from multiple places. Specific class methods are split out
///   as the partial classes to logically split the methods into groups
/// </summary>
internal static partial class NativeMethods
{
    internal delegate void OnLogMessage(IntPtr messageData, int messageLength, LogLevel level);

    internal delegate void OnLineDraw(JVec3 from, JVec3 to, JColour colour);

    internal delegate void OnTriangleDraw(JVec3 vertex1, JVec3 vertex2, JVec3 vertex3, JColour colour);

    internal enum LogLevel : byte
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
    }

    [DllImport("thrive_native")]
    internal static extern int InitThriveLibrary();

    [DllImport("thrive_native")]
    internal static extern int CheckAPIVersion();

    [DllImport("early_checks")]
    internal static extern int CheckEarlyAPIVersion();

    [DllImport("thrive_native")]
    internal static extern void ShutdownThriveLibrary();

    [DllImport("thrive_native")]
    internal static extern IntPtr GetIntercommunicationBridge();

    [DllImport("early_checks")]
    internal static extern CPUCheckResult CheckRequiredCPUFeatures();

    [DllImport("early_checks")]
    internal static extern CPUCheckResult CheckCompatibilityLibraryCPUFeatures();

    [DllImport("thrive_native")]
    internal static extern void SetLogLevel(LogLevel level);

    [DllImport("thrive_native")]
    internal static extern void SetLogForwardingCallback(OnLogMessage callback);

    [DllImport("thrive_native")]
    internal static extern void SetNativeExecutorThreads(int count);

    [DllImport("thrive_native")]
    internal static extern int GetNativeExecutorThreads();

    // The wrapper-specific methods are in their respective files like PhysicalWorld.cs etc.
}
