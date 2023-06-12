using System;
using System.Runtime.InteropServices;
using Godot;

/// <summary>
///   Calling interface from C# to the native code side of things for the native module
/// </summary>
public static class NativeInterop
{
    private static bool loadCalled;
    private static bool debugDrawIsPossible;

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

        NativeMethods.SetLogForwardingCallback(ForwardMessage);

        var result = NativeMethods.InitThriveLibrary();

        if (result != 0)
        {
            throw new InvalidOperationException($"Failed to initialize Thrive native library, code: {result}");
        }

        try
        {
            debugDrawIsPossible = NativeMethods.SetDebugDrawerCallbacks(ForwardLineDraw, ForwardTriangleDraw);
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to initialize potential for debug drawing: ", e);
            debugDrawIsPossible = false;
        }
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
            throw new Exception($"Failed to initialize Thrive native library, unexpected version {version} " +
                $"is not required: {NativeConstants.Version}");
        }

        GD.Print("Loaded native Thrive library version ", version);

        // Enable debug logging if this is being debugged
#if DEBUG
        NativeMethods.SetLogLevel(NativeMethods.LogLevel.Debug);
#endif
    }

    /// <summary>
    ///   Releases all native resources and prepares the library for process exit
    /// </summary>
    public static void Shutdown()
    {
        NativeMethods.DisableDebugDrawerCallbacks();
        NativeMethods.ShutdownThriveLibrary();
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

        NativeMethods.DisableDebugDrawerCallbacks();
    }

    private static void ForwardMessage(IntPtr messageData, int messageLength, NativeMethods.LogLevel level)
    {
        var message = Marshal.PtrToStringAnsi(messageData, messageLength);

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

    [DllImport("thrive_native")]
    internal static extern void ShutdownThriveLibrary();

    [DllImport("thrive_native")]
    internal static extern void SetLogLevel(LogLevel level);

    [DllImport("thrive_native")]
    internal static extern void SetLogForwardingCallback(OnLogMessage callback);

    [DllImport("thrive_native")]
    internal static extern bool SetDebugDrawerCallbacks(OnLineDraw lineDraw, OnTriangleDraw triangleDraw);

    [DllImport("thrive_native")]
    internal static extern void DisableDebugDrawerCallbacks();

    // The wrapper-specific methods are in their respective files like PhysicalWorld.cs etc.
}
