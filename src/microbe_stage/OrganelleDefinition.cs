using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

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
public class OrganelleDefinition : IRegistryType
{
    // TODO: split the following comment to the actual properties in this class:
    /*
    Organelle attributes:
    mass:   How heavy an organelle is. Affects speed, mostly.

    mpCost: The cost (in mutation points) an organelle costs in the
    microbe editor.

    mesh:   The name of the mesh file of the organelle.
    It has to be in the models folder.

    texture: The name of the texture file to use

    hexes:  A table of the hexes that the organelle occupies.

    chanceToCreate: The (relative) chance this organelle will appear in a
    randomly generated or mutated microbe (to do roulette selection).

    prokaryoteChance: The (relative) chance this organelle will appear in a
    randomly generated or mutated prokaryotes (to do roulette selection).

    processes:  A table with all the processes this organelle does,
    and the capacity of the process

    upgradeGUI:  path to a scene that is used to modify / upgrade the organelle. If not set the organelle is not
    modifiable
    */

    /// <summary>
    ///   User readable name
    /// </summary>
    [TranslateFrom(nameof(untranslatedName))]
    public string Name = null!;

    /// <summary>
    ///   A path to a scene to display this organelle with. If empty won't have a display model.
    /// </summary>
    public string? DisplayScene;

    /// <summary>
    ///   A path to a scene to display this organelle as a corpse chunk. Not needed if it is the same as DisplayScene.
    /// </summary>
    public string? CorpseChunkScene;

    /// <summary>
    ///   If the root of the display scene is not the MeshInstance this needs to have the relative node path
    /// </summary>
    public string? DisplaySceneModelPath;

    /// <summary>
    ///   If this organelle's display scene has animation this needs to be the path to the animation player node
    /// </summary>
    public string? DisplaySceneAnimation;

    /// <summary>
    ///   Loaded scene instance to be used when organelle of this type is placed
    /// </summary>
    [JsonIgnore]
    public PackedScene? LoadedScene;

    /// <summary>
    ///   Loaded scene instance to be used when organelle of this type needs to be displayed for a dead microbe
    /// </summary>
    [JsonIgnore]
    public PackedScene? LoadedCorpseChunkScene;

    /// <summary>
    ///   Loaded icon for display in GUIs
    /// </summary>
    [JsonIgnore]
    public Texture? LoadedIcon;

    public float Mass;

    /// <summary>
    ///   The chance this organelle is placed in an eukaryote when applying mutations
    /// </summary>
    public float ChanceToCreate;

    /// <summary>
    ///   Same as ChanceToCreate but for prokaryotes (bacteria)
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

    [JsonRequired]
    public OrganelleComponentFactoryInfo Components = null!;

    /// <summary>
    ///   Defines the processes this organelle does and their speed multipliers
    /// </summary>
    public Dictionary<string, float>? Processes;

    /// <summary>
    ///   List of hexes this organelle occupies
    /// </summary>
    public List<Hex> Hexes = null!;

    public Dictionary<string, int>? Enzymes;

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
    ///   Cost of placing this organelle in the editor
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
    ///   Path to a scene that is used to modify / upgrade the organelle. If not set the organelle is not modifiable
    /// </summary>
    public string? UpgradeGUI;

    /// <summary>
    ///   The upgrades that are available for this organelle type
    /// </summary>
    public Dictionary<string, AvailableUpgrade> AvailableUpgrades = new();

    /// <summary>
    ///   Caches the rotated hexes
    /// </summary>
    private readonly Dictionary<int, List<Hex>> rotatedHexesCache = new();

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

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

    public string InternalName { get; set; } = null!;

    // Faster checks for specific components
    public bool HasPilusComponent { get; private set; }
    public bool HasMovementComponent { get; private set; }
    public bool HasCiliaComponent { get; private set; }

    [JsonIgnore]
    public string UntranslatedName =>
        untranslatedName ?? throw new InvalidOperationException("Translations not initialized");

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
    public IEnumerable<Hex> GetRotatedHexes(int rotation)
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

