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

    public static bool IsSuccessful(this ModListValidationError resultToCheck)
    {
        return resultToCheck.ErrorType < 0;
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

        switch (checkResult.ErrorType)
        {
            default:
                result = TranslationServer.Translate("MOD_LIST_VALID");
                break;
            case ModLoader.CheckErrorStatus.IncompatibleVersion:
                result += TranslationServer.Translate("MOD_ERROR_INCOMPATIBLE_VERSION").FormatSafe(offendingMod.Name);
                break;
            case ModLoader.CheckErrorStatus.DependencyNotFound:
            {
                result += TranslationServer.Translate("MOD_ERROR_DEPENDENCIES").FormatSafe(offendingMod.Name,
                    otherMod.Name) + "\n";
                result += TranslationServer.Translate("MOD_ERROR_DEPENDENCIES_FIX");
                break;
            }

            case ModLoader.CheckErrorStatus.RequiredModsNotFound:
            {
                result += TranslationServer.Translate("MOD_ERROR_REQUIRED_MODS").FormatSafe(offendingMod.Name,
                    otherMod.Name) + "\n";
                result += TranslationServer.Translate("MOD_ERROR_REQUIRED_MODS_FIX").FormatSafe(otherMod.Name);
                break;
            }

            case ModLoader.CheckErrorStatus.InvalidDependencyOrder:
            {
                result += TranslationServer.Translate("MOD_ERROR_DEPENDENCIES_ORDER").FormatSafe(offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name) + "\n";
                result += TranslationServer.Translate("MOD_ERROR_DEPENDENCIES_ORDER_FIX").FormatSafe(offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name);
                break;
            }

            case ModLoader.CheckErrorStatus.IncompatibleMod:
            {
                result += TranslationServer.Translate("MOD_ERROR_INCOMPATIBLE_MOD").FormatSafe(offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name) + "\n";
                result += TranslationServer.Translate("MOD_ERROR_INCOMPATIBLE_MOD_FIX")
                    .FormatSafe(string.IsNullOrWhiteSpace(otherMod.Name) ?
                        otherMod.InternalName :
                        otherMod.Name);
                break;
            }

            case ModLoader.CheckErrorStatus.InvalidLoadOrderBefore:
            {
                result += TranslationServer.Translate("MOD_ERROR_LOAD_ORDER_BEFORE").FormatSafe(offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name) + "\n";
                result += TranslationServer.Translate("MOD_ERROR_LOAD_ORDER_BEFORE_FIX").FormatSafe(offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name);
                break;
            }

            case ModLoader.CheckErrorStatus.InvalidLoadOrderAfter:
            {
                result += TranslationServer.Translate("MOD_ERROR_LOAD_ORDER_AFTER").FormatSafe(offendingMod.Name,
                    otherMod.Name) + "\n";
                result += TranslationServer.Translate("MOD_ERROR_LOAD_ORDER_AFTER_FIX")
                    .FormatSafe(offendingMod.Name,
                        otherMod.Name);
                break;
            }
        }

        return result;
    }
}
