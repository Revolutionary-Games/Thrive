using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Godot;

/// <summary>
///   Shows details about a <see cref="Patch"/> in the GUI
/// </summary>
public partial class PatchDetailsPanel : PanelContainer
{
#pragma warning disable CA2213
    [Export]
    private Control nothingSelected = null!;

    [Export]
    private Control unknownPatch = null!;

    [Export]
    private Control detailsContainer = null!;

    [Export]
    private Control playerHere = null!;

    [Export]
    private Label patchName = null!;

    [Export]
    private Label biome = null!;

    [Export]
    private Label depth = null!;

    [Export]
    private HSeparator? moveToPatchHSeparator;

    [Export]
    private Button? moveToPatchButton;

    [Export]
    private CollapsibleList physicalConditionsContainer = null!;

    [Export]
    private CollapsibleList atmosphereContainer = null!;

    [Export]
    private CollapsibleList compoundsContainer = null!;

    [Export]
    private CollapsibleList speciesParentContainer = null!;

    private Label temperatureLabel = null!;
    private Label pressureLabel = null!;
    private Label lightLabel = null!;
    private Label lightMax = null!;
    private Label oxygenLabel = null!;
    private Label nitrogenLabel = null!;
    private Label co2Label = null!;
    private Label hydrogenSulfideLabel = null!;
    private Label ammoniaLabel = null!;
    private Label glucoseLabel = null!;
    private Label phosphateLabel = null!;
    private Label ironLabel = null!;

    private TextureRect temperatureSituation = null!;
    private TextureRect lightSituation = null!;
    private TextureRect hydrogenSulfideSituation = null!;
    private TextureRect glucoseSituation = null!;
    private TextureRect ironSituation = null!;
    private TextureRect ammoniaSituation = null!;
    private TextureRect phosphateSituation = null!;

    private CustomRichTextLabel speciesInfoDisplay = null!;

    private Texture2D increaseIcon = null!;
    private Texture2D decreaseIcon = null!;
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
        using var labelPath = new NodePath("Label");
        using var situation = new NodePath("Situation");

        // Physical conditions list
        var temperatureBase = physicalConditionsContainer.GetItem<Control>("Temperature");
        temperatureLabel = temperatureBase.GetNode<Label>(labelPath);
        temperatureSituation = temperatureBase.GetNode<TextureRect>(situation);

        var pressureBase = physicalConditionsContainer.GetItem<Control>("Pressure");
        pressureLabel = pressureBase.GetNode<Label>(labelPath);

        // pressureSituation = pressureBase.GetNode<TextureRect>(situation);

        var lightBase = physicalConditionsContainer.GetItem<Control>("Light");
        lightLabel = lightBase.GetNode<Label>("LightInfo/Current/Label");
        lightSituation = lightBase.GetNode<TextureRect>("LightInfo/Current/Situation");
        lightMax = lightBase.GetNode<Label>("LightInfo/MaxLabel");

        // Atmosphere list
        var oxygenBase = atmosphereContainer.GetItem<Control>("Oxygen");
        oxygenLabel = oxygenBase.GetNode<Label>(labelPath);

        // oxygenSituation = oxygenBase.GetNode<TextureRect>(situation);

        nitrogenLabel = null!;
        var nitrogenBase = atmosphereContainer.GetItem<Control>("Nitrogen");
        nitrogenLabel = nitrogenBase.GetNode<Label>(labelPath);

        // nitrogenSituation = nitrogenBase.GetNode<TextureRect>(situation);

        co2Label = null!;
        var co2Base = atmosphereContainer.GetItem<Control>("CO2");
        co2Label = co2Base.GetNode<Label>(labelPath);

        // co2Situation = co2Base.GetNode<TextureRect>(situation);

