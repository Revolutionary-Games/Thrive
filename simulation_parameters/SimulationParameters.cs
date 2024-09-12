using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using ThriveScriptsShared;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using FileAccess = Godot.FileAccess;

/// <summary>
///   Contains definitions for global game configuration like Compounds, Organelles etc.
/// </summary>
[GodotAutoload]
public partial class SimulationParameters : Node
{
    public const string AUTO_EVO_CONFIGURATION_NAME = "AutoEvoConfiguration";
    public const string DAY_NIGHT_CYCLE_NAME = "DayNightConfiguration";

    private static SimulationParameters? instance;

    /// <summary>
    ///   Holds direct addressing for compound data by index of <see cref="Compound"/>. For access by name see
    ///   <see cref="compounds"/>. This is a plain list to make it a very cheap operation to get the compound data.
    /// </summary>
    private readonly List<CompoundDefinition> compoundDefinitions = [];

    private Dictionary<string, MembraneType> membranes = null!;
    private Dictionary<string, Background> backgrounds = null!;
    private Dictionary<string, Biome> biomes = null!;
    private Dictionary<string, BioProcess> bioProcesses = null!;
    private Dictionary<string, CompoundDefinition> compounds = null!;
    private Dictionary<string, OrganelleDefinition> organelles = null!;
    private Dictionary<string, Enzyme> enzymes = null!;
    private Dictionary<string, MusicCategory> musicCategories = null!;
    private Dictionary<string, HelpTexts> helpTexts = null!;
    private PredefinedAutoEvoConfiguration autoEvoConfiguration = null!;
    private List<NamedInputGroup> inputGroups = null!;
    private Dictionary<string, Gallery> gallery = null!;
    private TranslationInfoLocaleChecking translationsInfo = null!;
    private GameCredits gameCredits = null!;
    private GameWiki gameWiki = null!;
    private DayNightConfiguration lightCycle = null!;
    private Dictionary<string, DifficultyPreset> difficultyPresets = null!;
    private Dictionary<string, ScreenEffect> screenEffects = null!;
    private BuildInfo? buildInfo;
    private Dictionary<string, VersionPatchNotes> oldVersionNotes = null!;
    private Dictionary<string, VersionPatchNotes> newerVersionNotes = null!;
    private Dictionary<string, WorldResource> worldResources = null!;
    private Dictionary<string, EquipmentDefinition> equipment = null!;
    private Dictionary<string, CraftingRecipe> craftingRecipes = null!;
    private Dictionary<string, StructureDefinition> structures = null!;
    private Dictionary<string, UnitType> unitTypes = null!;
    private Dictionary<string, SpaceStructureDefinition> spaceStructures = null!;
    private Dictionary<string, Technology> technologies = null!;
    private Dictionary<string, VisualResourceData> visualResources = null!;
    private Dictionary<VisualResourceIdentifier, VisualResourceData> visualResourceByIdentifier = null!;

    private List<Compound>? cachedCloudCompounds;
    private List<Enzyme>? cachedDigestiveEnzymes;

    public static SimulationParameters Instance => instance ?? throw new InstanceNotLoadedYetException();

    public IEnumerable<NamedInputGroup> InputGroups => inputGroups;

    public IAutoEvoConfiguration AutoEvoConfiguration => autoEvoConfiguration;

    public NameGenerator NameGenerator { get; private set; } = null!;
    public PatchMapNameGenerator PatchMapNameGenerator { get; private set; } = null!;

    public static CompoundDefinition GetCompound(Compound compoundId)
    {
#if DEBUG
        try
        {
#endif

            return instance!.GetCompoundDefinition(compoundId);
#if DEBUG
        }
        catch (NullReferenceException e)
        {
            throw new InstanceNotLoadedYetException(e);
        }
#endif
    }