    public Vector3 CalculateCenterOffset()
    {
        var offset = new Vector3(0, 0, 0);

        foreach (var hex in Hexes)
        {
            offset += Hex.AxialToCartesian(hex);
        }

        offset /= Hexes.Count;
        return offset;
    }

    public Vector3 CalculateModelOffset()
    {
        var temp = CalculateCenterOffset();
        temp /= HexCount;
        return temp * Constants.DEFAULT_HEX_SIZE;
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

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Name is not set");
        }

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);

        if (Unimplemented)
            return;

        if (Components == null)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "No components specified");
        }

        Components.Check(name);

        if (Components.Count < 1)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "No components specified");
        }

        if (Mass <= 0.0f)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Mass is unset");
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

        if (Hexes == null || Hexes.Count < 1)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Hexes is empty");
        }

        if (string.IsNullOrEmpty(DisplayScene) && string.IsNullOrEmpty(CorpseChunkScene))
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
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Multiple default upgrades specified");
        }
    }

    /// <summary>
    ///   Resolves references to external resources so that during runtime they don't need to be looked up
    /// </summary>
    public void Resolve(SimulationParameters parameters)
    {
        RunnableProcesses = new List<TweakedProcess>();

        // Preload the scene for instantiating in microbes
        if (!string.IsNullOrEmpty(DisplayScene))
        {
            LoadedScene = GD.Load<PackedScene>(DisplayScene);
        }

        if (!string.IsNullOrEmpty(CorpseChunkScene))
        {
            LoadedCorpseChunkScene = GD.Load<PackedScene>(CorpseChunkScene);
        }

        if (!string.IsNullOrEmpty(IconPath))
        {
            LoadedIcon = GD.Load<Texture>(IconPath);
        }

        // Resolve process names
        if (Processes != null)
        {
            foreach (var process in Processes)
            {
                RunnableProcesses.Add(new TweakedProcess(parameters.GetBioProcess(process.Key),
                    process.Value));
            }
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
        HasPilusComponent = HasComponentFactory<PilusComponentFactory>();
        HasMovementComponent = HasComponentFactory<MovementComponentFactory>();
        HasCiliaComponent = HasComponentFactory<CiliaComponentFactory>();
    }

    public class OrganelleComponentFactoryInfo
    {
        public NucleusComponentFactory? Nucleus;
        public StorageComponentFactory? Storage;
        public AgentVacuoleComponentFactory? AgentVacuole;
        public BindingAgentComponentFactory? BindingAgent;
        public MovementComponentFactory? Movement;
        public SlimeJetComponentFactory? SlimeJet;
        public PilusComponentFactory? Pilus;
        public ChemoreceptorComponentFactory? Chemoreceptor;
        public SignalingAgentComponentFactory? SignalingAgent;
        public CiliaComponentFactory? Cilia;
        public LysosomeComponentFactory? Lysosome;
        public AxonComponentFactory? Axon;
        public MyofibrilComponentFactory? Myofibril;

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

            if (Nucleus != null)
            {
                Nucleus.Check(name);
                allFactories.Add(Nucleus);
                ++count;
            }

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

            if (BindingAgent != null)
            {
                BindingAgent.Check(name);
                allFactories.Add(BindingAgent);
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

            if (Pilus != null)
            {
                Pilus.Check(name);
                allFactories.Add(Pilus);
                ++count;
            }

            if (Chemoreceptor != null)
            {
                Chemoreceptor.Check(name);
                allFactories.Add(Chemoreceptor);
                ++count;
            }

            if (SignalingAgent != null)
            {
                SignalingAgent.Check(name);
                allFactories.Add(SignalingAgent);
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

            if (Axon != null)
            {
                Axon.Check(name);
                allFactories.Add(Axon);
                ++count;
            }

            if (Myofibril != null)
            {
                Myofibril.Check(name);
                allFactories.Add(Myofibril);
                ++count;
            }
        }
    }
}
