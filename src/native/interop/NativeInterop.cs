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
using SharedBase.Utilities;

/// <summary>
///   Calling interface from C# to the native code side of things for the native module
/// </summary>
public static class NativeInterop
{
    // Need these delegate holders to keep delegates alive
    private static readonly NativeMethods.OnLogMessage LogMessageCallback = ForwardMessage;

    private static readonly NativeMethods.OnLineDraw LineDrawCallback = ForwardLineDraw;
    private static readonly NativeMethods.OnTriangleDraw TriangleDrawCallback = ForwardTriangleDraw;

    private static readonly Dictionary<string, string> FoundFolderLibraries = new();

    private static bool disableAvx;

    private static bool loadCalled;
    private static bool debugDrawIsPossible;
    private static bool nativeLoadSucceeded;

    private static bool printedDistributableNotice;

#if DEBUG
    private static bool printedSteamLibName;
#endif

    public delegate void OnLineDraw(Vector3 from, Vector3 to, Color colour);

    public delegate void OnTriangleDraw(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Color colour);

    // These forwarding static event handlers are needed, otherwise the callback coming back will have just entirely
    // bogus "this" values
    private static event OnLineDraw? OnLineDrawHandler;
    private static event OnTriangleDraw? OnTriangleDrawHandler;

    /// <summary>
    ///   Performs any initialization needed by the native library (note has to be called after the library is loaded)
    /// </summary>
    /// <param name="settings">Current game settings</param>
    public static void Init(Settings settings)
    {
        // Settings are passed as probably in the future something needs to be setup right in the native side of
        // things for the initial settings
        _ = settings;

        NativeMethods.SetLogForwardingCallback(LogMessageCallback);

        var result = NativeMethods.InitThriveLibrary();

        if (result != 0)
        {
            throw new InvalidOperationException($"Failed to initialize Thrive native library, code: {result}");
        }

        try
        {
            debugDrawIsPossible = NativeMethods.SetDebugDrawerCallbacks(LineDrawCallback, TriangleDrawCallback);
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to initialize potential for debug drawing: ", e);
            debugDrawIsPossible = false;
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

        loadCalled = true;

        // ReSharper disable once CommentTypo
        // TODO: come up with some approach for putting the native library to a sensible folder,
        // approach trying to manually load the library doesn't work (unless we manually look up all the methods
        // instead of using DllImportAttribute, also mono_dllmap_insert doesn't work as still the attributes load
        // before that can be used to set. With .NET 7 it should be possible to finally cleanly fix this:
        // https://learn.microsoft.com/en-us/dotnet/standard/native-interop/cross-platform#custom-import-resolver
        // `NativeLibrary.Load` would probably also be a good way to do something

        int version = NativeMethods.CheckAPIVersion();

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

            // If can support the full speed library all is well
            if (result == CPUCheckResult.CPUCheckSuccess)
            {
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

            GD.PrintErr("Current CPU detected as not sufficient for Thrive");

            GD.PrintErr(GetMissingFeatureList(result));

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

        NativeMethods.DisableDebugDrawerCallbacks();
        NativeMethods.ShutdownThriveLibrary();
    }

    /// <summary>
    ///   Disable loading avx-enabled libraries even if AVX was detected as being available
    /// </summary>
    public static void DisableAvx()
    {
        disableAvx = true;
    }

    public static bool RegisterDebugDrawer(OnLineDraw lineDraw, OnTriangleDraw triangleDraw)
    {
        if (!debugDrawIsPossible)
            return false;

        OnLineDrawHandler += lineDraw;
        OnTriangleDrawHandler += triangleDraw;

        return true;
    }

    public static void RemoveDebugDrawer()
    {
        // TODO: do single objects need to be able to unregister?
        OnLineDrawHandler = null;
        OnTriangleDrawHandler = null;

        if (nativeLoadSucceeded)
        {
            NativeMethods.DisableDebugDrawerCallbacks();
        }
        else
        {
            GD.Print("Skip native side debug draw unregister as the native library is not loaded");
        }
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

        return result | CheckCPUFeaturesCompatibility();
    }

    private static CPUCheckResult CheckCPUFeaturesCompatibility()
    {
        var result = CPUCheckResult.CPUCheckSuccess;

        if (!Sse42.IsSupported)
            result |= CPUCheckResult.CPUCheckMissingSse42;

        if (!Sse41.IsSupported)
            result |= CPUCheckResult.CPUCheckMissingSse41;

        return result;
    }

    private static string GetMissingFeatureList(CPUCheckResult result)
    {
        var builder = new StringBuilder();

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

    private static void ForwardLineDraw(JVec3 from, JVec3 to, JColour colour)
    {
        // TODO: is it possible to preserve precision by for example positioning the debug draw near the player?
        OnLineDrawHandler?.Invoke(from, to, colour);
    }

    private static void ForwardTriangleDraw(JVec3 vertex1, JVec3 vertex2, JVec3 vertex3, JColour colour)
    {
        OnTriangleDrawHandler?.Invoke(vertex1, vertex2, vertex3, colour);
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

        var currentPlatform = PlatformUtilities.GetCurrentPlatform();

        // TODO: different name when no avx is detected

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
            GD.Print("Loaded a distributable debug library, this is not optimal but likely works");
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

    private static bool LoadLibraryIfExists(string libraryPath, out IntPtr loaded)
    {
        if (File.Exists(libraryPath))
        {
            var full = Path.GetFullPath(libraryPath);

            loaded = NativeLibrary.Load(full);
            return true;
        }

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

    [DllImport("thrive_native")]
    internal static extern bool SetDebugDrawerCallbacks(OnLineDraw lineDraw, OnTriangleDraw triangleDraw);

    [DllImport("thrive_native")]
    internal static extern void DisableDebugDrawerCallbacks();

    // The wrapper-specific methods are in their respective files like PhysicalWorld.cs etc.
}