    /// <summary>
    ///   Loads the simulation configuration parameters from JSON files
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is now loaded in _Ready as otherwise the <see cref="ModLoader"/>'s _Ready would run after simulation
    ///     parameters are loaded causing some data that might want to be overridden by mods to be loaded too early.
    ///   </para>
    /// </remarks>
    public override void _Ready()
    {
        base._Ready();

        if (Engine.IsEditorHint())
            return;

        // Compounds are referenced by the other json files so it is loaded first and instance is assigned here
        instance = this;

        // Loading specific registry types need a custom JSON deserializer that can load their respective objects, but
        // the loader can't always be active because that breaks saving (as when saving we want to serialize as
        // identifiers)
        {
            var deserializers = new JsonConverter[]
            {
                new DirectTypeLoadOverride(typeof(Enzyme), null),
                new DirectTypeLoadOverride(typeof(Biome), null),
            };

            // Compounds are used by later stuff so it has to be in here when it otherwise wouldn't need to be here
            compounds = LoadRegistry<CompoundDefinition>("res://simulation_parameters/microbe_stage/compounds.json",
                deserializers);

            PostProcessCompounds();

            enzymes = LoadRegistry<Enzyme>("res://simulation_parameters/microbe_stage/enzymes.json", deserializers);
            biomes = LoadRegistry<Biome>("res://simulation_parameters/microbe_stage/biomes.json", deserializers);

            // These later things already depend on the earlier things so another phase of direct loaders are needed

            deserializers =
            [
                new DirectTypeLoadOverride(typeof(OrganelleDefinition), null),
            ];

            organelles = LoadRegistry<OrganelleDefinition>("res://simulation_parameters/microbe_stage/organelles.json",
                deserializers);
        }

        membranes = LoadRegistry<MembraneType>("res://simulation_parameters/microbe_stage/membranes.json");
        backgrounds = LoadRegistry<Background>("res://simulation_parameters/microbe_stage/backgrounds.json");
        bioProcesses = LoadRegistry<BioProcess>("res://simulation_parameters/microbe_stage/bio_processes.json");

        NameGenerator = LoadDirectObject<NameGenerator>("res://simulation_parameters/microbe_stage/species_names.json");

        musicCategories = LoadRegistry<MusicCategory>("res://simulation_parameters/common/music_tracks.json");

        helpTexts = LoadRegistry<HelpTexts>("res://simulation_parameters/common/help_texts.json");

        inputGroups = LoadListRegistry<NamedInputGroup>("res://simulation_parameters/common/input_options.json");

        autoEvoConfiguration =
            LoadDirectObject<PredefinedAutoEvoConfiguration>(
                "res://simulation_parameters/common/auto-evo_parameters.json");

        gallery = LoadRegistry<Gallery>("res://simulation_parameters/common/gallery.json");

        translationsInfo =
            LoadDirectObject<TranslationInfoLocaleChecking>(ThriveScriptConstants.TRANSLATIONS_PROGRESS_RES);

        gameCredits =
            LoadDirectObject<GameCredits>("res://simulation_parameters/common/credits.json");

        gameWiki =
            LoadDirectObject<GameWiki>("res://simulation_parameters/common/wiki.json");

        lightCycle =
            LoadDirectObject<DayNightConfiguration>("res://simulation_parameters/common/day_night_cycle.json");

        difficultyPresets =
            LoadRegistry<DifficultyPreset>("res://simulation_parameters/common/difficulty_presets.json");

        screenEffects =
            LoadRegistry<ScreenEffect>("res://simulation_parameters/common/screen_effects.json");

        PatchMapNameGenerator = LoadDirectObject<PatchMapNameGenerator>(
            "res://simulation_parameters/microbe_stage/patch_syllables.json");

        oldVersionNotes = LoadRegistry<VersionPatchNotes>("res://simulation_parameters/common/older_patch_notes.json");

        newerVersionNotes =
            LoadYamlFile<Dictionary<string, VersionPatchNotes>>("res://simulation_parameters/common/patch_notes.yml");

        worldResources =
            LoadRegistry<WorldResource>("res://simulation_parameters/awakening_stage/world_resources.json",
                [new DirectTypeLoadOverride(typeof(WorldResource), null)]);

        equipment =
            LoadRegistry<EquipmentDefinition>("res://simulation_parameters/awakening_stage/equipment.json",
                [new DirectTypeLoadOverride(typeof(EquipmentDefinition), null)]);

        craftingRecipes =
            LoadRegistry<CraftingRecipe>("res://simulation_parameters/awakening_stage/crafting_recipes.json",
                [new DirectTypeLoadOverride(typeof(CraftingRecipe), null)]);

        structures =
            LoadRegistry<StructureDefinition>("res://simulation_parameters/awakening_stage/structures.json",
                [new DirectTypeLoadOverride(typeof(StructureDefinition), null)]);

        unitTypes =
            LoadRegistry<UnitType>("res://simulation_parameters/industrial_stage/units.json",
                [new DirectTypeLoadOverride(typeof(UnitType), null)]);

        spaceStructures =
            LoadRegistry<SpaceStructureDefinition>("res://simulation_parameters/space_stage/space_structures.json",
                [new DirectTypeLoadOverride(typeof(SpaceStructureDefinition), null)]);

        technologies =
            LoadRegistry<Technology>("res://simulation_parameters/awakening_stage/technologies.json");

        visualResources =
            LoadRegistry<VisualResourceData>("res://simulation_parameters/common/visual_resources.json");

        // Build info is only loaded if the file is present
        if (FileAccess.FileExists(ThriveScriptConstants.BUILD_INFO_RES))
        {
            buildInfo = LoadDirectObject<BuildInfo>(ThriveScriptConstants.BUILD_INFO_RES);
        }

        // ReSharper disable HeuristicUnreachableCode
#pragma warning disable CS0162
        if (Constants.VERBOSE_SIMULATION_PARAMETER_LOADING)
            GD.Print("SimulationParameters loading ended");

        // ReSharper restore HeuristicUnreachableCode
#pragma warning restore CS0162

        CheckForInvalidValues();
        ResolveValueRelationships();

        // Apply translations here to ensure that initial translations are correct when the game starts.
        // This is done this way to allow StartupActions to run before SimulationParameters are loaded
        ApplyTranslations();

        GD.Print("SimulationParameters are good");
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Localization.Instance.OnTranslationsChanged += ApplyTranslations;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Localization.Instance.OnTranslationsChanged -= ApplyTranslations;
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
        return compounds[name].ID;
    }

