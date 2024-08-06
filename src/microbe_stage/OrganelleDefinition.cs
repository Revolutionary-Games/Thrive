using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Saving.Serializers;
using UnlockConstraints;

/// <summary>
///   Definition for a type of an organelle. This is not a placed organelle in a microbe
/// </summary>
/// <remarks>
///   <para>
///     Actual concrete placed organelles are PlacedOrganelle
///     objects. There should be only a single OrganelleTemplate
///     instance in existence for each organelle defined in
///     organelles.json.
///   </para>
/// </remarks>
[TypeConverter($"Saving.Serializers.{nameof(OrganelleDefinitionStringConverter)}")]
#pragma warning disable CA1001 // Owns Godot resource that is fine to stay for the program lifetime
public class OrganelleDefinition : IRegistryType
#pragma warning restore CA1001
{
    /// <summary>
    ///   User readable name
    /// </summary>
    [TranslateFrom(nameof(untranslatedName))]
    public string Name = null!;

    /// <summary>
    ///   When true the graphics for this organelle are positioned externally (i.e. moved to the membrane edge and
    ///   point outside from the cell)
    /// </summary>
    public bool PositionedExternally;

    /// <summary>
    ///   Loaded icon for display in GUIs
    /// </summary>
    [JsonIgnore]
    public Texture2D? LoadedIcon;

    /// <summary>
    ///   Density of this organelle. Note that densities should fall into just a few categories to ensure that cached
    ///   microbe collision shapes can be reused more widely
    /// </summary>
    public float Density = 1000;

    /// <summary>
    ///   How much the density of this organelle contributes. Should be set to 0 for pilus and other organelles that
    ///   have separate physics shapes created for them. Bigger organelles should have larger values to make them
    ///   impact the overall physics mass more. Similarly to <see cref="Density"/> this should also have only a few
    ///   used values among all organelles to make shape caching more effective (as the cache depends on density).
    /// </summary>
    public float RelativeDensityVolume = 1;

    /// <summary>
    ///   The (relative) chance this organelle is placed in an eukaryote when applying mutations or generating random
    ///   species (to do roulette selection).
    /// </summary>
    public float ChanceToCreate;

    /// <summary>
    ///   Same as <see cref="ChanceToCreate"/> but for prokaryotes (bacteria)
    /// </summary>
    public float ProkaryoteChance;

    /// <summary>
    ///   If set to true this part is unimplemented and isn't loadable (and not all properties are required)
    /// </summary>
    public bool Unimplemented;

    /// <summary>
    ///   The group of buttons under which the button to select to place this organelle is put
    /// </summary>
    [JsonRequired]
    public OrganelleGroup EditorButtonGroup = OrganelleGroup.Hidden;

    /// <summary>
    ///   Controls the order of editor organelle selection buttons within a single section. Smaller values are first
    /// </summary>
    public int EditorButtonOrder;

    /// <summary>
    ///   How good organelle is at breaking down iron using siderophore
    /// </summary>
    public int IronBreakdownEfficiency;

    public OrganelleComponentFactoryInfo Components = new();

    /// <summary>
    ///   Lightweight feature tags that this organelle has. This is used for simple features that don't need the full
    ///   features that <see cref="Components"/> provides.
    /// </summary>
    public OrganelleFeatureTag[] FeatureTags = Array.Empty<OrganelleFeatureTag>();

    /// <summary>
    ///   Defines the processes this organelle does and their speed multipliers
    /// </summary>
    public Dictionary<string, float>? Processes;

    /// <summary>
    ///   List of hexes this organelle occupies
    /// </summary>
    public List<Hex> Hexes = null!;

    [JsonProperty(PropertyName = "enzymes")]
    public Dictionary<string, int>? RawEnzymes;

    /// <summary>
    ///   Enzymes contained in this organelle
    /// </summary>
    public Dictionary<Enzyme, int> Enzymes = new();

    /// <summary>
    ///   The compounds this organelle consists of (how many resources are needed to duplicate this)
    /// </summary>
    public Dictionary<Compound, float> InitialComposition = null!;

    /// <summary>
    ///   Colour used for ATP production bar
    /// </summary>
    public string ProductionColour = null!;

    /// <summary>
    ///   Colour used for ATP consumption bar
    /// </summary>
    public string ConsumptionColour = null!;

    /// <summary>
    ///   Icon used for the ATP bars and editor selection buttons. Required if placeable by the player
    /// </summary>
    public string? IconPath;

    /// <summary>
    ///   Cost of placing this organelle in the editor (in mutation points)
    /// </summary>
    public int MPCost;

    /// <summary>
    ///   Controls whether this organelle scales with growth progress (progress towards division and reproduction).
    /// </summary>
    public bool ShouldScale = true;

    /// <summary>
    ///   Flags whether this organelle is exclusive for eukaryotes
    /// </summary>
    public bool RequiresNucleus;

    /// <summary>
    ///   Can this organelle only be placed once
    /// </summary>
    public bool Unique;

    /// <summary>
    ///   Determines whether this organelle appears in LAWK-only games
    /// </summary>
    public bool LAWK = true;

    /// <summary>
    ///   Path to a scene that is used to modify / upgrade the organelle. If not set the organelle is not modifiable.
    /// </summary>
    public string? UpgradeGUI;

    /// <summary>
    ///   If set to true then <see cref="AvailableUpgrades"/> won't be displayed by the default upgrader control, but
    ///   everything must be handled by <see cref="UpgradeGUI"/>.
    /// </summary>
    public bool UpgraderSkipDefaultControls;

    /// <summary>
    ///   The upgrades that are available for this organelle type
    /// </summary>
    public Dictionary<string, AvailableUpgrade> AvailableUpgrades = new();

    /// <summary>
    ///   The possible conditions where a player can unlock this organelle.
    /// </summary>
    public List<ConditionSet>? UnlockConditions;

    /// <summary>
    ///   What organelle does this organelle turn into when doing endosymbiosis. See
    ///   <see cref="MicrobeInternalCalculations.CalculatePossibleEndosymbiontsFromSpecies"/>.
    /// </summary>
    [JsonIgnore]
    public OrganelleDefinition? EndosymbiosisUnlocks;

    /// <summary>
    ///   Caches the rotated hexes
    /// </summary>
    private readonly Dictionary<int, List<Hex>> rotatedHexesCache = new();

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    /// <summary>
    ///   A path to a scene to display this organelle with. If empty won't have a display model.
    /// </summary>
    [JsonProperty]
    private SceneWithModelInfo graphics;

    /// <summary>
    ///   How to display this organelle as a corpse chunk. Not needed if it is the same as <see cref="graphics"/>.
    /// </summary>
    [JsonProperty]
    private SceneWithModelInfo corpseChunkGraphics;

    private LoadedSceneWithModelInfo loadedSceneData;
    private LoadedSceneWithModelInfo loadedCorpseScene;

    [JsonProperty]
    private string? endosymbiosisUnlocks;

    private Vector3 modelOffset;

    public enum OrganelleGroup
    {
        /// <summary>
        ///   Not shown in the GUI, not placeable by the player
        /// </summary>
        Hidden,

        Structural,
        Protein,
        External,
        Organelle,

        /// <summary>
        ///   Only available starting in multicellular
        /// </summary>
        Multicellular,
    }

    /// <summary>
    ///   <see cref="Name"/> without any special characters (line changes etc.)
    /// </summary>
    [JsonIgnore]
    public string NameWithoutSpecialCharacters => Name.Replace('\n', ' ');

    /// <summary>
    ///   The total amount of compounds in InitialComposition
    /// </summary>
    [JsonIgnore]
    public float OrganelleCost { get; private set; }

    [JsonIgnore]
    public List<IOrganelleComponentFactory> ComponentFactories => Components.Factories;

    [JsonIgnore]
    public List<TweakedProcess> RunnableProcesses { get; private set; } = null!;

    [JsonIgnore]
    public int HexCount => Hexes.Count;

    [JsonIgnore]
    public Vector3 ModelOffset => modelOffset;

    public string InternalName { get; set; } = null!;

    // Faster checks for specific components
    public bool HasPilusComponent { get; private set; }
    public bool HasMovementComponent { get; private set; }
    public bool HasCiliaComponent { get; private set; }

    /// <summary>
    ///   True if this is an agent vacuole. Number of agent vacuoles determine how often a cell can shoot toxins.
    /// </summary>
    public bool HasAgentVacuoleComponent { get; private set; }

    public bool HasSlimeJetComponent { get; private set; }

    public bool HasBindingFeature { get; private set; }

    public bool HasSignalingFeature { get; private set; }

    /// <summary>
    ///   True when this organelle is one that uses oxygen as a process input (and is metabolism related). This is
    ///   used to adjust toxin effects that have a distinction between oxygen breathers and others.
    /// </summary>
    public bool IsOxygenMetabolism { get; private set; }

    [JsonIgnore]
    public string UntranslatedName =>
        untranslatedName ?? throw new InvalidOperationException("Translations not initialized");

    /// <summary>
    ///   Gets the visual scene that should be used to represent this organelle (if there is one)
    /// </summary>
    /// <param name="upgrades">
    ///   Some upgrades alter organelle visuals so when upgrades are set for this organelle they should be passed here
    ///   to get the right visuals
    /// </param>
    /// <param name="modelInfo">
    ///   The model info returned like this (as it may be a struct type this can't return a nullable reference without
    ///   boxing)
    /// </param>
    /// <returns>True when this has a scene</returns>
    public bool TryGetGraphicsScene(OrganelleUpgrades? upgrades, out LoadedSceneWithModelInfo modelInfo)
    {
        if (TryGetGraphicsForUpgrade(upgrades, out modelInfo))
        {
            return true;
        }

        if (loadedSceneData.LoadedScene == null!)
        {
            return false;
        }

        modelInfo = loadedSceneData;
        return true;
    }

    public bool TryGetCorpseChunkGraphics(OrganelleUpgrades? upgrades, out LoadedSceneWithModelInfo modelInfo)
    {
        if (TryGetGraphicsForUpgrade(upgrades, out modelInfo))
        {
            return true;
        }

        if (loadedCorpseScene.LoadedScene == null!)
        {
            return false;
        }

        modelInfo = loadedCorpseScene;
        return true;
    }

    public bool ContainsHex(Hex hex)
    {
        foreach (var existingHex in Hexes)
        {
            if (existingHex.Equals(hex))
                return true;
        }

        return false;
    }

    /// <summary>
    ///   Returns The hexes but rotated (rotation is the number of 60 degree rotations)
    /// </summary>
    public IReadOnlyList<Hex> GetRotatedHexes(int rotation)
    {
        // The rotations repeat every 6 steps
        rotation %= 6;

        if (!rotatedHexesCache.TryGetValue(rotation, out var rotated))
        {
            rotated = new List<Hex>();

            foreach (var hex in Hexes)
            {
                rotated.Add(Hex.RotateAxialNTimes(hex, rotation));
            }

            rotatedHexesCache[rotation] = rotated;
        }

        return rotated;
    }

    /// <summary>
    ///   Returns true when this has the specified component factory.
    ///   For example <see cref="MovementComponentFactory"/>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The <see cref="PlacedOrganelle.HasComponent{T}"/> method checks for the actual component class this checks
    ///     for the *factory* class. For performance reasons a few components are available as direct boolean
    ///     properties, if a component you want to check for has such a boolean defined for it, use those instead of
    ///     this general interface.
    ///   </para>
    /// </remarks>
    public bool HasComponentFactory<T>()
        where T : IOrganelleComponentFactory
    {
        foreach (var factory in ComponentFactories)
        {
            if (factory is T)
                return true;
        }

        return false;
    }

    /// <summary>
    ///   Returns true when this has the specified organelle feature tag. These are lightweight alternative markers for
    ///   supported features compared to <see cref="HasComponentFactory{T}"/>
    /// </summary>
    /// <returns>True when this has the feature</returns>
    public bool HasFeatureTag(OrganelleFeatureTag featureTag)
    {
        foreach (var feature in FeatureTags)
        {
            if (featureTag == feature)
                return true;
        }

        return false;
    }

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Name is not set");
        }

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);

        if (Unimplemented)
            return;

        Components.Check(name);

        // Components list is now allowed to be empty as some organelles do not need any components

        if (Density < 100)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Density is unset or unrealistically low");
        }

        if (ProkaryoteChance != 0 && RequiresNucleus)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Prokaryote chance is non-zero but player requires a nucleus to place this");
        }

        if (InitialComposition == null || InitialComposition.Count < 1)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "InitialComposition is not set");
        }

        foreach (var entry in InitialComposition)
        {
            if (entry.Value <= MathUtils.EPSILON)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "InitialComposition has negative or really small value");
            }

            if (!entry.Key.IsCloud)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "InitialComposition has a compound that can't be a cloud");
            }
        }

        if (Hexes == null || Hexes.Count < 1)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Hexes is empty");
        }

        if (string.IsNullOrEmpty(graphics.ScenePath) && string.IsNullOrEmpty(corpseChunkGraphics.ScenePath))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Both DisplayScene and CorpseChunkScene are null");
        }

        // Check for duplicate position hexes
        for (int i = 0; i < Hexes.Count; ++i)
        {
            bool duplicate = false;

            for (int j = i + 1; j < Hexes.Count; ++j)
            {
                if (Hexes[i].Equals(Hexes[j]))
                {
                    duplicate = true;
                    break;
                }
            }

            if (duplicate)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Duplicate hex position");
            }
        }

        foreach (var availableUpgrade in AvailableUpgrades)
        {
            availableUpgrade.Value.InternalName = availableUpgrade.Key;
            availableUpgrade.Value.Check(availableUpgrade.Key);

            if ((availableUpgrade.Key == Constants.ORGANELLE_UPGRADE_SPECIAL_NONE &&
                    !availableUpgrade.Value.IsDefault) ||
                (availableUpgrade.Key != Constants.ORGANELLE_UPGRADE_SPECIAL_NONE && availableUpgrade.Value.IsDefault))
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Default upgrade must be named 'none', and the name must not be used by other upgrades");
            }
        }

        // Fail with multiple default upgrades
        if (AvailableUpgrades.Values.Count(u => u.IsDefault) > 1)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Multiple default upgrades specified");
        }

        if (UpgraderSkipDefaultControls && string.IsNullOrEmpty(UpgradeGUI))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Upgrader scene is required when default upgrade controls are suppressed");
        }

        // Check unlock conditions
        if (UnlockConditions != null)
        {
            foreach (var set in UnlockConditions)
                set.Check(name);
        }

