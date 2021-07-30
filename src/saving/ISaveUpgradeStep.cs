namespace Saving
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public interface ISaveUpgradeStep
    {
        /// <summary>
        ///   Performs a save upgrade. Throws exceptions on failure.
        /// </summary>
        /// <param name="saveInfo">Loaded save info from inputSave</param>
        /// <param name="inputSave">Save name that can be used to read the save data</param>
        /// <param name="outputSave">Target name of the upgraded save</param>
        /// <returns>The new version of the upgraded save</returns>
        string PerformUpgrade(SaveInformation saveInfo, string inputSave, string outputSave);

        /// <summary>
        ///   Calculates what the version of save would be if PerformUpgrade is ran on it
        /// </summary>
        /// <param name="saveInfo">The save info to inspect</param>
        /// <returns>The version after upgrade, null if can't upgrade</returns>
        string VersionAfterUpgrade(SaveInformation saveInfo);
    }

    public static class SaveUpgradeSteps
    {
        private static readonly Dictionary<string, ISaveUpgradeStep> StoredSaveUpgradeSteps =
            InitializeSaveUpgradeSteps();

        public static IReadOnlyDictionary<string, ISaveUpgradeStep> SupportedUpgrades => StoredSaveUpgradeSteps;

        public static ISaveUpgradeStep GetUpgradeStepForVersion(string version)
        {
            if (!SupportedUpgrades.TryGetValue(version, out ISaveUpgradeStep step))
                return null;

            return step;
        }

        private static Dictionary<string, ISaveUpgradeStep> InitializeSaveUpgradeSteps()
        {
            // TODO: would it be useful to specify a range of versions an upgrader can upgrade to make it less error
            // prone to accidentally miss a version
            return new Dictionary<string, ISaveUpgradeStep>
            {
                { "0.5.4.0", new UpgradeStep054To055() },
            };
        }
    }

    /// <summary>
    ///   Just updates the save version in a save file. Can be used to bring a file up to date if the actual save data
    ///   doesn't need any changes
    /// </summary>
    public class UpgradeJustVersionNumber : ISaveUpgradeStep
    {
        private readonly string versionToSet;

        public UpgradeJustVersionNumber(string versionToSet)
        {
            this.versionToSet = versionToSet;
        }

        public string PerformUpgrade(SaveInformation saveInfo, string inputSave, string outputSave)
        {
            // TODO: would be nice to be able to only partially load the object tree here
            var loadedData = Save.LoadFromFile(inputSave);

            saveInfo.ThriveVersion = versionToSet;
            loadedData.Info.ThriveVersion = versionToSet;

            loadedData.Name = outputSave;
            loadedData.SaveToFile();

            return versionToSet;
        }

        public string VersionAfterUpgrade(SaveInformation saveInfo)
        {
            return versionToSet;
        }
    }

    internal class UpgradeStep054To055 : BaseJSONUpgradeStep
    {
        protected override string VersionAfter => "0.5.5.0-alpha";

        protected override void PerformUpgradeOnJSON(JObject saveData)
        {
            foreach (var entry in saveData.Properties())
            {
                RecursivelyUpdateMembraneValues(entry);
            }
        }

        private void RecursivelyUpdateMembraneValues(JProperty property)
        {
            if (property.Name.Contains("Membrane") || property.Name.Contains("membrane"))
            {
                if (property.Value.Type == JTokenType.String &&
                    property.Value.ToObject<string>() == "calcium_carbonate")
                {
                    GD.Print("Updating value at ", property.Path);

                    // TODO: does this actually stick?
                    property.Value = "calciumCarbonate";
                }
            }

            if (property.Value.Type != JTokenType.Object)
                return;

            var valueObject = property.Value as JObject;
            if (valueObject == null)
                throw new JsonException("Child object convert to object type failed");

            foreach (var entry in valueObject.Properties())
            {
                RecursivelyUpdateMembraneValues(entry);
            }
        }
    }

    internal abstract class BaseJSONUpgradeStep : ISaveUpgradeStep
    {
        protected abstract string VersionAfter { get; }

        public string PerformUpgrade(SaveInformation saveInfo, string inputSave, string outputSave)
        {
            if (VersionUtils.Compare(VersionAfter, saveInfo.ThriveVersion) <= 0)
            {
                throw new ArgumentException("This converter can't upgrade the provided save");
            }

            // SaveInformation is not used here as saveInfo is assumed to be up to date
            var (freshInfo, saveStructure, screenshot) = Save.LoadJSONStructureFromFile(inputSave);

            if (freshInfo.ThriveVersion != saveInfo.ThriveVersion)
                GD.PrintErr("Unexpected save version in freshly loaded save information");

            PerformUpgradeOnJSON(saveStructure);

            PerformUpgradeOnInfo(saveInfo);

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
        }

        protected abstract void PerformUpgradeOnJSON(JObject saveData);
    }
}
