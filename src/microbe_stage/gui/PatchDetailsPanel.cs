using System;
using System.Globalization;
using Godot;

public class PatchDetailsPanel : PanelContainer
{
    [Export]
    public NodePath NothingSelectedPath = null!;

    [Export]
    public NodePath DetailsPath = null!;

    [Export]
    public NodePath NamePath = null!;

    [Export]
    public NodePath PlayerHerePath = null!;

    [Export]
    public NodePath BiomePath = null!;

    [Export]
    public NodePath DepthPath = null!;

    [Export]
    public NodePath TemperaturePath = null!;

    [Export]
    public NodePath PressurePath = null!;

    [Export]
    public NodePath LightPath = null!;

    [Export]
    public NodePath OxygenPath = null!;

    [Export]
    public NodePath NitrogenPath = null!;

    [Export]
    public NodePath CO2Path = null!;

    [Export]
    public NodePath HydrogenSulfidePath = null!;

    [Export]
    public NodePath AmmoniaPath = null!;

    [Export]
    public NodePath GlucosePath = null!;

    [Export]
    public NodePath PhosphatePath = null!;

    [Export]
    public NodePath IronPath = null!;

    [Export]
    public NodePath SpeciesListBoxPath = null!;

    [Export]
    public NodePath MoveToPatchButtonPath = null!;

    [Export]
    public NodePath TemperatureSituationPath = null!;

    [Export]
    public NodePath LightSituationPath = null!;

    [Export]
    public NodePath HydrogenSulfideSituationPath = null!;

    [Export]
    public NodePath GlucoseSituationPath = null!;

    [Export]
    public NodePath IronSituationPath = null!;

    [Export]
    public NodePath AmmoniaSituationPath = null!;

    [Export]
    public NodePath PhosphateSituationPath = null!;

    private readonly Compound ammoniaCompound = SimulationParameters.Instance.GetCompound("ammonia");
    private readonly Compound carbondioxideCompound = SimulationParameters.Instance.GetCompound("carbondioxide");
    private readonly Compound glucoseCompound = SimulationParameters.Instance.GetCompound("glucose");
    private readonly Compound hydrogensulfideCompound = SimulationParameters.Instance.GetCompound("hydrogensulfide");
    private readonly Compound ironCompound = SimulationParameters.Instance.GetCompound("iron");
    private readonly Compound nitrogenCompound = SimulationParameters.Instance.GetCompound("nitrogen");
    private readonly Compound oxygenCompound = SimulationParameters.Instance.GetCompound("oxygen");
    private readonly Compound phosphatesCompound = SimulationParameters.Instance.GetCompound("phosphates");
    private readonly Compound sunlightCompound = SimulationParameters.Instance.GetCompound("sunlight");

    private Control nothingSelected = null!;
    private Control details = null!;
    private Control playerHere = null!;
    private Label name = null!;
    private Label biome = null!;
    private Label depth = null!;
    private Label temperature = null!;
    private Label pressure = null!;
    private Label light = null!;
    private Label oxygen = null!;
    private Label nitrogen = null!;
    private Label co2 = null!;
    private Label hydrogenSulfide = null!;
    private Label ammonia = null!;
    private Label glucose = null!;
    private Label phosphate = null!;
    private Label iron = null!;
    private CollapsibleList speciesListBox = null!;
    private Button moveToPatchButton = null!;
    private TextureRect temperatureSituation = null!;
    private TextureRect lightSituation = null!;
    private TextureRect hydrogenSulfideSituation = null!;
    private TextureRect glucoseSituation = null!;
    private TextureRect ironSituation = null!;
    private TextureRect ammoniaSituation = null!;
    private TextureRect phosphateSituation = null!;
    private Texture increaseIcon = null!;
    private Texture decreaseIcon = null!;

    private Patch? patch;
    private Patch? currentPatch;

    public Action<Patch> OnMoveToPatchClicked { get; set; } = null!;

    public Patch? Patch
    {
        get => patch;
        set
        {
            patch = value;

            UpdateStatisticsPanel();
        }
    }

    public Patch? CurrentPatch
    {
        get => currentPatch;
        set
        {
            currentPatch = value;
            playerHere.Visible = CurrentPatch == Patch;

            if (Patch != null)
                UpdateConditionDifferencesBetweenPatches();
        }
    }

