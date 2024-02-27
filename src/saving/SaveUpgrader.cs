using System;
using Godot;
using Saving;

/// <summary>
///   Allows upgrading older saves to newer versions when there are save upgrade actions programmed
/// </summary>
public static class SaveUpgrader
{
    public static bool CanUpgradeSaveToVersion(SaveInformation saveInfo, string? targetVersion = null)
    {
        targetVersion ??= Constants.Version;

        if (saveInfo.ThriveVersion == targetVersion)
            throw new ArgumentException("Can't upgrade a save that is already at current version");

        // A bit hacky, but we need the SaveInformation to pass to VersionAfterUpgrade so we need to update it
        // so we store the original data here and restore it after the operations that modify it
        var savedVersion = saveInfo.ThriveVersion;

        // Try to find a path from saveInfo.ThriveVersion to targetVersion using available converters
        var nextStep = FindPathToVersion(saveInfo, saveInfo.ThriveVersion, targetVersion);

        saveInfo.ThriveVersion = savedVersion;

        return nextStep != null;
    }

    public static void PerformSaveUpgrade(string saveToUpgrade, bool backup = true, string? targetVersion = null)
    {
        var saveInfo = Save.LoadJustInfoFromSave(saveToUpgrade);

        PerformSaveUpgrade(saveInfo, saveToUpgrade, backup, targetVersion);
    }

    public static void PerformSaveUpgrade(SaveInformation saveInfo, string inputSave, bool backup = true,
        string? targetVersion = null)
    {
        targetVersion ??= Constants.Version;

        if (saveInfo.ThriveVersion == targetVersion)
            throw new ArgumentException("Can't upgrade a save that is already at current version");

        string fromSave;
        string toSave;

        if (backup)
        {
            if (IsSaveABackup(inputSave))
            {
                // Already backed up file
                fromSave = inputSave;
                toSave = RemoveBackupSuffix(inputSave);
            }
            else
            {
                toSave = inputSave;
                fromSave = inputSave.Remove(
                        inputSave.IndexOf(Constants.SAVE_EXTENSION_WITH_DOT, StringComparison.InvariantCulture),
                        Constants.SAVE_EXTENSION_WITH_DOT.Length) +
                    Constants.SAVE_BACKUP_SUFFIX;

                if (DirAccess.RenameAbsolute(SaveFileInfo.SaveNameToPath(toSave), SaveFileInfo.SaveNameToPath(fromSave)) !=
                    Error.Ok)
                {
                    throw new Exception("Failed to rename save to backup name");
                }
            }
        }
        else
        {
            GD.Print("Not making a backup before upgrading a save");
            fromSave = toSave = inputSave;
        }

        var fromVersion = saveInfo.ThriveVersion;
        GD.Print("Beginning save upgrade from version ", fromVersion, " from save: ", fromSave, " to save: ", toSave);

        while (true)
        {
            // Here we also need to do this hacky store thing
            fromVersion = saveInfo.ThriveVersion;
            var nextStep = FindPathToVersion(saveInfo, fromVersion, targetVersion);

            saveInfo.ThriveVersion = fromVersion;

            if (nextStep == null)
            {
                GD.Print("Upgrading finished, no steps remain");
                break;
            }

            var upgradedVersion = nextStep.PerformUpgrade(saveInfo, fromSave, toSave);
            GD.Print("Performed upgrade step from ", fromVersion, " to ", upgradedVersion);

            if (saveInfo.ThriveVersion != upgradedVersion)
                throw new Exception("Save info version not upgraded as expected");

            // After the first step the target save is upgraded in-place so we need to overwrite the old source
            fromSave = toSave;
        }
    }

    public static bool IsSaveABackup(string saveName)
    {
        return Constants.BackupRegex.IsMatch(saveName);
    }

    public static string RemoveBackupSuffix(string saveName)
    {
        return saveName.Remove(saveName.IndexOf(Constants.SAVE_BACKUP_SUFFIX, StringComparison.InvariantCulture),
            Constants.SAVE_BACKUP_SUFFIX.Length) + Constants.SAVE_EXTENSION_WITH_DOT;
    }

    private static ISaveUpgradeStep? FindPathToVersion(SaveInformation saveInfo, string fromVersion, string toVersion)
    {
        var step = SaveUpgradeSteps.GetUpgradeStepForVersion(fromVersion);

        if (step == null)
            return null;

        saveInfo.ThriveVersion = fromVersion;
        var nextVersion = step.VersionAfterUpgrade(saveInfo);

        // Stop if found target version
        if (VersionUtils.Compare(nextVersion, toVersion) >= 0)
            return step;

        // Otherwise verify that there exists steps until the toVersion
        if (FindPathToVersion(saveInfo, nextVersion, toVersion) == null)
        {
            // No further path found
            return null;
        }

        return step;
    }
}
