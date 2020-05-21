using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

public class SimulationParameters
{
    private static readonly SimulationParameters SingletonInstance = new SimulationParameters();

    private Dictionary<string, MembraneType> membranes;
    private Dictionary<string, Background> backgrounds;
    private Dictionary<string, Biome> biomes;
    private Dictionary<string, BioProcess> bioProcesses;
    private Dictionary<string, Compound> compounds;

    private Dictionary<string, OrganelleDefinition> organelles;

    // These are for mutations to be able to randomly pick items in a weighted manner
    private List<OrganelleDefinition> prokaryoticOrganelles;
    private float prokaryoticOrganellesTotalChance;
    private List<OrganelleDefinition> eukaryoticOrganelles;
    private float eukaryoticOrganellesChance;

    private Dictionary<string, MusicCategory> musicCategories;
    private Dictionary<string, HelpTexts> helpTexts;

    static SimulationParameters()
    {
    }

    /// <summary>
    ///   Loads the simulation configuration parameters from JSON files
    /// </summary>
    private SimulationParameters()
    {
        membranes = LoadRegistry<MembraneType>(
            "res://simulation_parameters/microbe_stage/membranes.json");
        backgrounds = LoadRegistry<Background>(
            "res://simulation_parameters/microbe_stage/backgrounds.json");
        biomes = LoadRegistry<Biome>(
            "res://simulation_parameters/microbe_stage/biomes.json");
        bioProcesses = LoadRegistry<BioProcess>(
            "res://simulation_parameters/microbe_stage/bio_processes.json");
        compounds = LoadRegistry<Compound>(
            "res://simulation_parameters/microbe_stage/compounds.json");
        organelles = LoadRegistry<OrganelleDefinition>(
                    "res://simulation_parameters/microbe_stage/organelles.json");

        NameGenerator = LoadDirectObject<NameGenerator>(
            "res://simulation_parameters/microbe_stage/species_names.json");

        EasterEggMessages = LoadDirectObject<EasterEggMessages>(
            "res://simulation_parameters/common/easter_egg_messages.json");

        musicCategories = LoadRegistry<MusicCategory>("res://simulation_parameters/common/music_tracks.json");

        helpTexts = LoadRegistry<HelpTexts>("res://simulation_parameters/common/help_texts.json");

        GD.Print("SimulationParameters loading ended");

        CheckForInvalidValues();
        ResolveValueRelationships();

        GD.Print("SimulationParameters are good");
    }

    public static SimulationParameters Instance
    {
        get
        {
            return SingletonInstance;
        }
    }

    public NameGenerator NameGenerator { get; }

    public EasterEggMessages EasterEggMessages { get; }

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

    public MembraneType GetMembrane(string name)
    {
        return membranes[name];
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

    public Dictionary<string, MusicCategory> GetMusicCategories()
    {
        return musicCategories;
    }

    public HelpTexts GetHelpTexts(string name)
    {
        return helpTexts[name];
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

    private static void CheckRegistryType<T>(Dictionary<string, T> registry)
        where T : class, IRegistryType
    {
        foreach (var entry in registry)
        {
            entry.Value.InternalName = entry.Key;
            entry.Value.Check(entry.Key);
        }
    }

    private static string ReadJSONFile(string path)
    {
        using (var file = new File())
        {
            file.Open(path, File.ModeFlags.Read);
            var result = file.GetAsText();

            // This might be completely unnecessary
            file.Close();

            return result;
        }
    }

    private Dictionary<string, T> LoadRegistry<T>(string path)
    {
        var result = JsonConvert.DeserializeObject<Dictionary<string, T>>(ReadJSONFile(path));

        GD.Print($"Loaded registry for {typeof(T)} with {result.Count} items");
        return result;
    }

    private T LoadDirectObject<T>(string path)
    {
        return JsonConvert.DeserializeObject<T>(ReadJSONFile(path));
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

        NameGenerator.Check(string.Empty);
        EasterEggMessages.Check(string.Empty);
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
            entry.Value.Resolve(this);
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
