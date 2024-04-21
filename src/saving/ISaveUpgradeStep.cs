namespace Saving;

using System;
using System.Collections.Generic;
using System.Linq;
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
    public string PerformUpgrade(SaveInformation saveInfo, string inputSave, string outputSave);

    /// <summary>
    ///   Calculates what the version of save would be if PerformUpgrade is ran on it
    /// </summary>
    /// <param name="saveInfo">The save info to inspect</param>
    /// <returns>The version after upgrade, null if can't upgrade</returns>
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
            { "0.5.4.0-rc1", new UpgradeJustVersionNumber("0.5.4.0") },
            { "0.5.4.0", new UpgradeStep054To055() },
            { "0.5.5.0-alpha", new UpgradeJustVersionNumber("0.5.5.0-rc1") },
            { "0.5.5.0-rc1", new UpgradeJustVersionNumber("0.5.5.0") },
            { "0.5.5.0", new UpgradeStep055To056() },
            { "0.5.6.0-alpha", new UpgradeJustVersionNumber("0.5.6.0-rc1") },
            { "0.5.6.0-rc1", new UpgradeJustVersionNumber("0.5.6.0") },
            { "0.5.6.0", new UpgradeJustVersionNumber("0.5.6.1") },
            { "0.5.6.1", new UpgradeStep0561To057() },
            { "0.5.7.0-rc1", new UpgradeJustVersionNumber("0.5.7.0") },
            { "0.5.7.0", new UpgradeStep057To058() },
            { "0.5.8.0-alpha", new UpgradeJustVersionNumber("0.5.8.0-rc1") },
            { "0.5.8.0-rc1", new UpgradeJustVersionNumber("0.5.8.0") },
            { "0.5.8.0", new UpgradeJustVersionNumber("0.5.8.1-alpha") },
            { "0.5.8.1-alpha", new UpgradeJustVersionNumber("0.5.8.1") },
            { "0.5.9.0", new UpgradeJustVersionNumber("0.5.10.0-rc1") },
            { "0.5.10.0-rc1", new UpgradeJustVersionNumber("0.5.10.0") },
            { "0.5.10.0", new UpgradeJustVersionNumber("0.6.0.0-rc1") },
            { "0.6.0.0-rc1", new UpgradeJustVersionNumber("0.6.0.0") },
            { "0.6.0.0", new UpgradeJustVersionNumber("0.6.1.0-rc1") },
            { "0.6.1.0-rc1", new UpgradeJustVersionNumber("0.6.1.0") },
            { "0.6.1.0", new UpgradeJustVersionNumber("0.6.2.0-rc1") },
            { "0.6.2.0-rc1", new UpgradeJustVersionNumber("0.6.2.0") },
            { "0.6.2.0", new UpgradeJustVersionNumber("0.6.3.0-rc1") },
            { "0.6.3.0-rc1", new UpgradeJustVersionNumber("0.6.3.0") },
            { "0.6.4.0", new UpgradeJustVersionNumber("0.6.4.1") },
            { "0.6.4.1", new UpgradeStep0641To065() },
            { "0.6.5.0-alpha", new UpgradeJustVersionNumber("0.6.5.0-rc1") },
            { "0.6.5.0-rc1", new UpgradeJustVersionNumber("0.6.5.0") },
        };
    }
}

internal class UpgradeStep054To055 : BaseRecursiveJSONWalkerStep
{
    protected override string VersionAfter => "0.5.5.0-alpha";

    protected override void RecursivelyUpdateObjectProperties(JObject? jObject)
    {
        base.RecursivelyUpdateObjectProperties(jObject);

        foreach (var entry in jObject!.Properties())
        {
            if (entry.Name == "DespawnRadiusSqr")
            {
                // This modifies the object so this is done in a separate loop that is broken when hit
                GD.Print("Updating property name at ", entry.Path);
                entry.Replace(new JProperty("DespawnRadiusSquared", entry.Value));
                break;
            }
        }
    }

    protected override void CheckAndUpdateProperty(JProperty property)
    {
        if (property.Name.Contains("Membrane") || property.Name.Contains("membrane"))
        {
            if (property.Value.Type == JTokenType.String &&
                property.Value.ToObject<string>() == "calcium_carbonate")
            {
                GD.Print("Updating value at ", property.Path);
                property.Value = "calciumCarbonate";
            }
        }
    }
}

