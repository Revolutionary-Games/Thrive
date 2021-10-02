using System;
using System.Globalization;
using Godot;

public class PatchPanel : PanelContainer
{
    [Export]
    public NodePath NothingSelectedPath;

    [Export]
    public NodePath DetailsPath;

    [Export]
    public NodePath NamePath;

    [Export]
    public NodePath PlayerHerePath;

    [Export]
    public NodePath BiomePath;

    [Export]
    public NodePath DepthPath;

    [Export]
    public NodePath TemperaturePath;

    [Export]
    public NodePath PressurePath;

    [Export]
    public NodePath LightPath;

    [Export]
    public NodePath OxygenPath;

    [Export]
    public NodePath NitrogenPath;

    [Export]
    public NodePath CO2Path;

    [Export]
    public NodePath HydrogenSulfidePath;

    [Export]
    public NodePath AmmoniaPath;

    [Export]
    public NodePath GlucosePath;

    [Export]
    public NodePath PhosphatePath;

    [Export]
    public NodePath IronPath;

    [Export]
    public NodePath SpeciesListBoxPath;

    [Export]
    public NodePath MoveToPatchButtonPath;

    [Export]
    public NodePath TemperatureSituationPath;

    [Export]
    public NodePath LightSituationPath;

    [Export]
    public NodePath HydrogenSulfideSituationPath;

    [Export]
    public NodePath GlucoseSituationPath;

    [Export]
    public NodePath IronSituationPath;

    [Export]
    public NodePath AmmoniaSituationPath;

    [Export]
    public NodePath PhosphateSituationPath;

    private readonly Compound ammoniaCompound = SimulationParameters.Instance.GetCompound("ammonia");
    private readonly Compound carbondioxideCompound = SimulationParameters.Instance.GetCompound("carbondioxide");
    private readonly Compound glucoseCompound = SimulationParameters.Instance.GetCompound("glucose");
    private readonly Compound hydrogensulfideCompound = SimulationParameters.Instance.GetCompound("hydrogensulfide");
    private readonly Compound ironCompound = SimulationParameters.Instance.GetCompound("iron");
    private readonly Compound nitrogenCompound = SimulationParameters.Instance.GetCompound("nitrogen");
    private readonly Compound oxygenCompound = SimulationParameters.Instance.GetCompound("oxygen");
    private readonly Compound phosphatesCompound = SimulationParameters.Instance.GetCompound("phosphates");
    private readonly Compound sunlightCompound = SimulationParameters.Instance.GetCompound("sunlight");

    private Control nothingSelected;
    private Control details;
    private Control playerHere;
    private Label name;
    private Label biome;
    private Label depth;
    private Label temperature;
    private Label pressure;
    private Label light;
    private Label oxygen;
    private Label nitrogen;
    private Label co2;
    private Label hydrogenSulfide;
    private Label ammonia;
    private Label glucose;
    private Label phosphate;
    private Label iron;
    private CollapsibleList speciesListBox;
    private Button moveToPatchButton;
    private TextureRect temperatureSituation;
    private TextureRect lightSituation;
    private TextureRect hydrogenSulfideSituation;
    private TextureRect glucoseSituation;
    private TextureRect ironSituation;
    private TextureRect ammoniaSituation;
    private TextureRect phosphateSituation;
    private Texture increaseIcon;
    private Texture decreaseIcon;

    private Patch patch;
    private Patch currentPatch;

    public Action<Patch> OnMoveToPatchClicked { get; set; }

    public Patch Patch
    {
        get => patch;
        set
        {
            patch = value;

            UpdateStatisticsPanel();
        }
    }

    public Patch CurrentPatch
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

    private void MoveToPatchClicked()
    {
        OnMoveToPatchClicked?.Invoke(Patch);
    }

    private void UpdateStatisticsPanel()
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

        name.Text = TranslationServer.Translate(Patch.Name);

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

    /// <remarks>
    ///   TODO: this function should be cleaned up by generalizing the adding
    ///   the increase or decrease icons in order to remove the duplicated
    ///   logic here
    /// </remarks>
    private void UpdateConditionDifferencesBetweenPatches()
    {
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