    public bool IsPatchMoveValid { get; set; }

    public override void _Ready()
    {
        increaseIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/increase.png");
        decreaseIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/decrease.png");

        nothingSelected = GetNode<Control>(NothingSelectedPath);
        details = GetNode<Control>(DetailsPath);
        playerHere = GetNode<Control>(PlayerHerePath);
        name = GetNode<Label>(NamePath);
        biome = GetNode<Label>(BiomePath);
        depth = GetNode<Label>(DepthPath);
        temperature = GetNode<Label>(TemperaturePath);
        pressure = GetNode<Label>(PressurePath);
        light = GetNode<Label>(LightPath);
        oxygen = GetNode<Label>(OxygenPath);
        nitrogen = GetNode<Label>(NitrogenPath);
        co2 = GetNode<Label>(CO2Path);
        hydrogenSulfide = GetNode<Label>(HydrogenSulfidePath);
        ammonia = GetNode<Label>(AmmoniaPath);
        glucose = GetNode<Label>(GlucosePath);
        phosphate = GetNode<Label>(PhosphatePath);
        iron = GetNode<Label>(IronPath);
        speciesListBox = GetNode<CollapsibleList>(SpeciesListBoxPath);
        moveToPatchButton = GetNode<Button>(MoveToPatchButtonPath);
        temperatureSituation = GetNode<TextureRect>(TemperatureSituationPath);
        lightSituation = GetNode<TextureRect>(LightSituationPath);
        hydrogenSulfideSituation = GetNode<TextureRect>(HydrogenSulfideSituationPath);
        glucoseSituation = GetNode<TextureRect>(GlucoseSituationPath);
        ironSituation = GetNode<TextureRect>(IronSituationPath);
        ammoniaSituation = GetNode<TextureRect>(AmmoniaSituationPath);
        phosphateSituation = GetNode<TextureRect>(PhosphateSituationPath);
    }