        // Compounds list
        hydrogenSulfideLabel = null!;
        var hydrogenSulfideBase = compoundsContainer.GetItem<Control>("HydrogenSulfide");
        hydrogenSulfideLabel = hydrogenSulfideBase.GetNode<Label>(labelPath);
        hydrogenSulfideSituation = hydrogenSulfideBase.GetNode<TextureRect>(situation);

        ammoniaLabel = null!;
        var ammoniaBase = compoundsContainer.GetItem<Control>("Ammonia");
        ammoniaLabel = ammoniaBase.GetNode<Label>(labelPath);
        ammoniaSituation = ammoniaBase.GetNode<TextureRect>(situation);

        glucoseLabel = null!;
        var glucoseBase = compoundsContainer.GetItem<Control>("Glucose");
        glucoseLabel = glucoseBase.GetNode<Label>(labelPath);
        glucoseSituation = glucoseBase.GetNode<TextureRect>(situation);

        phosphateLabel = null!;
        var phosphateBase = compoundsContainer.GetItem<Control>("Phosphate");
        phosphateLabel = phosphateBase.GetNode<Label>(labelPath);
        phosphateSituation = phosphateBase.GetNode<TextureRect>(situation);

        ironLabel = null!;
        var ironBase = compoundsContainer.GetItem<Control>("Iron");
        ironLabel = ironBase.GetNode<Label>(labelPath);
        ironSituation = ironBase.GetNode<TextureRect>(situation);

        // Species list
        speciesInfoDisplay = speciesParentContainer.GetItem<CustomRichTextLabel>("SpeciesList");

        // Get compound references
        ammoniaCompound = SimulationParameters.Instance.GetCompound("ammonia");
        carbondioxideCompound = SimulationParameters.Instance.GetCompound("carbondioxide");
        glucoseCompound = SimulationParameters.Instance.GetCompound("glucose");
        hydrogensulfideCompound = SimulationParameters.Instance.GetCompound("hydrogensulfide");
        ironCompound = SimulationParameters.Instance.GetCompound("iron");
        nitrogenCompound = SimulationParameters.Instance.GetCompound("nitrogen");
        oxygenCompound = SimulationParameters.Instance.GetCompound("oxygen");
        phosphatesCompound = SimulationParameters.Instance.GetCompound("phosphates");
        sunlightCompound = SimulationParameters.Instance.GetCompound("sunlight");

        increaseIcon = GD.Load<Texture2D>("res://assets/textures/gui/bevel/increase.png");
        decreaseIcon = GD.Load<Texture2D>("res://assets/textures/gui/bevel/decrease.png");

