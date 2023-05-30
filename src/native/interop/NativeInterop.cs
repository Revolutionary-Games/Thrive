using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Godot;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Path = System.IO.Path;

/// <summary>
///   Calling interface from C# to the native code side of things for the native module
/// </summary>
public static class NativeInterop
{
    private static bool loadCalled;

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

        int version = NativeMethods.CheckAPIVersion();

        if (version != NativeConstants.Version)
        {
            throw new Exception($"Failed to initialize Thrive native library, unexpected version {version} " +
                $"is not required: {NativeConstants.Version}");
        }

        GD.Print("Loaded native Thrive library version ", version);
    }

    /// <summary>
    ///   Performs any initialization needed by the native library
    /// </summary>
    /// <param name="settings">Current game settings</param>
    public static void Init(Settings settings)
    {
        // Settings are passed as probably in the future something needs to be setup right in the native side of
        // things for the initial settings
        _ = settings;

        var result = NativeMethods.InitThriveLibrary();

        if (result != 0)
        {
            throw new InvalidOperationException($"Failed to initialize Thrive native library, code: {result}");
        }
    }

    /// <summary>
    ///   Releases all native resources and prepares the library for process exit
    /// </summary>
    public static void Shutdown()
    {
        NativeMethods.ShutdownThriveLibrary();
    }
}

internal static class NativeMethods
{
    [DllImport("thrive_native")]
    internal static extern int CheckAPIVersion();

    [DllImport("thrive_native")]
    internal static extern int InitThriveLibrary();

    [DllImport("thrive_native")]
    internal static extern void ShutdownThriveLibrary();
}