internal class UpgradeStep055To056 : BaseRecursiveJSONWalkerStep
{
    private static readonly string[] BehaviouralKeys = { "Aggression", "Opportunism", "Fear", "Activity", "Focus" };

    protected override string VersionAfter => "0.5.6.0-alpha";

    protected override void CheckAndUpdateProperty(JProperty property)
    {
        var children = property.Value.Children<JProperty>();
        var childrenNames = children.Select(c => c.Name);

        if (property.Name != "Behaviour" && BehaviouralKeys.All(p => childrenNames.Contains(p)))
        {
            UpgradeBehaviouralValues(property, children);
        }
    }

    /// <summary>
    ///   Updates the behavioural values. Triggers on a specific species
    /// </summary>
    /// <param name="property">Should be a specific species</param>
    /// <param name="children">The children of the given property</param>
    /// <remarks>
    ///   <para>
    ///     Changes a json like:
    ///     <code>
    ///       "1": {
    ///         ...
    ///         "Aggression": 126.188889,
    ///         "Opportunism": 34.3588943,
    ///         "Fear": 52.6969757,
    ///         "Activity": 74.67135,
    ///         "Focus": 111.778221,
    ///         ...
    ///       }
    ///     </code>
    ///     to
    ///     <code>
    ///       "1": {
    ///         ...
    ///         "Behaviour": {
    ///           "Aggression": 126.188889,
    ///           "Opportunism": 34.3588943,
    ///           "Fear": 52.6969757,
    ///           "Activity": 74.67135,
    ///           "Focus": 111.778221
    ///         },
    ///         ...
    ///       }
    ///     </code>
    ///   </para>
    /// </remarks>
    private void UpgradeBehaviouralValues(JProperty property, JEnumerable<JProperty> children)
    {
        var aggression = children.First(p => p.Name == "Aggression");
        var opportunism = children.First(p => p.Name == "Opportunism");
        var fear = children.First(p => p.Name == "Fear");
        var activity = children.First(p => p.Name == "Activity");
        var focus = children.First(p => p.Name == "Focus");

        aggression.Remove();
        opportunism.Remove();
        fear.Remove();
        activity.Remove();
        focus.Remove();

        ((JObject)property.Value).Add("Behaviour",
            new JObject(aggression, opportunism, fear, activity, focus));
    }
}

internal class UpgradeStep0561To057 : BaseRecursiveJSONWalkerStep
{
    protected override string VersionAfter => "0.5.7.0-rc1";

    protected override void CheckAndUpdateProperty(JProperty property)
    {
        if (property.Name.Contains("GameWorld"))
        {
            ((JObject)property.Value).Add("eventsLog", new JObject());
        }
    }
}

internal class UpgradeStep057To058 : BaseRecursiveJSONWalkerStep
{
    protected override string VersionAfter => "0.5.8.0-alpha";

    protected override void PerformUpgradeOnJSON(JObject saveData)
    {
        base.PerformUpgradeOnJSON(saveData);

        // We are just interested in updating the microbe editor properties (if it is present)
        var editor = saveData.GetValue("MicrobeEditor") as JObject;

        if (editor == null)
            return;

        JToken? GetAndRemoveEditorProperty(string name)
        {
            return GetAndRemoveProperty(editor, name);
        }

        var gui = GetAndRemoveEditorProperty("gui") as JObject;

        if (gui == null)
            throw new JsonException("Expected 'gui' child object on editor object");

        var editedSpecies = editor.GetValue("editedSpecies");

        // Move some properties around so that they are correct with the refactoring
        var reportTab = new JObject
        {
            ["selectedReportSubtab"] = gui.GetValue("selectedReportSubtab"),
        };

        var patchMapTab = new JObject
        {
            ["targetPatch"] = GetAndRemoveEditorProperty("targetPatch"),
            ["canStillMove"] = GetAndRemoveEditorProperty("canStillMove"),
            ["playerPatchOnEntry"] = GetAndRemoveEditorProperty("playerPatchOnEntry"),
        };

        var behaviourEditor = new JObject
        {
            ["Behaviour"] = GetAndRemoveEditorProperty("behaviour"),
            ["editedSpecies"] = editedSpecies,
        };

        var cellEditorTab = new JObject
        {
            ["NewName"] = GetAndRemoveEditorProperty("NewName"),
            ["initialCellSpeed"] = gui.GetValue("initialCellSpeed"),
            ["initialCellSize"] = gui.GetValue("initialCellSize"),
            ["initialCellHp"] = gui.GetValue("initialCellHp"),
            ["MovingPlacedHex"] = GetAndRemoveEditorProperty("MovingOrganelle"),
            ["selectedSelectionMenuTab"] = gui.GetValue("selectedSelectionMenuTab"),
            ["behaviourEditor"] = behaviourEditor,
        };

        MoveObjectProperties(editor, cellEditorTab,
            "colour", "rigidity", "editedMicrobeOrganelles", "placementRotation", "activeActionName",
            "Membrane", "Symmetry", "MicrobePreviewMode");

        // Remove the organelle change callbacks which will be redone anyway on load
        RemoveProperty(cellEditorTab, "editedMicrobeOrganelles.onAdded");
        RemoveProperty(cellEditorTab, "editedMicrobeOrganelles.onRemoved");

        // Fix references to things in the action history which will be removed
        var references = CreateObjectReferenceDatabase((JObject?)editor["history"] ??
            throw new JsonException("editor history object missing"));

        ResolveObjectReferences((JObject?)cellEditorTab["editedMicrobeOrganelles"] ??
            throw new JsonException("edited microbe organelles has disappeared"), references);

        editor["selectedEditorTab"] = gui.GetValue("selectedEditorTab");
        editor["reportTab"] = reportTab;
        editor["patchMapTab"] = patchMapTab;
        editor["cellEditorTab"] = cellEditorTab;

        // Clear out the action history as updating that would be quite a bit of extra effort to code
        editor["history"] = new JObject
        {
            ["actions"] = new JArray(),
            ["actionIndex"] = 0,
        };
    }

