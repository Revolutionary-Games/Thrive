namespace Saving;

using System;
using System.Collections.Generic;
using Godot;

public interface ISaveUpgradeStep
{
    /// <summary>
    ///   Performs a save upgrade. Throws exceptions on failure.
    /// </summary>
    /// <param name="saveInfo">Loaded save info from inputSave</param>
    /// <param name="inputSave">Save name that can be used to read the save data</param>
    /// <param name="outputSave">Target name of the upgraded save</param>
    /// <returns>The new version of the upgraded save</returns>
    public string PerformUpgrade(SaveInformation saveInfo, string inputSave, string outputSave);

    /// <summary>
    ///   Calculates what the version of save would be if PerformUpgrade is run on it
    /// </summary>
    /// <param name="saveInfo">The save info to inspect</param>
    /// <returns>The version after upgrade, null if save can't be upgraded</returns>
    public string VersionAfterUpgrade(SaveInformation saveInfo);
}

public static class SaveUpgradeSteps
{
    private static readonly Dictionary<string, ISaveUpgradeStep> StoredSaveUpgradeSteps =
        InitializeSaveUpgradeSteps();

    public static IReadOnlyDictionary<string, ISaveUpgradeStep> SupportedUpgrades => StoredSaveUpgradeSteps;

    public static ISaveUpgradeStep? GetUpgradeStepForVersion(string version)
    {
        return SupportedUpgrades.GetValueOrDefault(version);
    }

    /// <summary>
    ///   Creates a list of all existing save upgrade steps
    /// </summary>
    /// <returns>The created list</returns>
    /// <remarks>
    ///   <para>
    ///     Also see <see cref="SaveHelper.KnownSaveIncompatibilityPoints"/> to see where the save version known
    ///     incompatibilities are.
    ///   </para>
    /// </remarks>
    private static Dictionary<string, ISaveUpgradeStep> InitializeSaveUpgradeSteps()
    {
        // TODO: would it be useful to specify a range of versions an upgrader can upgrade to make it less error
        // prone to accidentally miss a version
        return new Dictionary<string, ISaveUpgradeStep>
        {
            { "0.8.3.0-rc1", new UpgradeJustVersionNumber("0.8.3.0") },
        };
    }
}

/// <summary>
///   Just updates the save version in a save file. Can be used to bring a file up to date if the actual save data
///   doesn't need any changes.
/// </summary>
internal class UpgradeJustVersionNumber : ISaveUpgradeStep
{
    public UpgradeJustVersionNumber(string versionToSet)
    {
        VersionAfter = versionToSet;
    }

    protected string VersionAfter { get; }

    // TODO: this needs to be redone for archive-based saves
    public string PerformUpgrade(SaveInformation saveInfo, string inputSave, string outputSave)
    {
        var versionDifference = VersionUtils.Compare(VersionAfter, saveInfo.ThriveVersion);

        if (versionDifference == int.MaxValue)
            throw new Exception("Could not compare version in save to version it would be upgraded to");

        if (versionDifference <= 0)
        {
            throw new ArgumentException("This converter can't upgrade the provided save");
        }

        // SaveInformation is not used here as saveInfo is assumed to be up to date
        // TODO: this bit is not updated yet to work with the new save format
        var (freshInfo, saveStructure, screenshot) = Save.LoadJSONStructureFromFile(inputSave);

        if (freshInfo.ThriveVersion != saveInfo.ThriveVersion)
            GD.PrintErr("Unexpected save version in freshly loaded save information");

        PerformUpgradeOnInfo(saveInfo);

        // TODO: does this need to update the info in the main archive? That would get quite difficult.

        Save.WriteSaveJSONToFile(saveInfo, saveStructure, screenshot, outputSave);

        return VersionAfter;
    }

    public string VersionAfterUpgrade(SaveInformation saveInfo)
    {
        return VersionAfter;
    }

    protected virtual void PerformUpgradeOnInfo(SaveInformation saveInformation)
    {
        saveInformation.ThriveVersion = VersionAfter;

        // Update the ID of the save as it is in practice save with different content
        saveInformation.ID = Guid.NewGuid();
    }
}