    public CompoundDefinition GetCompoundDefinition(Compound compoundId)
    {
        if (compoundId == 0)
        {
            throw new ArgumentException("Cannot get compound definition for invalid compound (0)");
        }

        var index = (int)compoundId;

        try
        {
            return compoundDefinitions[index];
        }
        catch (Exception e)
        {
            throw new ArgumentException("Compound definition index is out of range", e);
        }
    }

    public CompoundDefinition GetCompoundDefinition(string name)
    {
        return compounds[name];
    }

    public CompoundDefinition GetCompoundCaseInsensitive(string name)
    {
        foreach (var entry in compounds)
        {
            if (StringComparer.OrdinalIgnoreCase.Compare(entry.Key, name) == 0)
                return entry.Value;
        }

        throw new KeyNotFoundException("Compound definition not found: " + name);
    }

    public Dictionary<string, CompoundDefinition> GetAllCompounds()
    {
        return compounds;
    }

    public bool DoesCompoundExist(string name)
    {
        return compounds.ContainsKey(name);
    }

    public Enzyme GetEnzyme(string name)
    {
        return enzymes[name];
    }

    public IEnumerable<Enzyme> GetAllEnzymes()
    {
        return enzymes.Values;
    }

    public bool DoesEnzymeExist(string name)
    {
        return enzymes.ContainsKey(name);
    }

    /// <summary>
    ///   Returns all compounds that are clouds (cloud compounds are the ones that exist in the environment as fluid
    ///   simulated "clouds" for microbes to hoover up)
    /// </summary>
    /// <returns>A readonly list with all the cloud compounds</returns>
    public IReadOnlyList<Compound> GetCloudCompounds()
    {
        return cachedCloudCompounds ??= ComputeCloudCompounds();
    }

    public IReadOnlyList<Enzyme> GetHydrolyticEnzymes()
    {
        return cachedDigestiveEnzymes ??= ComputeHydrolyticEnzymes();
    }

    public IReadOnlyDictionary<string, MusicCategory> GetMusicCategories()
    {
        return musicCategories;
    }

    public HelpTexts GetHelpTexts(string name)
    {
        return helpTexts[name];
    }

    public IReadOnlyDictionary<string, Gallery> GetGalleries()
    {
        return gallery;
    }

    public Gallery GetGallery(string name)
    {
        return gallery[name];
    }

    public bool DoesGalleryExist(string name)
    {
        return gallery.ContainsKey(name);
    }

    public TranslationsInfo GetTranslationsInfo()
    {
        return translationsInfo;
    }

    public GameCredits GetCredits()
    {
        return gameCredits;
    }

    public GameWiki GetWiki()
    {
        return gameWiki;
    }

    public DayNightConfiguration GetDayNightCycleConfiguration()
    {
        return lightCycle;
    }

    public DifficultyPreset GetDifficultyPreset(string name)
    {
        return difficultyPresets[name];
    }

    public DifficultyPreset GetDifficultyPresetByIndex(int index)
    {
        return difficultyPresets.Values.First(p => p.Index == index);
    }

    public IEnumerable<DifficultyPreset> GetAllDifficultyPresets()
    {
        return difficultyPresets.Values;
    }

    public ScreenEffect GetScreenEffect(string name)
    {
        return screenEffects[name];
    }

    public ScreenEffect GetScreenEffectByIndex(int index)
    {
        return screenEffects.Values.First(p => p.Index == index);
    }