    protected override void CheckAndUpdateProperty(JProperty property)
    {
        if (property.Name is "organelles" or "Organelles" or "editedMicrobeOrganelles" &&
            property.Value.Type == JTokenType.Object)
        {
            var asObject = (JObject)property.Value;

            // Organelle layout has Organelles key which we want to update to the new hex layout
            if (asObject.TryGetValue("Organelles", out var organelleList))
            {
                if (organelleList.Type != JTokenType.Array)
                    return;

                asObject.Remove("Organelles");
                asObject["existingHexes"] = organelleList;
            }
        }
    }
}

internal class UpgradeStep0641To065 : BaseRecursiveJSONWalkerStep
{
    /// <summary>
    ///   This refers to <see cref="Patch.Visibility"/>
    /// </summary>
    private const string VISIBILITY = "Visibility";

    protected override string VersionAfter => "0.6.5.0-alpha";

    protected override void CheckAndUpdateProperty(JProperty property)
    {
        // The patch map needs to be updated in older versions to show the entire thing,
        // as 0.6.5 introduces fog-of-war

        // Make patches visible
        if (property.Name == "Patches")
        {
            IEnumerable<JObject> elements;

            if (property.Value is JArray array)
            {
                elements = array.Select(t => (JObject)t);
            }
            else if (property.Value is JObject @object)
            {
                elements = @object.Values().Select(t => (JObject)t);
            }
            else
            {
                return;
            }

            foreach (var element in elements)
            {
                if (!element.ContainsKey(nameof(Patch.BiomeType)))
                    continue;

                element.Add(VISIBILITY, (int)MapElementVisibility.Shown);
            }
        }

        // Make regions visible
        if (property.Name == "Region")
        {
            var dict = (JObject)property.Value;
            dict.Add(VISIBILITY, (int)MapElementVisibility.Shown);
        }
    }
}

internal abstract class BaseRecursiveJSONWalkerStep : BaseJSONUpgradeStep
{
    protected override void PerformUpgradeOnJSON(JObject saveData)
    {
        RecursivelyUpdateObjectProperties(saveData);
    }

    protected virtual void RecursivelyUpdateObjectProperties(JObject? jObject)
    {
        if (jObject == null)
            throw new JsonException("Null JSON object passed to looping properties");

        foreach (var entry in jObject.Properties())
        {
            RecursivelyUpdateValues(entry);
        }

        DetectAndUpdateKeysThatAreJSON(jObject);
    }

    protected virtual void DetectAndUpdateKeysThatAreJSON(JObject jObject)
    {
        foreach (var entry in jObject.Properties().Where(e =>
                     e.Name.StartsWith("{", StringComparison.InvariantCulture) &&
                     e.Name.EndsWith("}", StringComparison.InvariantCulture)).ToList())
        {
            UpdateJSONPropertyKey(entry);
        }
    }

    protected abstract void CheckAndUpdateProperty(JProperty property);