#if DEBUG
        if (!string.IsNullOrEmpty(corpseChunkGraphics.ScenePath))
        {
            if (!ResourceLoader.Exists(corpseChunkGraphics.ScenePath))
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Corpse chunk scene path doesn't exist");
            }
        }
#endif
    }

    /// <summary>
    ///   Resolves references to external resources so that during runtime they don't need to be looked up
    /// </summary>
    public void Resolve(SimulationParameters parameters)
    {
        CalculateModelOffset();

        IsOxygenMetabolism = false;

        RunnableProcesses = new List<TweakedProcess>();

        // Preload the scene for instantiating in microbes
        // TODO: switch this to only load when loading the microbe stage to not load this in the future when we have
        // playable stages that don't need these graphics
        if (!string.IsNullOrEmpty(graphics.ScenePath))
        {
            loadedSceneData.LoadFrom(graphics);
        }

        if (!string.IsNullOrEmpty(corpseChunkGraphics.ScenePath))
        {
            loadedCorpseScene.LoadFrom(corpseChunkGraphics);
        }
        else
        {
            // Use default values from the primary scene
            loadedCorpseScene = loadedSceneData;
        }

        if (!string.IsNullOrEmpty(IconPath))
        {
            LoadedIcon = GD.Load<Texture2D>(IconPath);
        }

        // Resolve process names
        if (Processes != null)
        {
            var oxygen = parameters.GetCompound("oxygen");

            foreach (var process in Processes)
            {
                var resolvedProcess = new TweakedProcess(parameters.GetBioProcess(process.Key),
                    process.Value);

                if (process.Value <= 0)
                {
                    throw new InvalidRegistryDataException(InternalName, nameof(OrganelleDefinition),
                        "Process speed value should be above 0");
                }

                if (resolvedProcess.Process.IsMetabolismProcess && ProcessUsesOxygen(resolvedProcess, oxygen))
                    IsOxygenMetabolism = true;

                RunnableProcesses.Add(resolvedProcess);
            }
        }

        // Resolve enzymes from strings to Enzyme objects
        if (RawEnzymes != null)
        {
            foreach (var entry in RawEnzymes)
            {
                var enzyme = parameters.GetEnzyme(entry.Key);

                Enzymes[enzyme] = entry.Value;
            }
        }

        // Resolve unlock conditions
        if (UnlockConditions != null)
        {
            foreach (var set in UnlockConditions)
                set.Resolve(parameters);
        }

        // Resolve endosymbiosis data
        if (!string.IsNullOrEmpty(endosymbiosisUnlocks))
        {
            EndosymbiosisUnlocks = parameters.GetOrganelleType(endosymbiosisUnlocks);
        }

        if (Unimplemented)
            return;

        // Compute total cost from the initial composition
        OrganelleCost = 0;

        foreach (var entry in InitialComposition)
        {
            OrganelleCost += entry.Value;
        }

        // Precompute rotations
        for (int i = 0; i < 6; ++i)
        {
            GetRotatedHexes(i);
        }

        ComputeFactoryCache();

        foreach (var availableUpgrade in AvailableUpgrades.Values)
        {
            availableUpgrade.Resolve();
        }
    }

    /// <summary>
    ///   A bbcode string containing all the unlock conditions for this organelle.
    /// </summary>
    public void GenerateUnlockRequirementsText(LocalizedStringBuilder builder,
        WorldAndPlayerDataSource worldAndPlayerArgs)
    {
        if (UnlockConditions != null)
        {
            bool first = true;
            foreach (var unlockCondition in UnlockConditions)
            {
                if (!first)
                {
                    builder.Append(" ");
                    builder.Append(new LocalizedString("OR_UNLOCK_CONDITION"));
                    builder.Append(" ");
                }

                unlockCondition.GenerateTooltip(builder, worldAndPlayerArgs);
                first = false;
            }
        }
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);

        foreach (var availableUpgrade in AvailableUpgrades.Values)
        {
            availableUpgrade.ApplyTranslations();
        }
    }

    public override string ToString()
    {
        return Name + " Organelle";
    }

    private void ComputeFactoryCache()
    {
        HasPilusComponent = HasFeatureTag(OrganelleFeatureTag.Pilus);
        HasMovementComponent = HasComponentFactory<MovementComponentFactory>();
        HasCiliaComponent = HasComponentFactory<CiliaComponentFactory>();
        HasAgentVacuoleComponent = HasComponentFactory<AgentVacuoleComponentFactory>();
        HasSlimeJetComponent = HasComponentFactory<SlimeJetComponentFactory>();

        HasBindingFeature = HasFeatureTag(OrganelleFeatureTag.BindingAgent);
        HasSignalingFeature = HasFeatureTag(OrganelleFeatureTag.SignalingAgent);
    }

    private void CalculateModelOffset()
    {
        var temp = CalculateCenterOffset();
        temp /= HexCount;
        modelOffset = temp * Constants.DEFAULT_HEX_SIZE;
    }

    private Vector3 CalculateCenterOffset()
    {
        var offset = new Vector3(0, 0, 0);

        foreach (var hex in Hexes)
        {
            offset += Hex.AxialToCartesian(hex);
        }

        offset /= Hexes.Count;
        return offset;
    }

    private bool TryGetGraphicsForUpgrade(OrganelleUpgrades? upgrades, out LoadedSceneWithModelInfo upgradeScene)
    {
        if (upgrades == null)
        {
            upgradeScene = default(LoadedSceneWithModelInfo);

            return false;
        }

        foreach (var availableUpgrade in AvailableUpgrades)
        {
            if (upgrades.UnlockedFeatures.Contains(availableUpgrade.Key))
            {
                if (availableUpgrade.Value.TryGetGraphicsScene(out upgradeScene))
                {
                    return true;
                }
            }
        }

        upgradeScene = default(LoadedSceneWithModelInfo);

        return false;
    }

    private bool ProcessUsesOxygen(TweakedProcess resolvedProcess, Compound oxygen)
    {
        foreach (var processInput in resolvedProcess.Process.Inputs)
        {
            if (processInput.Key == oxygen)
                return true;
        }

        return false;
    }

    public class OrganelleComponentFactoryInfo
    {
        public StorageComponentFactory? Storage;
        public AgentVacuoleComponentFactory? AgentVacuole;
        public MovementComponentFactory? Movement;
        public SlimeJetComponentFactory? SlimeJet;
        public ChemoreceptorComponentFactory? Chemoreceptor;
        public CiliaComponentFactory? Cilia;
        public LysosomeComponentFactory? Lysosome;

        private readonly List<IOrganelleComponentFactory> allFactories = new();

        [JsonIgnore]
        private int count = -1;

        /// <summary>
        ///   The number of components
        /// </summary>
        public int Count => count;

        public List<IOrganelleComponentFactory> Factories => allFactories;

        /// <summary>
        ///   Checks and initializes the factory data
        /// </summary>
        public void Check(string name)
        {
            count = 0;

            if (Storage != null)
            {
                Storage.Check(name);
                allFactories.Add(Storage);
                ++count;
            }

            if (AgentVacuole != null)
            {
                AgentVacuole.Check(name);
                allFactories.Add(AgentVacuole);
                ++count;
            }

            if (Movement != null)
            {
                Movement.Check(name);
                allFactories.Add(Movement);
                ++count;
            }

            if (SlimeJet != null)
            {
                SlimeJet.Check(name);
                allFactories.Add(SlimeJet);
                ++count;
            }

            if (Chemoreceptor != null)
            {
                Chemoreceptor.Check(name);
                allFactories.Add(Chemoreceptor);
                ++count;
            }

            if (Cilia != null)
            {
                Cilia.Check(name);
                allFactories.Add(Cilia);
                ++count;
            }

            if (Lysosome != null)
            {
                Lysosome.Check(name);
                allFactories.Add(Lysosome);
                ++count;
            }
        }
    }
}
