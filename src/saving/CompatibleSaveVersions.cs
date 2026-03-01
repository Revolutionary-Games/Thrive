namespace Saving;

using System.Collections.Generic;
using System.Diagnostics;
using Godot;

/// <summary>
///   With the new archive format and per-object versions, this class holds a list of compatible versions for each
///   Thrive version to allow loading them without scary popups for the player.
/// </summary>
public static class CompatibleSaveVersions
{
    private static readonly Dictionary<string, List<(string CompatibleVersion, bool AllowPrototypes)>>
        KnownVersionCompatibilityMapping = new()
        {
            {
                "1.0.1.0-rc1", [
                    ("0.9.0.0", false), ("0.9.0.1", false),
                    ("0.9.1.0", true), ("0.9.1.1", true),
                    ("0.9.2.0", true), ("1.0.0.0", true),
                    ("1.0.1.0-alpha", true),
                ]
            },
        };

    public static void VerifyInfoForCurrentVersion()
    {
        // Game versions must always be immediately added to the list above!
        if (KnownVersionCompatibilityMapping.ContainsKey(Constants.Version))
        {
#if DEBUG
            foreach (var (compatibleVersion, _) in KnownVersionCompatibilityMapping[Constants.Version])
            {
                if (compatibleVersion != Constants.Version)
                    continue;

                GD.PrintErr("Current version is marked as compatible with itself");
                Debugger.Break();
                SceneManager.Instance.QuitDueToError();
                return;
            }

#endif

            return;
        }

        GD.PrintErr("CURRENT THRIVE VERSION NOT REGISTERED TO COMPATIBLE SAVE VERSIONS!");

        if (Debugger.IsAttached)
            Debugger.Break();

        SceneManager.Instance.QuitDueToError();
    }

    public static bool IsMarkedCompatible(string thriveVersion, bool isPrototypeSave)
    {
        if (!KnownVersionCompatibilityMapping.TryGetValue(Constants.Version, out var versions))
        {
            GD.PrintErr("No known compatible versions found for current Thrive version!");
            return false;
        }

        foreach (var (compatibleVersion, allowPrototypes) in versions)
        {
            if (compatibleVersion != thriveVersion)
                continue;

            // Found a potentially compatible version
            if (isPrototypeSave && !allowPrototypes)
            {
                // But it doesn't allow prototypes
                return false;
            }

#if DEBUG

            if (SaveHelper.IsKnownIncompatible(thriveVersion))
            {
                GD.PrintErr("Save was marked compatible but was found in the explicit incompatibility list!");

                if (Debugger.IsAttached)
                    Debugger.Break();

                return false;
            }
#endif

            // Found a compatible version marker
            return true;
        }

        return false;
    }
}