    public IEnumerable<ScreenEffect> GetAllScreenEffects()
    {
        return screenEffects.Values;
    }

    public PatchMapNameGenerator GetPatchMapNameGenerator()
    {
        return PatchMapNameGenerator;
    }

    public BuildInfo? GetBuildInfoIfExists()
    {
        return buildInfo;
    }

    /// <summary>
    ///   Returns all the known patch notes data
    /// </summary>
    /// <returns>Enumerable of the patch notes, this needs to be ordered from the oldest to the newest</returns>
    public IEnumerable<KeyValuePair<string, VersionPatchNotes>> GetPatchNotes()
    {
        foreach (var note in oldVersionNotes)
            yield return note;

        foreach (var note in newerVersionNotes)
            yield return note;
    }

    public WorldResource GetWorldResource(string name)
    {
        return worldResources[name];
    }

    public bool DoesWorldResourceExist(string name)
    {
        return worldResources.ContainsKey(name);
    }

    public EquipmentDefinition GetBaseEquipmentDefinition(string name)
    {
        return equipment[name];
    }

    public CraftingRecipe GetCraftingRecipe(string name)
    {
        return craftingRecipes[name];
    }

    public StructureDefinition GetStructure(string name)
    {
        return structures[name];
    }

    public UnitType GetUnitType(string name)
    {
        return unitTypes[name];
    }

    public SpaceStructureDefinition GetSpaceStructure(string name)
    {
        return spaceStructures[name];
    }

    public Technology GetTechnology(string name)
    {
        return technologies[name];
    }

    public IEnumerable<Technology> GetTechnologies()
    {
        return technologies.Values;
    }

    public VisualResourceData GetVisualResource(VisualResourceIdentifier identifier)
    {
        if (visualResourceByIdentifier.TryGetValue(identifier, out var result))
            return result;

        GD.PrintErr("Visual resource doesn't exist: ", (long)identifier);
        return GetErrorVisual();
    }

    public VisualResourceData GetVisualResource(string internalName)
    {
        if (visualResources.TryGetValue(internalName, out var result))
            return result;

        GD.PrintErr("Visual resource internal name doesn't exist: ", internalName);
        return GetErrorVisual();
    }

    public VisualResourceData GetErrorVisual()
    {
        return visualResourceByIdentifier[VisualResourceIdentifier.Error];
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
        ApplyRegistryObjectTranslations(enzymes);
        ApplyRegistryObjectTranslations(musicCategories);
        ApplyRegistryObjectTranslations(helpTexts);
        ApplyRegistryObjectTranslations(inputGroups);
        ApplyRegistryObjectTranslations(gallery);
        ApplyRegistryObjectTranslations(difficultyPresets);
        ApplyRegistryObjectTranslations(screenEffects);
        ApplyRegistryObjectTranslations(oldVersionNotes);
        ApplyRegistryObjectTranslations(newerVersionNotes);
        ApplyRegistryObjectTranslations(worldResources);
        ApplyRegistryObjectTranslations(equipment);
        ApplyRegistryObjectTranslations(craftingRecipes);
        ApplyRegistryObjectTranslations(structures);
        ApplyRegistryObjectTranslations(unitTypes);
        ApplyRegistryObjectTranslations(spaceStructures);
        ApplyRegistryObjectTranslations(technologies);
        ApplyRegistryObjectTranslations(visualResources);
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
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var result = file.GetAsText();

        // This might be completely unnecessary
        file.Close();

        if (string.IsNullOrEmpty(result))
            throw new IOException($"Failed to read registry file: {path}");

        return result;
    }

    private static JsonSerializerSettings GetJSONSettings(JsonConverter[]? extraConverters = null)
    {
        var settings = new JsonSerializerSettings();

        if (extraConverters != null)
        {
            settings.Converters = extraConverters;
        }

        return settings;
    }

    private Dictionary<string, T> LoadRegistry<T>(string path, JsonConverter[]? extraConverters = null)
    {
        var result = JsonConvert.DeserializeObject<Dictionary<string, T>>(ReadJSONFile(path),
            GetJSONSettings(extraConverters));

        if (result == null)
            throw new InvalidDataException("Could not load a registry from file: " + path);

        // ReSharper disable HeuristicUnreachableCode
#pragma warning disable CS0162
        if (Constants.VERBOSE_SIMULATION_PARAMETER_LOADING)

            GD.Print($"Loaded registry for {typeof(T)} with {result.Count} items");

        // ReSharper restore HeuristicUnreachableCode
#pragma warning restore CS0162

        return result;
    }

