using System;
using System.Diagnostics;
using System.Globalization;
using Godot;
using Path = System.IO.Path;

/// <summary>
///   This is the first autoloaded class. Used to perform some actions that should happen
///   as the first things in the game
/// </summary>
public class StartupActions : Node
{
    private bool preventStartup;

    private StartupActions()
    {
        // Print game version
        // TODO: for devbuilds it would be nice to print the hash here
        GD.Print("This is Thrive version: ", Constants.Version, " (see below for exact build info)");

        // Add unhandled exception logger if debugger is not attached
        if (!Debugger.IsAttached)
        {
            GD.UnhandledException += UnhandledExceptionLogger.OnUnhandledException;
            GD.Print("Unhandled exception logger attached");
        }

        GD.Print("Startup C# locale is: ", CultureInfo.CurrentCulture, " Godot locale is: ",
            TranslationServer.GetLocale());

        var userDir = Constants.UserFolderAsNativePath;

        GD.Print("user:// directory is: ", userDir);

        // Print the logs folder to see in the output where they are stored
        GD.Print("Game logs are written to: ", Path.Combine(userDir, Constants.LOGS_FOLDER_NAME),
            " latest log is 'log.txt'");

        // TODO: mono runtime doesn't have intrinsics support for checking AVX
        // https://learn.microsoft.com/fi-fi/dotnet/api/system.runtime.intrinsics.x86.avx?view=net-8.0

        bool loadNative = true;

        try
        {
            if (!LaunchOptions.SkipCPUCheck)
            {
                if (!NativeInterop.CheckCPU())
                {
                    if (Engine.EditorHint)
                    {
                        GD.Print(
                            "Skipping native library load in editor as it is not available (CPU check lib missing)");
                    }
                    else
                    {
                        // Thrive needs SSE4.1, SSE4.2, and AVX (1) currently, this is not told to the player to avoid
                        // confusion with what they are missing
                        GD.Print("Thrive requires a new enough CPU to have various extension instruction sets, " +
                            "see above for what is detected as missing");
                        GD.PrintErr("Detected CPU features are insufficient for running Thrive, a newer CPU with " +
                            "required instruction set extensions is required");
                    }

                    loadNative = false;
                }
                else
                {
                    GD.Print("Checked that required CPU features are present");
                }
            }
            else
            {
                GD.Print("Skipping CPU type check, please do not report any crashes due to illegal CPU " +
                    "instruction problems (as that indicates missing CPU feature this check would test)");
            }

            if (loadNative)
            {
                NativeInterop.Load();
            }
            else if (Engine.EditorHint)
            {
                GD.Print("Skipping native library load in editor as the CPU feature check couldn't pass");
                loadNative = false;
            }
            else
            {
                GD.PrintErr("Thrive will now quit due to required native library requiring a newer processor " +
                    "than is available");
                preventStartup = true;
                SceneManager.NotifyEarlyQuit();
                return;
            }
        }
        catch (Exception e)
        {
            GD.Print($"Thrive native library load failed due to: {e.Message}");

            if (Engine.EditorHint && e is DllNotFoundException)
            {
                loadNative = false;
                GD.Print("Skipping native library load in editor as it is not available");
            }
            else
            {
                GD.PrintErr("Native library is missing (or unloadable). If you downloaded a compiled Thrive " +
                    "version, this version (may be) broken. If you are trying to compile Thrive you need to compile " +
                    "the native modules as well");
                GD.PrintErr("Please do not report to us the next unhandled exception error about this, unless " +
                    "this is an official Thrive release that has this issue");

                if (FeatureInformation.GetOS() == FeatureInformation.PlatformLinux)
                {
                    GD.PrintErr("On Linux please verify you have new enough GLIBC version as otherwise the library " +
                        "is unloadable. Updating your distro to the latest version should resolve the issue as long " +
                        "as you are using a supported distro.");
                }

                GD.PrintErr(e);

                preventStartup = true;
                SceneManager.NotifyEarlyQuit();
                return;
            }
        }

        // Load settings here, to make sure locales etc. are applied to the main loaded and autoloaded scenes
        try
        {
            // We just want to do something to ensure settings instance is fine
            // ReSharper disable once UnusedVariable
            var hashCode = Settings.Instance.GetHashCode();
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to initialize settings: ", e);
        }

        if (loadNative)
        {
            NativeInterop.Init(Settings.Instance);
        }
    }

    public override void _Ready()
    {
        // We need to specifically only access the scene tree after ready is called as otherwise it is just null
        if (preventStartup)
        {
            GD.Print("Preventing startup due to StartupActions failing");
            SceneManager.QuitDueToProblem(this);
        }
    }
}