    private void RecursivelyUpdateValues(JProperty property)
    {
        CheckAndUpdateProperty(property);

        if (property.Value.Type == JTokenType.Array)
        {
            var listObject = property.Value as JArray;
            if (listObject == null)
                throw new JsonException("Child array convert to array type failed");

            foreach (var entry in listObject)
            {
                if (entry.Type == JTokenType.Object)
                    RecursivelyUpdateObjectProperties(entry as JObject);
            }
        }

        if (property.Value.Type != JTokenType.Object)
            return;

        var valueObject = property.Value as JObject;
        if (valueObject == null)
            throw new JsonException("Child object convert to object type failed");

        RecursivelyUpdateObjectProperties(valueObject);
    }

    private void UpdateJSONPropertyKey(JProperty property)
    {
        var data = JObject.Parse(property.Name);

        RecursivelyUpdateObjectProperties(data);

        var newData = data.ToString(Formatting.None);

        if (newData != property.Name)
        {
            GD.Print("Updating JSON data in a key at: ", property.Path);
            property.Replace(new JProperty(newData, property.Value));
        }
    }
}

internal abstract class BaseJSONUpgradeStep : ISaveUpgradeStep
{
    protected abstract string VersionAfter { get; }

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
        var (freshInfo, saveStructure, screenshot) = Save.LoadJSONStructureFromFile(inputSave);

        if (freshInfo.ThriveVersion != saveInfo.ThriveVersion)
            GD.PrintErr("Unexpected save version in freshly loaded save information");

        PerformUpgradeOnJSON(saveStructure);

        PerformUpgradeOnInfo(saveInfo);

        CopySaveInfoToStructure(saveStructure, saveInfo);

        // TODO: should the "Name" in saveStructure be updated? (there's a bigger need to update it when making the
        // backup file rather than here...)

        Save.WriteSaveJSONToFile(saveInfo, saveStructure, screenshot, outputSave);

