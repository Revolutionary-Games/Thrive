using System.Collections.Generic;
using Godot;
using static FullModDetails;

public static class ModHelpers
{
    /// <summary>
    ///   Checks if the mod is Compatible With Thrive
    /// </summary>
    /// <returns> Returns true if the mod is compatible with this version of thrive or if is unknown. </returns>
    public static bool IsCompatible(this VersionCompatibility versionToCheck)
    {
        return versionToCheck > VersionCompatibility.NotExplicitlyCompatible;
    }

    /// <summary>
    ///   Turns the result from a check into a string of the error and how to fix it
    /// </summary>
    /// <returns> Returns a translated string of the possible error. </returns>
    public static string CheckResultToString(ModListValidationError checkResult,
        List<FullModDetails> list)
    {
        var result = string.Empty;

        // The mod that is causing the error
        ModInfo offendingMod = new ModInfo();
        if (checkResult.CheckedMod is not null)
        {
            offendingMod = checkResult.CheckedMod;
        }
        else
        {
            offendingMod.Name = TranslationServer.Translate("UNKNOWN_MOD");
        }

        // The reason why the mod is causing an error
        ModInfo otherMod = new ModInfo();
        if (checkResult.OtherMod is not null)
        {
            otherMod = checkResult.OtherMod;
        }
        else
        {
            otherMod.Name = TranslationServer.Translate("UNKNOWN_MOD");
        }

        switch ((ModLoader.CheckErrorStatus)checkResult.ErrorType)
        {
            default:
                result = TranslationServer.Translate("MOD_LIST_VALID");
                break;
            case ModLoader.CheckErrorStatus.IncompatibleVersion:
                result += string.Format(TranslationServer.Translate("MOD_ERROR_INCOMPATIBLE_VERSION"),
                    offendingMod.Name);
                break;
            case ModLoader.CheckErrorStatus.DependencyNotFound:
                result += string.Format(TranslationServer.Translate("MOD_ERROR_DEPENDENCIES"), offendingMod.Name,
                    otherMod.Name) + "\n";
                result += TranslationServer.Translate("MOD_ERROR_DEPENDENCIES_FIX");
                break;
            case ModLoader.CheckErrorStatus.RequiredModsNotFound:
                result += string.Format(TranslationServer.Translate("MOD_ERROR_REQUIRED_MODS"), offendingMod.Name,
                    otherMod.Name) + "\n";
                result += string.Format(TranslationServer.Translate("MOD_ERROR_REQUIRED_MODS_FIX"), otherMod.Name);
                break;
            case ModLoader.CheckErrorStatus.InvalidDependencyOrder:
                result += string.Format(TranslationServer.Translate("MOD_ERROR_DEPENDENCIES_ORDER"), offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name) + "\n";
                result += string.Format(TranslationServer.Translate("MOD_ERROR_DEPENDENCIES_ORDER_FIX"),
                    offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name);
                break;
            case ModLoader.CheckErrorStatus.IncompatibleMod:
                result += string.Format(TranslationServer.Translate("MOD_ERROR_INCOMPATIBLE_MOD"), offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name) + "\n";
                result += string.Format(TranslationServer.Translate("MOD_ERROR_INCOMPATIBLE_MOD_FIX"),
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name);
                break;
            case ModLoader.CheckErrorStatus.InvalidLoadOrderBefore:
                result += string.Format(TranslationServer.Translate("MOD_ERROR_LOAD_ORDER_BEFORE"), offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name) + "\n";
                result += string.Format(TranslationServer.Translate("MOD_ERROR_LOAD_ORDER_BEFORE_FIX"),
                    offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name);
                break;
            case ModLoader.CheckErrorStatus.InvalidLoadOrderAfter:
                result += string.Format(TranslationServer.Translate("MOD_ERROR_LOAD_ORDER_AFTER"), offendingMod.Name,
                    otherMod.Name) + "\n";
                result += string.Format(TranslationServer.Translate("MOD_ERROR_LOAD_ORDER_AFTER_FIX"),
                    offendingMod.Name, otherMod.Name);
                break;
        }

        return result;
    }
}
