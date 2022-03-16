using System.Collections.Generic;
using Godot;
using static FullModDetails;

public static class ModHelpers
{
    /// <summary>
    ///   Checks if the mod is Compatible With Thrive
    /// </summary>
    /// <returns> Returns true if the mod is compatible with this version of thrive or if is unknown. </returns>
    public static bool IsCompatible(this VersionCompatibility compatibleVersions)
    {
        return compatibleVersions > VersionCompatibility.NotExplicitlyCompatible;
    }

    /// <summary>
    ///   Turns the result from a check into a string of the error and how to fix it
    /// </summary>
    /// <returns> Returns a translated string of the possible error. </returns>
    public static string CheckResultToString((int ErrorType, int ModIndex, int OtherModIndex) checkResult,
        List<FullModDetails> list)
    {
        var result = string.Empty;

        // The mod that is causing the error
        ModInfo offendingMod = new ModInfo();
        if (checkResult.ModIndex >= 0)
        {
            offendingMod = list[checkResult.ModIndex].Info;
        }
        else
        {
            offendingMod.Name = TranslationServer.Translate("UNKNOWN_MOD");
        }

        // The reason why the mod is causing an error
        ModInfo otherMod = new ModInfo();
        if (checkResult.OtherModIndex >= 0)
        {
            otherMod = list[checkResult.OtherModIndex].Info;
        }
        else
        {
            otherMod.Name = TranslationServer.Translate("UNKNOWN_MOD");
        }

        switch (checkResult.ErrorType)
        {
            default:
                result = TranslationServer.Translate("MOD_LIST_VALID");
                break;
            case (int)ModLoader.CheckErrorStatus.IncompatibleVersion:
                result += string.Format(TranslationServer.Translate("MOD_ERROR_INCOMPATIBLE_VERSION"),
                    offendingMod.Name);
                break;
            case (int)ModLoader.CheckErrorStatus.DependencyNotFound:
                string? otherModName = string.Empty;
                if (checkResult.OtherModIndex <= (offendingMod.Dependencies?.Count ?? default(int)))
                {
                    if (offendingMod.Dependencies != null)
                    {
                        otherModName = offendingMod.Dependencies[checkResult.OtherModIndex];
                    }
                }
                else
                {
                    otherModName = TranslationServer.Translate("UNKNOWN_MOD");
                }

                result += string.Format(TranslationServer.Translate("MOD_ERROR_DEPENDENCIES"), offendingMod.Name,
                    otherModName) + "\n";
                result += TranslationServer.Translate("MOD_ERROR_DEPENDENCIES_FIX");
                break;
            case (int)ModLoader.CheckErrorStatus.RequiredModsNotFound:
                if (checkResult.OtherModIndex <= (offendingMod.RequiredMods?.Count ?? default(int)))
                {
                    if (offendingMod.RequiredMods != null)
                    {
                        otherModName = offendingMod.RequiredMods[checkResult.OtherModIndex];
                    }
                    else
                    {
                        otherModName = TranslationServer.Translate("UNKNOWN_MOD");
                    }
                }
                else
                {
                    otherModName = TranslationServer.Translate("UNKNOWN_MOD");
                }

                result += string.Format(TranslationServer.Translate("MOD_ERROR_REQUIRED_MODS"), offendingMod.Name,
                    otherModName) + "\n";
                result += TranslationServer.Translate("MOD_ERROR_REQUIRED_MODS_FIX");
                break;
            case (int)ModLoader.CheckErrorStatus.InvalidDependencyOrder:
                result += string.Format(TranslationServer.Translate("MOD_ERROR_DEPENDENCIES_ORDER"), offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name) + "\n";
                result += string.Format(TranslationServer.Translate("MOD_ERROR_DEPENDENCIES_ORDER_FIX"),
                    offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name);
                break;
            case (int)ModLoader.CheckErrorStatus.IncompatibleMod:
                result += string.Format(TranslationServer.Translate("MOD_ERROR_INCOMPATIBLE_MOD"), offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name) + "\n";
                result += string.Format(TranslationServer.Translate("MOD_ERROR_INCOMPATIBLE_MOD_FIX"),
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name);
                break;
            case (int)ModLoader.CheckErrorStatus.InvalidLoadOrderBefore:
                result += string.Format(TranslationServer.Translate("MOD_ERROR_LOAD_ORDER_BEFORE"), offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name) + "\n";
                result += string.Format(TranslationServer.Translate("MOD_ERROR_LOAD_ORDER_BEFORE_FIX"),
                    offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name);
                break;
            case (int)ModLoader.CheckErrorStatus.InvalidLoadOrderAfter:
                result += string.Format(TranslationServer.Translate("MOD_ERROR_LOAD_ORDER_AFTER"), offendingMod.Name,
                    otherMod.Name) + "\n";
                result += string.Format(TranslationServer.Translate("MOD_ERROR_LOAD_ORDER_AFTER_FIX"),
                    offendingMod.Name, otherMod.Name);
                break;
        }

        return result;
    }

    public static VersionCompatibility GetVersionCompatibility(ModInfo info)
    {
        var isCompatibleVersion = 0;
        var compatibleVersionTest = 0;

        if (!string.IsNullOrEmpty(info.MinimumThriveVersion) &&
            VersionUtils.Compare(Constants.Version, info.MinimumThriveVersion ?? string.Empty) >= 0)
        {
            ++compatibleVersionTest;
        }
        else if (!string.IsNullOrEmpty(info.MinimumThriveVersion))
        {
            compatibleVersionTest--;
        }

        if (!string.IsNullOrEmpty(info.MaximumThriveVersion) &&
            VersionUtils.Compare(Constants.Version, info.MaximumThriveVersion ?? string.Empty) >= 0)
        {
            ++isCompatibleVersion;
        }
        else if (!string.IsNullOrEmpty(info.MaximumThriveVersion))
        {
            compatibleVersionTest--;
        }

        if (compatibleVersionTest >= 1)
        {
            isCompatibleVersion = 1;
        }
        else if (compatibleVersionTest == 0)
        {
            isCompatibleVersion = -1;
        }
        else
        {
            isCompatibleVersion = -2;
        }

        return (VersionCompatibility)isCompatibleVersion;
    }
}