        UpdateMoveToPatchButton();
    }

    public void UpdateShownPatchDetails()
    {
        if (SelectedPatch == null)
        {
            detailsContainer.Visible = false;
            nothingSelected.Visible = true;
            unknownPatch.Visible = false;

            return;
        }

        if (SelectedPatch.Visibility != MapElementVisibility.Shown)
        {
            detailsContainer.Visible = false;
            nothingSelected.Visible = false;
            unknownPatch.Visible = true;
        }
        else
        {
            detailsContainer.Visible = true;
            nothingSelected.Visible = false;
            unknownPatch.Visible = false;

            UpdatePatchDetails();
        }

        if (moveToPatchButton != null)
        {
            // Enable move to patch button if this is a valid move
            moveToPatchButton.Disabled = !IsPatchMoveValid;
        }
    }

    /// <summary>
    ///   Updates patch-specific GUI elements with data from a patch
    /// </summary>
    private void UpdatePatchDetails()
    {
        patchName.Text = SelectedPatch!.Name.ToString();

        // Biome: {0}
        biome.Text = Localization.Translate("BIOME_LABEL").FormatSafe(SelectedPatch.BiomeTemplate.Name);

        // {0}-{1}m below sea level
        depth.Text = new LocalizedString("BELOW_SEA_LEVEL", SelectedPatch.Depth[0], SelectedPatch.Depth[1]).ToString();
        playerHere.Visible = CurrentPatch == SelectedPatch;

        var percentageFormat = Localization.Translate("PERCENTAGE_VALUE");
        var unitFormat = Localization.Translate("VALUE_WITH_UNIT");

        // Atmospheric gasses
        var temperature = SimulationParameters.Instance.GetCompound("temperature");
        temperatureLabel.Text =
            unitFormat.FormatSafe(SelectedPatch.Biome.CurrentCompoundAmounts[temperature].Ambient, temperature.Unit);
        pressureLabel.Text = unitFormat.FormatSafe(20, "bar");

        var maxLightLevel = GetCompoundAmount(SelectedPatch, sunlightCompound.InternalName, CompoundAmountType.Maximum);
        lightLabel.Text =
            unitFormat.FormatSafe(percentageFormat.FormatSafe(Math.Round(GetCompoundAmount(SelectedPatch,
                sunlightCompound.InternalName))), "lx");
        lightMax.Text = Localization.Translate("LIGHT_LEVEL_LABEL_AT_NOON").FormatSafe(
            unitFormat.FormatSafe(percentageFormat.FormatSafe(maxLightLevel), "lx"));
        lightMax.Visible = maxLightLevel > 0;

        oxygenLabel.Text = percentageFormat.FormatSafe(Math.Round(
            GetCompoundAmount(SelectedPatch, oxygenCompound.InternalName),
            Constants.ATMOSPHERIC_COMPOUND_DISPLAY_DECIMALS));
        nitrogenLabel.Text =
            percentageFormat.FormatSafe(Math.Round(GetCompoundAmount(SelectedPatch, nitrogenCompound.InternalName),
                Constants.ATMOSPHERIC_COMPOUND_DISPLAY_DECIMALS));
        co2Label.Text =
            percentageFormat.FormatSafe(Math.Round(GetCompoundAmount(SelectedPatch, carbondioxideCompound.InternalName),
                Constants.ATMOSPHERIC_COMPOUND_DISPLAY_DECIMALS));

        // Compounds
        hydrogenSulfideLabel.Text =
            Math.Round(GetCompoundAmount(SelectedPatch, hydrogensulfideCompound.InternalName),
                    Constants.PATCH_CONDITIONS_COMPOUND_DISPLAY_DECIMALS)
                .ToString(CultureInfo.CurrentCulture);
        ammoniaLabel.Text =
            Math.Round(GetCompoundAmount(SelectedPatch, ammoniaCompound.InternalName),
                    Constants.PATCH_CONDITIONS_COMPOUND_DISPLAY_DECIMALS)
                .ToString(CultureInfo.CurrentCulture);
        glucoseLabel.Text =
            Math.Round(GetCompoundAmount(SelectedPatch, glucoseCompound.InternalName),
                    Constants.PATCH_CONDITIONS_COMPOUND_DISPLAY_DECIMALS)
                .ToString(CultureInfo.CurrentCulture);
        phosphateLabel.Text =
            Math.Round(GetCompoundAmount(SelectedPatch, phosphatesCompound.InternalName),
                    Constants.PATCH_CONDITIONS_COMPOUND_DISPLAY_DECIMALS)
                .ToString(CultureInfo.CurrentCulture);
        ironLabel.Text =
            Math.Round(GetCompoundAmount(SelectedPatch, ironCompound.InternalName),
                    Constants.PATCH_CONDITIONS_COMPOUND_DISPLAY_DECIMALS)
                .ToString(CultureInfo.CurrentCulture);

        var speciesList = new StringBuilder(100);

        foreach (var species in SelectedPatch.SpeciesInPatch.Keys.OrderBy(s => s.FormattedName))
        {
            speciesList.AppendLine(Localization.Translate("SPECIES_WITH_POPULATION").FormatSafe(
                species.FormattedNameBbCode, SelectedPatch.GetSpeciesSimulationPopulation(species)));
        }

        speciesInfoDisplay.ExtendedBbcode = speciesList.ToString();

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
