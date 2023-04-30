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

    public static string FormatAsAList(this List<string>? unformattedList)
    {
        var formattedString = new LocalizedStringBuilder();
        if (unformattedList != null)
        {
            foreach (var currentItem in unformattedList)
            {
                formattedString.Append(new LocalizedString("FORMATTED_LIST_ITEM", currentItem));
                formattedString.Append(System.Environment.NewLine);
            }
        }

        return formattedString.ToString();
    }

    /// <summary>
    ///   Turns the result from a check into a string of the error and how to fix it
    /// </summary>
    /// <returns> Returns a translated string of the possible error. </returns>
    public static string CheckResultToString(ModListValidationError checkResult)
    {
        var result = new LocalizedStringBuilder();

        // The mod that is causing the error
        ModInfo offendingMod = new ModInfo();
        if (checkResult.CheckedMod != null)
        {
            offendingMod = checkResult.CheckedMod;
        }
        else
        {
            offendingMod.Name = TranslationServer.Translate("UNKNOWN_MOD");
        }

        // The reason why the mod is causing an error
        ModInfo otherMod;
        if (checkResult.OtherMod != null)
        {
            otherMod = checkResult.OtherMod;
        }
        else
        {
            otherMod = new ModInfo
            {
                Name = TranslationServer.Translate("UNKNOWN_MOD"),
            };
        }

        switch (checkResult.ErrorType)
        {
            default:
                result.Append(TranslationServer.Translate("MOD_LIST_VALID"));
                break;
            case ModLoader.CheckErrorStatus.IncompatibleVersion:
                result.Append(new LocalizedString("MOD_ERROR_INCOMPATIBLE_VERSION", offendingMod.Name));
                break;
            case ModLoader.CheckErrorStatus.DependencyNotFound:
            {
                result.Append(new LocalizedString("MOD_ERROR_DEPENDENCIES", offendingMod.Name,
                    otherMod.Name));
                result.Append(System.Environment.NewLine);
                result.Append(new LocalizedString("MOD_ERROR_DEPENDENCIES_FIX"));
                break;
            }

            case ModLoader.CheckErrorStatus.RequiredModsNotFound:
            {
                result.Append(new LocalizedString("MOD_ERROR_REQUIRED_MODS", offendingMod.Name,
                    otherMod.Name));
                result.Append(System.Environment.NewLine);
                result.Append(new LocalizedString("MOD_ERROR_REQUIRED_MODS_FIX", otherMod.Name));
                break;
            }

            case ModLoader.CheckErrorStatus.InvalidDependencyOrder:
            {
                result.Append(new LocalizedString("MOD_ERROR_DEPENDENCIES_ORDER", offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name));
                result.Append(System.Environment.NewLine);
                result.Append(new LocalizedString("MOD_ERROR_DEPENDENCIES_ORDER_FIX", offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name));
                break;
            }

            case ModLoader.CheckErrorStatus.IncompatibleMod:
            {
                result.Append(new LocalizedString("MOD_ERROR_INCOMPATIBLE_MOD", offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name));
                result.Append(new LocalizedString("MOD_ERROR_INCOMPATIBLE_MOD_FIX", string.IsNullOrWhiteSpace(otherMod.Name) ?
                    otherMod.InternalName :
                    otherMod.Name));
                break;
            }

            case ModLoader.CheckErrorStatus.InvalidLoadOrderBefore:
            {
                result.Append(new LocalizedString("MOD_ERROR_LOAD_ORDER_BEFORE", offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name));
                result.Append(new LocalizedString("MOD_ERROR_LOAD_ORDER_BEFORE_FIX", offendingMod.Name,
                    string.IsNullOrWhiteSpace(otherMod.Name) ? otherMod.InternalName : otherMod.Name));
                break;
            }

            case ModLoader.CheckErrorStatus.InvalidLoadOrderAfter:
            {
                result.Append(new LocalizedString("MOD_ERROR_LOAD_ORDER_AFTER", offendingMod.Name,
                    otherMod.Name));
                result.Append(new LocalizedString("MOD_ERROR_LOAD_ORDER_AFTER_FIX", offendingMod.Name,
                    otherMod.Name));
                break;
            }
        }

        return result.ToString();
    }
}
