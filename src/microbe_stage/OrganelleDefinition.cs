using System.Collections.Generic;
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
    */

    /// <summary>
    ///   User readable name
    /// </summary>
    [TranslateFrom("untranslatedName")]
    public string Name;

    /// <summary>
    ///   A path to a scene to display this organelle with.
    ///   If empty won't have a display model.
    /// </summary>
    public string DisplayScene;

    /// <summary>
    ///   A path to a scene to display this organelle as a corpse chunk.
    ///   Not needed if it is the same as DisplayScene.
    /// </summary>
    public string CorpseChunkScene;

    /// <summary>
    ///   If the root of the display scene is not the MeshInstance this needs to have the relative node path
    /// </summary>
    public string DisplaySceneModelPath;

    /// <summary>
    ///   If this organelle's display scene has animation this needs to be the path to the animation player node
    /// </summary>
    public string DisplaySceneAnimation;

    /// <summary>
    ///   Loaded scene instance to be used when organelle of this type is placed
    /// </summary>
    public PackedScene LoadedScene;

    /// <summary>
    ///   Loaded scene instance to be used when organelle of this type needs to be displayed for a dead microbe
    /// </summary>
    public PackedScene LoadedCorpseChunkScene;

    /// <summary>
    ///   Loaded icon for display in GUIs
    /// </summary>
    public Texture LoadedIcon;

    public float Mass;

    /// <summary>
    ///   The chance this organelle is placed in an eukaryote when applying mutations
    /// </summary>
    public float ChanceToCreate;

    /// <summary>
    ///   Same as ChanceToCreate but for prokaryotes (bacteria)
    /// </summary>
    public float ProkaryoteChance;

    public OrganelleComponentFactoryInfo Components;

    /// <summary>
    ///   Defines the processes this organelle does and their speed multipliers
    /// </summary>
    public Dictionary<string, float> Processes;

    /// <summary>
    ///   List of hexes this organelle occupies
    /// </summary>
    public List<Hex> Hexes;

    /// <summary>
    ///   The compounds this organelle consists of (how many resources
    ///   are needed to duplicate this)
    /// </summary>
    public Dictionary<Compound, float> InitialComposition;

    /// <summary>
    ///   Colour used for ATP production bar
    /// </summary>
    public string ProductionColour;

    /// <summary>
    ///   Colour used for ATP consumption bar
    /// </summary>
    public string ConsumptionColour;

    /// <summary>
    ///   Icon used for the ATP bars
    /// </summary>
    public string IconPath;

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
    ///   Caches the rotated hexes
    /// </summary>
    private Dictionary<int, List<Hex>> rotatedHexesCache = new Dictionary<int, List<Hex>>();

#pragma warning disable 169 // Used through reflection
    private string untranslatedName;
#pragma warning restore 169

    /// <summary>
    ///   The total amount of compounds in InitialComposition
    /// </summary>
    public float OrganelleCost { get; private set; }

    [JsonIgnore]
    public List<IOrganelleComponentFactory> ComponentFactories => Components.Factories;

    [JsonIgnore]
    public List<TweakedProcess> RunnableProcesses { get; private set; }

    [JsonIgnore]
    public int HexCount => Hexes.Count;

    public string InternalName { get; set; }

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
        rotation = rotation % 6;

        if (!rotatedHexesCache.ContainsKey(rotation))
        {
            var rotated = new List<Hex>();

            foreach (var hex in Hexes)
            {
                rotated.Add(Hex.RotateAxialNTimes(hex, rotation));
            }

            rotatedHexesCache[rotation] = rotated;
        }

        return rotatedHexesCache[rotation];
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
    ///   Returns true when this has the specified component
    ///   factory. For example MovementComponentFactory.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The PlacedOrganelle.HasComponent method checks for the
    ///     actual component class this checks for the *factory*
    ///     class.
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
        if (Components == null)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "No components specified");
        }

        Components.Check(name);

        if (Components.Count < 1)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "No components specified");
        }

        if (Mass <= 0.0f)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Mass is unset");
        }

        if (Mass <= 0.0f)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Mass is unset");
        }

        if (string.IsNullOrEmpty(Name))
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Name is not set");
        }

        if (InitialComposition == null || InitialComposition.Count < 1)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "InitialComposition is not set");
        }

        if (Hexes == null || Hexes.Count < 1)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Hexes is empty");
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

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    /// <summary>
    ///   Resolves references to external resources so that during
    ///   runtime they don't need to be looked up
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
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    public override string ToString()
    {
        return Name + " Organelle";
    }

    public class OrganelleComponentFactoryInfo
    {
        public NucleusComponentFactory Nucleus;
        public StorageComponentFactory Storage;
        public AgentVacuoleComponentFactory AgentVacuole;
        public BindingAgentComponentFactory BindingAgent;
        public MovementComponentFactory Movement;
        public PilusComponentFactory Pilus;
        public ChemoreceptorComponentFactory Chemoreceptor;

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
        }
    }
}
