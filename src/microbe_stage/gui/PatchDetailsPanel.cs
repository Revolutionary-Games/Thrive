using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Godot;

public class PatchDetailsPanel : PanelContainer
{
    [Export]
    public NodePath? NothingSelectedPath;

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
    public NodePath LightMaxPath = null!;

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
    public NodePath MoveToPatchHSeparatorPath = null!;

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

    [Export]
    public NodePath UnknownPatchPath = null!;

#pragma warning disable CA2213
    private Control nothingSelected = null!;
    private Control details = null!;
    private Control playerHere = null!;
    private Control unknownPatch = null!;
    private Label name = null!;
    private Label biome = null!;
    private Label depth = null!;
    private Label temperatureLabel = null!;
    private Label pressure = null!;
    private Label light = null!;
    private Label lightMax = null!;
    private Label oxygen = null!;
    private Label nitrogen = null!;
    private Label co2 = null!;
    private Label hydrogenSulfide = null!;
    private Label ammonia = null!;
    private Label glucose = null!;
    private Label phosphate = null!;
    private Label iron = null!;
    private CollapsibleList speciesListBox = null!;
    private HSeparator? moveToPatchHSeparator;
    private Button? moveToPatchButton;

    private TextureRect temperatureSituation = null!;
    private TextureRect lightSituation = null!;
    private TextureRect hydrogenSulfideSituation = null!;
    private TextureRect glucoseSituation = null!;
    private TextureRect ironSituation = null!;
    private TextureRect ammoniaSituation = null!;
    private TextureRect phosphateSituation = null!;

    private Texture increaseIcon = null!;
    private Texture decreaseIcon = null!;
#pragma warning restore CA2213

    private Compound ammoniaCompound = null!;
    private Compound carbondioxideCompound = null!;
    private Compound glucoseCompound = null!;
    private Compound hydrogensulfideCompound = null!;
    private Compound ironCompound = null!;
    private Compound nitrogenCompound = null!;
    private Compound oxygenCompound = null!;
    private Compound phosphatesCompound = null!;
    private Compound sunlightCompound = null!;

    private Patch? targetPatch;
    private Patch? currentPatch;

    private bool moveToPatchButtonVisible = true;

    public Action<Patch>? OnMoveToPatchClicked { get; set; }

    public Patch? SelectedPatch
    {
        get => targetPatch;
        set
        {
            targetPatch = value;
            UpdateShownPatchDetails();
        }
    }

    public Patch? CurrentPatch
    {
        get => currentPatch;
        set
        {
            currentPatch = value;
            playerHere.Visible = CurrentPatch == SelectedPatch;

            if (SelectedPatch != null)
                UpdateConditionDifferencesBetweenPatches();
        }
    }

    public bool IsPatchMoveValid { get; set; }

    [Export]
    public bool MoveToPatchButtonVisible
    {
        get => moveToPatchButtonVisible;
        set
        {
            moveToPatchButtonVisible = value;
            UpdateMoveToPatchButton();
        }
    }

    public override void _Ready()
    {
        nothingSelected = GetNode<Control>(NothingSelectedPath);
        details = GetNode<Control>(DetailsPath);
        playerHere = GetNode<Control>(PlayerHerePath);
        unknownPatch = GetNode<Control>(UnknownPatchPath);
        name = GetNode<Label>(NamePath);
        biome = GetNode<Label>(BiomePath);
        depth = GetNode<Label>(DepthPath);
        temperatureLabel = GetNode<Label>(TemperaturePath);
        pressure = GetNode<Label>(PressurePath);
        light = GetNode<Label>(LightPath);
        lightMax = GetNode<Label>(LightMaxPath);
        oxygen = GetNode<Label>(OxygenPath);
        nitrogen = GetNode<Label>(NitrogenPath);
        co2 = GetNode<Label>(CO2Path);
        hydrogenSulfide = GetNode<Label>(HydrogenSulfidePath);
        ammonia = GetNode<Label>(AmmoniaPath);
        glucose = GetNode<Label>(GlucosePath);
        phosphate = GetNode<Label>(PhosphatePath);
        iron = GetNode<Label>(IronPath);
        speciesListBox = GetNode<CollapsibleList>(SpeciesListBoxPath);
        moveToPatchHSeparator = GetNode<HSeparator>(MoveToPatchHSeparatorPath);
        moveToPatchButton = GetNode<Button>(MoveToPatchButtonPath);

        temperatureSituation = GetNode<TextureRect>(TemperatureSituationPath);
        lightSituation = GetNode<TextureRect>(LightSituationPath);
        hydrogenSulfideSituation = GetNode<TextureRect>(HydrogenSulfideSituationPath);
        glucoseSituation = GetNode<TextureRect>(GlucoseSituationPath);
        ironSituation = GetNode<TextureRect>(IronSituationPath);
        ammoniaSituation = GetNode<TextureRect>(AmmoniaSituationPath);
        phosphateSituation = GetNode<TextureRect>(PhosphateSituationPath);

        ammoniaCompound = SimulationParameters.Instance.GetCompound("ammonia");
        carbondioxideCompound = SimulationParameters.Instance.GetCompound("carbondioxide");
        glucoseCompound = SimulationParameters.Instance.GetCompound("glucose");
        hydrogensulfideCompound = SimulationParameters.Instance.GetCompound("hydrogensulfide");
        ironCompound = SimulationParameters.Instance.GetCompound("iron");
        nitrogenCompound = SimulationParameters.Instance.GetCompound("nitrogen");
        oxygenCompound = SimulationParameters.Instance.GetCompound("oxygen");
        phosphatesCompound = SimulationParameters.Instance.GetCompound("phosphates");
        sunlightCompound = SimulationParameters.Instance.GetCompound("sunlight");

        increaseIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/increase.png");
        decreaseIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/decrease.png");

        UpdateMoveToPatchButton();
    }

