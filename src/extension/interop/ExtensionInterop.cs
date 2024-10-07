using System;
using System.Runtime.InteropServices;
using Godot;

/// <summary>
///   Interop handling for the GDExtension for Thrive. In contrast <see cref="NativeInterop"/> handles the non-Godot
///   using native library.
/// </summary>
public static class ExtensionInterop
{
    private static readonly Lazy<bool> ExtensionAvailability = new(() => ClassDB.CanInstantiate("ThriveConfig"));

    private static readonly Lazy<GodotObject> ThriveExtension =
        new(() => ClassDB.Instantiate("ThriveConfig").AsGodotObject() ?? throw new NullReferenceException());

    private static IntPtr nativeConfigInstance;

    private static bool shutdown;

    /// <summary>
    ///   Checks if the Thrive GDExtension has been loaded by Godot and is ready for use
    /// </summary>
    /// <returns>True if extension is available</returns>
    public static bool IsExtensionAvailable()
    {
        return ExtensionAvailability.Value;
    }

    public static bool LoadExtension()
    {
        if (shutdown)
            throw new InvalidOperationException("Cannot load extensions after shutdown");

        if (!IsExtensionAvailable())
        {
            GD.PrintErr("Cannot load Thrive extension as it is not available");
            return false;
        }

        GodotObject godotObject;

        try
        {
            godotObject = ThriveExtension.Value;
        }
        catch (Exception e)
        {
            GD.PrintErr("Cannot load Thrive extension: " + e.Message);
            return false;
        }

        // Need to fetch the intercommunication from the other native module to pass to this one
        var intercommunication = NativeInterop.GetIntercommunication(out var version);

        var sanityCheck = godotObject.Call("ReportOtherVersions", NativeConstants.ExtensionVersion, version);

        if (!sanityCheck.AsBool())
        {
            GD.PrintErr("Cannot load Thrive extension due to mismatching file versions");
            return false;
        }

        var result = godotObject.Call("Initialize", intercommunication);

        var resultPtr = new IntPtr(result.AsInt64());

        if (resultPtr == IntPtr.Zero)
        {
            GD.PrintErr("Thrive GDExtension initialization call failed");
            return false;
        }

        // We now know the native instance we can call methods on
        nativeConfigInstance = resultPtr;

        return true;
    }

    public static bool ShutdownExtension()
    {
        if (!ThriveExtension.IsValueCreated || nativeConfigInstance == IntPtr.Zero)
            return true;

        if (shutdown)
            return true;

        // No longer allow calling to the extension
        nativeConfigInstance = IntPtr.Zero;
        shutdown = true;

        var result = ThriveExtension.Value.Call("Shutdown");

        if (!result.AsBool())
        {
            GD.PrintErr("Failed to shutdown Thrive extension");
            return false;
        }

        // Delete the native side object after shutdown
        ThriveExtension.Value.Free();

        return true;
    }

    public static int GetVersion()
    {
        if (shutdown || nativeConfigInstance == IntPtr.Zero)
        {
            GD.PrintErr("Thrive extension is not available");
            return -1;
        }

        return NativeMethods.ExtensionGetVersion(nativeConfigInstance);
    }
}

/// <summary>
///   Native methods specifically from the Thrive GDExtension binary
/// </summary>
internal static partial class NativeMethods
{
    [DllImport("thrive_extension")]
    internal static extern int ExtensionGetVersion(IntPtr thriveConfig);

    [DllImport("thrive_extension")]
    internal static extern int Unwrap(float texelSize, IntPtr mesh);
}