    private T LoadYamlFile<T>(string path)
    {
        var deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();

        var result = deserializer.Deserialize<T>(ReadJSONFile(path));

        return result;
    }

    private List<T> LoadListRegistry<T>(string path, JsonConverter[]? extraConverters = null)
    {
        var result = JsonConvert.DeserializeObject<List<T>>(ReadJSONFile(path), GetJSONSettings(extraConverters));

        if (result == null)
            throw new InvalidDataException("Could not load a registry from file: " + path);

        // ReSharper disable HeuristicUnreachableCode
#pragma warning disable CS0162
        if (Constants.VERBOSE_SIMULATION_PARAMETER_LOADING)
            GD.Print($"Loaded registry for {typeof(T)} with {result.Count} items");

        // ReSharper restore HeuristicUnreachableCode
#pragma warning restore CS0162

        return result;
    }

    private T LoadDirectObject<T>(string path, JsonConverter[]? extraConverters = null)
        where T : class
    {
        var result = JsonConvert.DeserializeObject<T>(ReadJSONFile(path), GetJSONSettings(extraConverters));

        if (result == null)
            throw new InvalidDataException("Could not load a registry from file: " + path);

        return result;
    }

    private void PostProcessCompounds()
    {
        compoundDefinitions.Clear();

        // Fetch inbuilt compounds

        // Invalid compound is added to make indexing math simpler
        compoundDefinitions.Add(null!);

        for (Compound i = Compound.ATP; i <= Compound.LastInbuiltCompound; ++i)
        {
            // Initialize and assign the compound
            var relatedDefinition = GetCompoundCaseInsensitive(Enum.GetName(i) ??
                throw new Exception("Inbuilt compound fetch name getting failed"));
            relatedDefinition.ID = i;

            compoundDefinitions.Add(relatedDefinition);
        }
    }

    private void CheckForInvalidValues()
    {
        CheckRegistryType(membranes);
        CheckRegistryType(backgrounds);
        CheckRegistryType(biomes);
        CheckRegistryType(bioProcesses);
        CheckRegistryType(compounds);
        CheckRegistryType(organelles);
        CheckRegistryType(enzymes);
        CheckRegistryType(musicCategories);
        CheckRegistryType(helpTexts);
        CheckRegistryType(inputGroups);
        CheckRegistryType(gallery);
        CheckRegistryType(difficultyPresets);
        CheckRegistryType(screenEffects);
        CheckRegistryType(oldVersionNotes);
        CheckRegistryType(newerVersionNotes);
        CheckRegistryType(worldResources);
        CheckRegistryType(equipment);
        CheckRegistryType(craftingRecipes);
        CheckRegistryType(structures);
        CheckRegistryType(unitTypes);
        CheckRegistryType(spaceStructures);
        CheckRegistryType(technologies);
        CheckRegistryType(visualResources);

        NameGenerator.Check(string.Empty);
        PatchMapNameGenerator.Check(string.Empty);
        autoEvoConfiguration.Check(string.Empty);
        autoEvoConfiguration.InternalName = AUTO_EVO_CONFIGURATION_NAME;
        translationsInfo.Check(string.Empty);
        gameCredits.Check(string.Empty);
        gameWiki.Check(string.Empty);
        lightCycle.Check(string.Empty);
        lightCycle.InternalName = DAY_NIGHT_CYCLE_NAME;
        buildInfo?.Check(string.Empty);

        if (oldVersionNotes.Count < 1)
            throw new Exception("Could not read old versions data");

        if (newerVersionNotes.Count < 1)
            throw new Exception("Could not read newer version patch notes data");
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

        foreach (var entry in structures)
        {
            entry.Value.Resolve();
        }

        foreach (var entry in unitTypes)
        {
            entry.Value.Resolve();
        }

        foreach (var entry in spaceStructures)
        {
            entry.Value.Resolve();
        }

        foreach (var entry in technologies)
        {
            entry.Value.Resolve(this);
        }

        NameGenerator.Resolve(this);

        // TODO: there could also be a check for making sure non-existent compounds, processes etc. are not used

        visualResourceByIdentifier = visualResources.ToDictionary(t => t.Value.Identifier, t => t.Value);
    }

    private List<Compound> ComputeCloudCompounds()
    {
        return compounds.Where(p => p.Value.IsCloud).Select(p => p.Value.ID).ToList();
    }

    private List<Enzyme> ComputeHydrolyticEnzymes()
    {
        return enzymes.Where(e => e.Value.Property == Enzyme.EnzymeProperty.Hydrolytic).Select(e => e.Value).ToList();
    }
}