    public void UpdateStatisticsPanel()
    {
        if (Patch == null)
        {
            details.Visible = false;
            nothingSelected.Visible = true;

            return;
        }

        // Enable move to patch button if this is a valid move
        moveToPatchButton.Disabled = !IsPatchMoveValid;

        details.Visible = true;
        nothingSelected.Visible = false;

        name.Text = Patch.Name.ToString();

        // Biome: {0}
        biome.Text = string.Format(CultureInfo.CurrentCulture,
            TranslationServer.Translate("BIOME_LABEL"),
            Patch.BiomeTemplate.Name);

        // {0}-{1}m below sea level
        depth.Text = string.Format(CultureInfo.CurrentCulture,
            TranslationServer.Translate("BELOW_SEA_LEVEL"),
            Patch.Depth[0], Patch.Depth[1]);
        playerHere.Visible = CurrentPatch == Patch;

        var percentageFormat = TranslationServer.Translate("PERCENTAGE_VALUE");

        // Atmospheric gasses
        temperature.Text = Patch.Biome.AverageTemperature + " °C";
        pressure.Text = "20 bar";
        light.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            GetCompoundAmount(Patch, sunlightCompound.InternalName)) + " lx";
        oxygen.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            GetCompoundAmount(Patch, oxygenCompound.InternalName));
        nitrogen.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            GetCompoundAmount(Patch, nitrogenCompound.InternalName));
        co2.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            GetCompoundAmount(Patch, carbondioxideCompound.InternalName));

        // Compounds
        hydrogenSulfide.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            Math.Round(GetCompoundAmount(Patch, hydrogensulfideCompound.InternalName), 3));
        ammonia.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            Math.Round(GetCompoundAmount(Patch, ammoniaCompound.InternalName), 3));
        glucose.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            Math.Round(GetCompoundAmount(Patch, glucoseCompound.InternalName), 3));
        phosphate.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            Math.Round(GetCompoundAmount(Patch, phosphatesCompound.InternalName), 3));
        iron.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            GetCompoundAmount(Patch, ironCompound.InternalName));

        // Refresh species list
        speciesListBox.ClearItems();

        foreach (var species in Patch.SpeciesInPatch.Keys)
        {
            var speciesLabel = new Label();
            speciesLabel.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            speciesLabel.Autowrap = true;
            speciesLabel.Text = string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("WITH_POPULATION"), species.FormattedName,
                Patch.GetSpeciesPopulation(species));
            speciesListBox.AddItem(speciesLabel);
        }

        UpdateConditionDifferencesBetweenPatches();
    }

    private void MoveToPatchClicked()
    {
        if (Patch == null)
            return;

        OnMoveToPatchClicked.Invoke(Patch);
    }

    /// <remarks>
    ///   TODO: this function should be cleaned up by generalizing the adding
    ///   the increase or decrease icons in order to remove the duplicated
    ///   logic here
    /// </remarks>
    private void UpdateConditionDifferencesBetweenPatches()
    {
        if (Patch == null || CurrentPatch == null)
            return;

        var nextCompound = Patch.Biome.AverageTemperature;

        if (nextCompound > CurrentPatch.Biome.AverageTemperature)
        {
            temperatureSituation.Texture = increaseIcon;
        }
        else if (nextCompound < CurrentPatch.Biome.AverageTemperature)
        {
            temperatureSituation.Texture = decreaseIcon;
        }
        else
        {
            temperatureSituation.Texture = null;
        }

        nextCompound = Patch.Biome.Compounds[sunlightCompound].Dissolved;

        if (nextCompound > CurrentPatch.Biome.Compounds[sunlightCompound].Dissolved)
        {
            lightSituation.Texture = increaseIcon;
        }
        else if (nextCompound < CurrentPatch.Biome.Compounds[sunlightCompound].Dissolved)
        {
            lightSituation.Texture = decreaseIcon;
        }
        else
        {
            lightSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(Patch, hydrogensulfideCompound.InternalName);

        if (nextCompound > GetCompoundAmount(CurrentPatch, hydrogensulfideCompound.InternalName))
        {
            hydrogenSulfideSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(CurrentPatch, hydrogensulfideCompound.InternalName))
        {
            hydrogenSulfideSituation.Texture = decreaseIcon;
        }
        else
        {
            hydrogenSulfideSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(Patch, glucoseCompound.InternalName);

        if (nextCompound > GetCompoundAmount(CurrentPatch, glucoseCompound.InternalName))
        {
            glucoseSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(CurrentPatch, glucoseCompound.InternalName))
        {
            glucoseSituation.Texture = decreaseIcon;
        }
        else
        {
            glucoseSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(Patch, ironCompound.InternalName);

        if (nextCompound > GetCompoundAmount(CurrentPatch, ironCompound.InternalName))
        {
            ironSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(CurrentPatch, ironCompound.InternalName))
        {
            ironSituation.Texture = decreaseIcon;
        }
        else
        {
            ironSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(Patch, ammoniaCompound.InternalName);

        if (nextCompound > GetCompoundAmount(CurrentPatch, ammoniaCompound.InternalName))
        {
            ammoniaSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(CurrentPatch, ammoniaCompound.InternalName))
        {
            ammoniaSituation.Texture = decreaseIcon;
        }
        else
        {
            ammoniaSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(Patch, phosphatesCompound.InternalName);

        if (nextCompound > GetCompoundAmount(CurrentPatch, phosphatesCompound.InternalName))
        {
            phosphateSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(CurrentPatch, phosphatesCompound.InternalName))
        {
            phosphateSituation.Texture = decreaseIcon;
        }
        else
        {
            phosphateSituation.Texture = null;
        }
    }

    private float GetCompoundAmount(Patch patch, string compoundName)
    {
        var compound = SimulationParameters.Instance.GetCompound(compoundName);
        var patchBiome = patch.Biome;

        return compoundName switch
        {
            "sunlight" => patchBiome.Compounds[compound].Dissolved * 100,
            "oxygen" => patchBiome.Compounds[compound].Dissolved * 100,
            "carbondioxide" => patchBiome.Compounds[compound].Dissolved * 100,
            "nitrogen" => patchBiome.Compounds[compound].Dissolved * 100,
            "iron" => patch.GetTotalChunkCompoundAmount(compound),
            _ => patchBiome.Compounds[compound].Density * patchBiome.Compounds[compound].Amount +
                patch.GetTotalChunkCompoundAmount(compound),
        };
    }
}
