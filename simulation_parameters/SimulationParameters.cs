using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Newtonsoft.Json;
using File = Godot.File;

/// <summary>
///   Contains definitions for global game configuration like Compounds, Organelles etc.
/// </summary>
public class SimulationParameters : Node
{
    private static SimulationParameters? instance;

    private Dictionary<string, MembraneType> membranes = null!;
    private Dictionary<string, Background> backgrounds = null!;
    private Dictionary<string, Biome> biomes = null!;
    private Dictionary<string, BioProcess> bioProcesses = null!;
    private Dictionary<string, Compound> compounds = null!;
    private Dictionary<string, OrganelleDefinition> organelles = null!;
    private Dictionary<string, MusicCategory> musicCategories = null!;
    private Dictionary<string, HelpTexts> helpTexts = null!;
    private AutoEvoConfiguration autoEvoConfiguration = null!;
    private List<NamedInputGroup> inputGroups = null!;
    private Dictionary<string, Gallery> gallery = null!;
    private TranslationsInfo translationsInfo = null!;
    private GameCredits gameCredits = null!;

    // These are for mutations to be able to randomly pick items in a weighted manner
    private List<OrganelleDefinition> prokaryoticOrganelles = null!;
    private float prokaryoticOrganellesTotalChance;
    private List<OrganelleDefinition> eukaryoticOrganelles = null!;
    private float eukaryoticOrganellesChance;

    public static SimulationParameters Instance => instance ?? throw new InstanceNotLoadedYetException();

    public IEnumerable<NamedInputGroup> InputGroups => inputGroups;

    public AutoEvoConfiguration AutoEvoConfiguration => autoEvoConfiguration;

    public NameGenerator NameGenerator { get; private set; } = null!;