        return VersionAfter;
    }

    public string VersionAfterUpgrade(SaveInformation saveInfo)
    {
        return VersionAfter;
    }

    protected static JToken? GetAndRemoveProperty(JObject @object, string name)
    {
        var result = @object.GetValue(name);

        if (result == null)
            return null;

        @object.Remove(name);
        return result;
    }

    protected static void MoveObjectProperties(JObject from, JObject to, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            to[propertyName] = GetAndRemoveProperty(from, propertyName);
        }
    }

    /// <summary>
    ///   Removes a property multiple levels deep
    /// </summary>
    /// <param name="baseObject">The base object to which the path is relative</param>
    /// <param name="pathToProperty">
    ///   The path to the property to remove, with each level of property names separated by dots, for example
    ///   <code>someProperty.AnotherLevel.FinalProperty</code>
    /// </param>
    /// <param name="errorIfCannotRemove">If true an error is thrown if the property can't be removed</param>
    protected static void RemoveProperty(JObject baseObject, string pathToProperty, bool errorIfCannotRemove = true)
    {
        var pathParts = pathToProperty.Split('.');

        RemoveProperty(baseObject, pathParts, errorIfCannotRemove);
    }

    /// <summary>
    ///   Finds all subobjects in the object tree that specify an ID and collects them
    /// </summary>
    /// <param name="objectTree">The object to start finding the objects with IDs from</param>
    /// <returns>A dictionary mapping IDs to the objects</returns>
    protected static Dictionary<string, JObject> CreateObjectReferenceDatabase(JObject objectTree)
    {
        var result = new Dictionary<string, JObject>();

        BuildObjectTreeIDDatabase(objectTree, result);

        return result;
    }

    /// <summary>
    ///   Detects child objects that are actually references to objects that are known and replaces them with
    ///   the actual objects
    /// </summary>
    /// <param name="objectTree">
    ///   The parent object to start looking for child properties that are object references from
    /// </param>
    /// <param name="knownIds">
    ///   Known IDs that are to be handled. Use <see cref="CreateObjectReferenceDatabase"/> to create this
    /// </param>
    protected static void ResolveObjectReferences(JObject objectTree, Dictionary<string, JObject> knownIds)
    {
        // Find child properties where we can replace references
        foreach (var property in objectTree.Properties().ToList())
        {
            if (property.Value.Type == JTokenType.Object)
            {
                var newObject = GetReferencedObjectIfIsAReference((JObject)property.Value, knownIds);

                if (newObject != null)
                {
                    property.Replace(new JProperty(property.Name, newObject));

                    // TODO: should we recursively handle new objects that are assigned here?
                    // ResolveObjectReferences(newObject, knownIds);
                }
            }
            else if (property.Value.Type == JTokenType.Array)
            {
                var array = (JArray)property.Value;

                for (int i = 0; i < array.Count; ++i)
                {
                    var arrayItem = array[i];

                    if (arrayItem is JObject asObject)
                    {
                        var newObject = GetReferencedObjectIfIsAReference(asObject, knownIds);

                        if (newObject != null)
                        {
                            array.RemoveAt(i);
                            array.Insert(i, newObject);

                            // See TODO comment above which also applies here
                        }
                    }
                }
            }
        }
    }

    protected virtual void PerformUpgradeOnInfo(SaveInformation saveInformation)
    {
        saveInformation.ThriveVersion = VersionAfter;

        // Update the ID of the save as it is in practice save with different content
        saveInformation.ID = Guid.NewGuid();
    }

    protected abstract void PerformUpgradeOnJSON(JObject saveData);

    private static void BuildObjectTreeIDDatabase(JObject currentObject, Dictionary<string, JObject> result)
    {
        // Detect is current an object with an id
        var potentialId = currentObject.GetValue(BaseThriveConverter.ID_PROPERTY);

        if (potentialId != null)
        {
            result.Add(
                potentialId.Value<string>() ?? throw new JsonException("failed to convert object id to a string"),
                currentObject);
        }

        // Recurse to child objects
        foreach (var property in currentObject.Properties())
        {
            if (property.Value.Type == JTokenType.Object)
            {
                BuildObjectTreeIDDatabase((JObject)property.Value, result);
            }
            else if (property.Value.Type == JTokenType.Array)
            {
                foreach (var arrayItem in (JArray)property.Value)
                {
                    if (arrayItem is JObject asObject)
                        BuildObjectTreeIDDatabase(asObject, result);
                }
            }
        }
    }

    private static JObject? GetReferencedObjectIfIsAReference(JObject potentialReference,
        Dictionary<string, JObject> knownIds)
    {
        var reference = potentialReference.GetValue(BaseThriveConverter.REF_PROPERTY);

        if (reference == null)
            return null;

        if (knownIds.TryGetValue(
                reference.Value<string>() ?? throw new JsonException("Ref property conversion to string failed"),
                out var result))
        {
            return result;
        }

        return null;
    }

    private static void RemoveProperty(JObject currentObject, string[] remainingPath, bool errorIfCannotRemove)
    {
        if (remainingPath.Length < 1 || string.IsNullOrEmpty(remainingPath[0]))
            throw new ArgumentException("Zero length path part or no path provided at all");

        if (remainingPath.Length == 1)
        {
            // Remove the key now that we are at the right object
            if (!currentObject.Remove(remainingPath[0]) && errorIfCannotRemove)
            {
                throw new JsonException(
                    $"Cannot delete property '{remainingPath[0]}' as it doesn't exist in the current object");
            }

            return;
        }

        // Recurse deeper as we aren't at the end of the path yet

        // TODO: support array index references

        var nextObject = currentObject[remainingPath[0]] as JObject;

        if (nextObject == null)
            throw new JsonException($"Failed to find next part in path with key '{remainingPath[0]}'");

        RemoveProperty(nextObject, remainingPath.Skip(1).ToArray(), errorIfCannotRemove);
    }

    private void CopySaveInfoToStructure(JObject saveData, SaveInformation saveInfo)
    {
        var info = saveData[nameof(Save.Info)] ?? throw new JsonException("Save is missing info field");

        foreach (var property in BaseThriveConverter.PropertiesOf(saveInfo))
        {
            var value = property.GetValue(saveInfo);

            if (value == null)
            {
                GD.PrintErr($"Save info structure copy failed, property \"{property.Name}\" is null");
                continue;
            }

            info[property.Name] = JToken.FromObject(value);
        }
    }
}

/// <summary>
///   Just updates the save version in a save file. Can be used to bring a file up to date if the actual save data
///   doesn't need any changes
/// </summary>
internal class UpgradeJustVersionNumber : BaseJSONUpgradeStep
{
    public UpgradeJustVersionNumber(string versionToSet)
    {
        VersionAfter = versionToSet;
    }

    protected override string VersionAfter { get; }

    protected override void PerformUpgradeOnJSON(JObject saveData)
    {
        // Nothing is actually needed to be done here as the base class wil already update save info to the new
        // version and copy that to saveData
    }
}