    public void UpdateShownPatchDetails()
    {
        if (SelectedPatch == null)
        {
            details.Visible = false;
            nothingSelected.Visible = true;
            unknownPatch.Visible = false;

            return;
        }

        if (SelectedPatch.Known && !SelectedPatch.Discovered)
        {
            details.Visible = true;
            nothingSelected.Visible = false;
            unknownPatch.Visible = true;
        }

        details.Visible = true;
        nothingSelected.Visible = false;
        unknownPatch.Visible = false;

        UpdatePatchDetails();

        if (moveToPatchButton != null)
        {
            // Enable move to patch button if this is a valid move
            moveToPatchButton.Disabled = !IsPatchMoveValid;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (NothingSelectedPath != null)
            {
                NothingSelectedPath.Dispose();
                DetailsPath.Dispose();
                NamePath.Dispose();
                PlayerHerePath.Dispose();
                BiomePath.Dispose();
                DepthPath.Dispose();
                TemperaturePath.Dispose();
                PressurePath.Dispose();
                LightPath.Dispose();
                LightMaxPath.Dispose();
                OxygenPath.Dispose();
                NitrogenPath.Dispose();
                CO2Path.Dispose();
                HydrogenSulfidePath.Dispose();
                AmmoniaPath.Dispose();
                GlucosePath.Dispose();
                PhosphatePath.Dispose();
                IronPath.Dispose();
                SpeciesListBoxPath.Dispose();
                MoveToPatchHSeparatorPath.Dispose();
                MoveToPatchButtonPath.Dispose();
                TemperatureSituationPath.Dispose();
                LightSituationPath.Dispose();
                HydrogenSulfideSituationPath.Dispose();
                GlucoseSituationPath.Dispose();
                IronSituationPath.Dispose();
                AmmoniaSituationPath.Dispose();
                PhosphateSituationPath.Dispose();
                UnknownPatchPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Updates patch-specific GUI elements with data from a patch
    /// </summary>
    private void UpdatePatchDetails()
    {
        name.Text = SelectedPatch!.Name.ToString();

        // Biome: {0}
        biome.Text = TranslationServer.Translate("BIOME_LABEL").FormatSafe(SelectedPatch.BiomeTemplate.Name);

        // {0}-{1}m below sea level
        depth.Text = new LocalizedString("BELOW_SEA_LEVEL", SelectedPatch.Depth[0], SelectedPatch.Depth[1]).ToString();
        playerHere.Visible = CurrentPatch == SelectedPatch;

        var percentageFormat = TranslationServer.Translate("PERCENTAGE_VALUE");
        var unitFormat = TranslationServer.Translate("VALUE_WITH_UNIT");

        // Atmospheric gasses
        var temperature = SimulationParameters.Instance.GetCompound("temperature");
        temperatureLabel.Text =
            unitFormat.FormatSafe(SelectedPatch.Biome.CurrentCompoundAmounts[temperature].Ambient, temperature.Unit);
        pressure.Text = unitFormat.FormatSafe(20, "bar");

        var maxLightLevel = GetCompoundAmount(SelectedPatch, sunlightCompound.InternalName, CompoundAmountType.Maximum);
        light.Text = unitFormat.FormatSafe(percentageFormat.FormatSafe(Math.Round(
            GetCompoundAmount(SelectedPatch, sunlightCompound.InternalName))), "lx");
        lightMax.Text = TranslationServer.Translate("LIGHT_LEVEL_LABEL_AT_NOON").FormatSafe(
            unitFormat.FormatSafe(percentageFormat.FormatSafe(maxLightLevel), "lx"));
        lightMax.Visible = maxLightLevel > 0;

        oxygen.Text = percentageFormat.FormatSafe(GetCompoundAmount(SelectedPatch, oxygenCompound.InternalName));
        nitrogen.Text = percentageFormat.FormatSafe(GetCompoundAmount(SelectedPatch, nitrogenCompound.InternalName));
        co2.Text = percentageFormat.FormatSafe(GetCompoundAmount(SelectedPatch, carbondioxideCompound.InternalName));

        // Compounds
        hydrogenSulfide.Text =
            Math.Round(GetCompoundAmount(SelectedPatch, hydrogensulfideCompound.InternalName), 3)
                .ToString(CultureInfo.CurrentCulture);
        ammonia.Text =
            Math.Round(GetCompoundAmount(SelectedPatch, ammoniaCompound.InternalName), 3)
                .ToString(CultureInfo.CurrentCulture);
        glucose.Text =
            Math.Round(GetCompoundAmount(SelectedPatch, glucoseCompound.InternalName), 3)
                .ToString(CultureInfo.CurrentCulture);
        phosphate.Text =
            Math.Round(GetCompoundAmount(SelectedPatch, phosphatesCompound.InternalName), 3)
                .ToString(CultureInfo.CurrentCulture);
        iron.Text = GetCompoundAmount(SelectedPatch, ironCompound.InternalName)
            .ToString(CultureInfo.CurrentCulture);

        var label = speciesListBox.GetItem<CustomRichTextLabel>("SpeciesList");
        var speciesList = new StringBuilder(100);

        foreach (var species in SelectedPatch.SpeciesInPatch.Keys.OrderBy(s => s.FormattedName))
        {
            speciesList.AppendLine(TranslationServer.Translate("SPECIES_WITH_POPULATION").FormatSafe(
                species.FormattedNameBbCode, SelectedPatch.GetSpeciesSimulationPopulation(species)));
        }

        label.ExtendedBbcode = speciesList.ToString();

        UpdateConditionDifferencesBetweenPatches();
    }

    private void UpdateMoveToPatchButton()
    {
        if (moveToPatchButton != null)
        {
            moveToPatchButton.Visible = MoveToPatchButtonVisible;
        }

        if (moveToPatchHSeparator != null)
        {
            moveToPatchHSeparator.Visible = MoveToPatchButtonVisible;
        }
    }

    private float GetCompoundAmount(Patch patch, string compoundName,
        CompoundAmountType amountType = CompoundAmountType.Current)
    {
        return patch.GetCompoundAmount(compoundName, amountType);
    }

    /// <remarks>
    ///   <para>
    ///     TODO: this function should be cleaned up by generalizing the adding the increase or decrease icons in order
    ///     to remove the duplicated logic here
    ///   </para>
    /// </remarks>
    private void UpdateConditionDifferencesBetweenPatches()
    {
        if (SelectedPatch == null || CurrentPatch == null)
            return;

        var temperature = SimulationParameters.Instance.GetCompound("temperature");
        var nextCompound = SelectedPatch.Biome.Compounds[temperature].Ambient;

        if (nextCompound > CurrentPatch.Biome.Compounds[temperature].Ambient)
        {
            temperatureSituation.Texture = increaseIcon;
        }
        else if (nextCompound < CurrentPatch.Biome.Compounds[temperature].Ambient)
        {
            temperatureSituation.Texture = decreaseIcon;
        }
        else
        {
            temperatureSituation.Texture = null;
        }

        // We want to compare against the non-time of day adjusted light levels
        nextCompound = SelectedPatch.Biome.Compounds[sunlightCompound].Ambient;

        if (nextCompound > CurrentPatch.Biome.Compounds[sunlightCompound].Ambient)
        {
            lightSituation.Texture = increaseIcon;
        }
        else if (nextCompound < CurrentPatch.Biome.Compounds[sunlightCompound].Ambient)
        {
            lightSituation.Texture = decreaseIcon;
        }
        else
        {
            lightSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(SelectedPatch, hydrogensulfideCompound.InternalName);

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

        nextCompound = GetCompoundAmount(SelectedPatch, glucoseCompound.InternalName);

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

        nextCompound = GetCompoundAmount(SelectedPatch, ironCompound.InternalName);

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

        nextCompound = GetCompoundAmount(SelectedPatch, ammoniaCompound.InternalName);

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

        nextCompound = GetCompoundAmount(SelectedPatch, phosphatesCompound.InternalName);

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

    private void MoveToPatchClicked()
    {
        if (SelectedPatch == null)
            return;

        if (OnMoveToPatchClicked == null)
        {
            GD.PrintErr("No move to patch callback set, probably nothing is going to happen");
            return;
        }

        OnMoveToPatchClicked.Invoke(SelectedPatch);
    }
}