    /// <summary>
    ///   Loads the simulation configuration parameters from JSON files
    /// </summary>
    /// <remarks>
    ///   This is now loaded in _Ready as otherwise the <see cref="ModLoader"/>'s _Ready would run after simulation
    ///   parameters are loaded causing some data that might want to be overridden by mods to be loaded too early.
    /// </remarks>
    public override void _Ready()
    {
        base._Ready();

        // Compounds are referenced by the other json files so it is loaded first and instance is assigned here
        instance = this;

        // Loading the compounds needs a custom JSON deserializer that can load the Compound objects, but the loader
        // can't always be active because that breaks saving
        {
            var compoundDeserializer = new JsonConverter[] { new CompoundLoader(null) };

            compounds = LoadRegistry<Compound>(
                "res://simulation_parameters/microbe_stage/compounds.json", compoundDeserializer);
        }

        membranes = LoadRegistry<MembraneType>(
            "res://simulation_parameters/microbe_stage/membranes.json");
        backgrounds = LoadRegistry<Background>(
            "res://simulation_parameters/microbe_stage/backgrounds.json");
        biomes = LoadRegistry<Biome>(
            "res://simulation_parameters/microbe_stage/biomes.json");
        bioProcesses = LoadRegistry<BioProcess>(
            "res://simulation_parameters/microbe_stage/bio_processes.json");
        organelles = LoadRegistry<OrganelleDefinition>(
            "res://simulation_parameters/microbe_stage/organelles.json");

        NameGenerator = LoadDirectObject<NameGenerator>(
            "res://simulation_parameters/microbe_stage/species_names.json");

        musicCategories = LoadRegistry<MusicCategory>("res://simulation_parameters/common/music_tracks.json");

        helpTexts = LoadRegistry<HelpTexts>("res://simulation_parameters/common/help_texts.json");

        inputGroups = LoadListRegistry<NamedInputGroup>("res://simulation_parameters/common/input_options.json");

        autoEvoConfiguration =
            LoadDirectObject<AutoEvoConfiguration>("res://simulation_parameters/common/auto-evo_parameters.json");

        gallery = LoadRegistry<Gallery>("res://simulation_parameters/common/gallery.json");

        translationsInfo =
            LoadDirectObject<TranslationsInfo>("res://simulation_parameters/common/translations_info.json");

        gameCredits =
            LoadDirectObject<GameCredits>("res://simulation_parameters/common/credits.json");

        GD.Print("SimulationParameters loading ended");

        CheckForInvalidValues();
        ResolveValueRelationships();

        // Apply translations here to ensure that initial translations are correct when the game starts.
        // This is done this way to allow StartupActions to run before SimulationParameters are loaded
        ApplyTranslations();

        GD.Print("SimulationParameters are good");
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            ApplyTranslations();
        }
    }

    public OrganelleDefinition GetOrganelleType(string name)
    {
        return organelles[name];
    }

    /// <summary>
    ///   Returns all organelles
    /// </summary>
    public IEnumerable<OrganelleDefinition> GetAllOrganelles()
    {
        return organelles.Values;
    }

    public bool DoesOrganelleExist(string name)
    {
        return organelles.ContainsKey(name);
    }

    public MembraneType GetMembrane(string name)
    {
        return membranes[name];
    }

    public IEnumerable<MembraneType> GetAllMembranes()
    {
        return membranes.Values;
    }

    public bool DoesMembraneExist(string name)
    {
        return membranes.ContainsKey(name);
    }

    public Background GetBackground(string name)
    {
        return backgrounds[name];
    }

    public Biome GetBiome(string name)
    {
        return biomes[name];
    }

    public BioProcess GetBioProcess(string name)
    {
        return bioProcesses[name];
    }

    public Compound GetCompound(string name)
    {
        return compounds[name];
    }

    public bool DoesCompoundExist(string name)
    {
        return compounds.ContainsKey(name);
    }

    /// <summary>
    ///   Returns all compounds that are clouds
    /// </summary>
    public List<Compound> GetCloudCompounds()
    {
        var result = new List<Compound>();

        foreach (var entry in compounds)
        {
            if (entry.Value.IsCloud)
            {
                result.Add(entry.Value);
            }
        }

        return result;
    }

    /// <summary>
    ///   Returns all environmental *molecular* compounds that are dissolved in the environment, i.e. gas.
    /// </summary>
    /// <remarks>This excludes sunlight, and includes O2, CO2, N...</remarks>
    public List<Compound> GetGasCompounds()
    {
        var result = new List<Compound>();

        foreach (var entry in compounds)
        {
            // The ability to be distributed is a distinctive heuristic for molecular compounds
            if (!entry.Value.IsCloud && entry.Value.IsEnvironmental && entry.Value.CanBeDistributed)
            {
                result.Add(entry.Value);
            }
        }

        return result;
    }

    public Dictionary<string, MusicCategory> GetMusicCategories()
    {
        return musicCategories;
    }

    public HelpTexts GetHelpTexts(string name)
    {
        return helpTexts[name];
    }

    public Gallery GetGallery(string name)
    {
        return gallery[name];
    }

    public TranslationsInfo GetTranslationsInfo()
    {
        return translationsInfo;
    }

    public GameCredits GetCredits()
    {
        return gameCredits;
    }

    public OrganelleDefinition GetRandomProkaryoticOrganelle(Random random)
    {
        float valueLeft = random.Next(0.0f, prokaryoticOrganellesTotalChance);

        foreach (var organelle in prokaryoticOrganelles)
        {
            valueLeft -= organelle.ProkaryoteChance;

            if (valueLeft <= 0.00001f)
                return organelle;
        }

        return prokaryoticOrganelles[prokaryoticOrganelles.Count - 1];
    }

    public OrganelleDefinition GetRandomEukaryoticOrganelle(Random random)
    {
        float valueLeft = random.Next(0.0f, eukaryoticOrganellesChance);

        foreach (var organelle in eukaryoticOrganelles)
        {
            valueLeft -= organelle.ChanceToCreate;

            if (valueLeft <= 0.00001f)
                return organelle;
        }

        return eukaryoticOrganelles[eukaryoticOrganelles.Count - 1];
    }

    /// <summary>
    ///   Applies translations to all registry loaded types. Called whenever the locale is changed
    /// </summary>
    public void ApplyTranslations()
    {
        ApplyRegistryObjectTranslations(membranes);
        ApplyRegistryObjectTranslations(backgrounds);
        ApplyRegistryObjectTranslations(biomes);
        ApplyRegistryObjectTranslations(bioProcesses);
        ApplyRegistryObjectTranslations(compounds);
        ApplyRegistryObjectTranslations(organelles);
        ApplyRegistryObjectTranslations(musicCategories);
        ApplyRegistryObjectTranslations(helpTexts);
        ApplyRegistryObjectTranslations(inputGroups);
        ApplyRegistryObjectTranslations(gallery);
    }

    private static void CheckRegistryType<T>(Dictionary<string, T> registry)
        where T : class, IRegistryType
    {
        foreach (var entry in registry)
        {
            entry.Value.InternalName = entry.Key;
            entry.Value.Check(entry.Key);
        }
    }

    private static void CheckRegistryType<T>(IEnumerable<T> registry)
        where T : class, IRegistryType
    {
        foreach (var entry in registry)
        {
            entry.Check(string.Empty);

            if (string.IsNullOrEmpty(entry.InternalName))
                throw new Exception("registry list type should set internal name in Check");
        }
    }

    private static void ApplyRegistryObjectTranslations<T>(Dictionary<string, T> registry)
        where T : class, IRegistryType
    {
        foreach (var entry in registry)
        {
            entry.Value.ApplyTranslations();
        }
    }

    private static void ApplyRegistryObjectTranslations<T>(IEnumerable<T> registry)
        where T : class, IRegistryType
    {
        foreach (var entry in registry)
        {
            entry.ApplyTranslations();
        }
    }

    private static string ReadJSONFile(string path)
    {
        using var file = new File();
        file.Open(path, File.ModeFlags.Read);
        var result = file.GetAsText();

        // This might be completely unnecessary
        file.Close();

        return result;
    }

    private Dictionary<string, T> LoadRegistry<T>(string path, JsonConverter[]? extraConverters = null)
    {
        extraConverters ??= Array.Empty<JsonConverter>();

        var result = JsonConvert.DeserializeObject<Dictionary<string, T>>(ReadJSONFile(path), extraConverters);

        if (result == null)
            throw new InvalidDataException("Could not load a registry from file: " + path);

        GD.Print($"Loaded registry for {typeof(T)} with {result.Count} items");
        return result;
    }

    private List<T> LoadListRegistry<T>(string path, JsonConverter[]? extraConverters = null)
    {
        extraConverters ??= Array.Empty<JsonConverter>();

        var result = JsonConvert.DeserializeObject<List<T>>(ReadJSONFile(path), extraConverters);

        if (result == null)
            throw new InvalidDataException("Could not load a registry from file: " + path);

        GD.Print($"Loaded registry for {typeof(T)} with {result.Count} items");
        return result;
    }

    private T LoadDirectObject<T>(string path, JsonConverter[]? extraConverters = null)
        where T : class
    {
        extraConverters ??= Array.Empty<JsonConverter>();

        var result = JsonConvert.DeserializeObject<T>(ReadJSONFile(path), extraConverters);

        if (result == null)
            throw new InvalidDataException("Could not load a registry from file: " + path);

        return result;
    }

    private void CheckForInvalidValues()
    {
        CheckRegistryType(membranes);
        CheckRegistryType(backgrounds);
        CheckRegistryType(biomes);
        CheckRegistryType(bioProcesses);
        CheckRegistryType(compounds);
        CheckRegistryType(organelles);
        CheckRegistryType(musicCategories);
        CheckRegistryType(helpTexts);
        CheckRegistryType(inputGroups);
        CheckRegistryType(gallery);

        NameGenerator.Check(string.Empty);
        autoEvoConfiguration.Check(string.Empty);
        translationsInfo.Check(string.Empty);
        gameCredits.Check(string.Empty);
    }

    private void ResolveValueRelationships()
    {
        foreach (var entry in organelles)
        {
            entry.Value.Resolve(this);
        }

        foreach (var entry in biomes)
        {
            entry.Value.Resolve(this);
        }

        foreach (var entry in backgrounds)
        {
            entry.Value.Resolve(this);
        }

        foreach (var entry in membranes)
        {
            entry.Value.Resolve();
        }

        foreach (var entry in compounds)
        {
            entry.Value.Resolve();
        }

        foreach (var entry in gallery)
        {
            entry.Value.Resolve();
        }

        NameGenerator.Resolve(this);

        BuildOrganelleChances();

        // TODO: there could also be a check for making sure
        // non-existent compounds, processes etc. are not used
    }

    private void BuildOrganelleChances()
    {
        prokaryoticOrganelles = new List<OrganelleDefinition>();
        eukaryoticOrganelles = new List<OrganelleDefinition>();
        prokaryoticOrganellesTotalChance = 0.0f;
        eukaryoticOrganellesChance = 0.0f;

        foreach (var entry in organelles)
        {
            var organelle = entry.Value;

            if (organelle.ChanceToCreate > 0)
            {
                eukaryoticOrganelles.Add(organelle);
                eukaryoticOrganellesChance += organelle.ChanceToCreate;
            }

            if (organelle.ProkaryoteChance > 0)
            {
                prokaryoticOrganelles.Add(organelle);
                prokaryoticOrganellesTotalChance += organelle.ChanceToCreate;
            }
        }
    }
}
