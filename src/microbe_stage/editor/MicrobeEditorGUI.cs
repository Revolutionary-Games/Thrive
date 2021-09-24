using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main class managing the microbe editor GUI
/// </summary>
public class MicrobeEditorGUI : Node, ISaveLoadedTracked
{
    // The labels to update are at really long relative paths, so they are set in the Godot editor
    [Export]
    public NodePath MenuPath;

    [Export]
    public NodePath ReportTabButtonPath;

    [Export]
    public NodePath PatchMapButtonPath;

    [Export]
    public NodePath CellEditorButtonPath;

    [Export]
    public NodePath StructureTabButtonPath;

    [Export]
    public NodePath AppearanceTabButtonPath;

    [Export]
    public NodePath StructureTabPath;

    [Export]
    public NodePath AppearanceTabPath;

    [Export]
    public NodePath SizeLabelPath;

    [Export]
    public NodePath OrganismStatisticsPath;

    [Export]
    public NodePath SpeedLabelPath;

    [Export]
    public NodePath HpLabelPath;

    [Export]
    public NodePath GenerationLabelPath;

    [Export]
    public NodePath CurrentMutationPointsLabelPath;

    [Export]
    public NodePath MutationPointsArrowPath;

    [Export]
    public NodePath ResultingMutationPointsLabelPath;

    [Export]
    public NodePath BaseMutationPointsLabelPath;

    [Export]
    public NodePath MutationPointsBarPath;

    [Export]
    public NodePath MutationPointsSubtractBarPath;

    [Export]
    public NodePath SpeciesNameEditPath;

    [Export]
    public NodePath MembraneColorPickerPath;

    [Export]
    public NodePath MenuButtonPath;

    [Export]
    public NodePath HelpButtonPath;

    [Export]
    public NodePath NewCellButtonPath;

    [Export]
    public NodePath UndoButtonPath;

    [Export]
    public NodePath RedoButtonPath;

    [Export]
    public NodePath FinishButtonPath;

    [Export]
    public NodePath CancelButtonPath;

    [Export]
    public NodePath SymmetryButtonPath;

    [Export]
    public NodePath ATPBalanceLabelPath;

    [Export]
    public NodePath ATPProductionLabelPath;

    [Export]
    public NodePath ATPConsumptionLabelPath;

    [Export]
    public NodePath ATPProductionBarPath;

    [Export]
    public NodePath ATPConsumptionBarPath;

    [Export]
    public NodePath TimeIndicatorPath;

    [Export]
    public NodePath PhysicalConditionsIconLegendPath;

    [Export]
    public NodePath TemperatureChartPath;

    [Export]
    public NodePath SunlightChartPath;

    [Export]
    public NodePath AtmosphericGassesChartPath;

    [Export]
    public NodePath CompoundsChartPath;

    [Export]
    public NodePath SpeciesPopulationChartPath;

    [Export]
    public NodePath GlucoseReductionLabelPath;

    [Export]
    public NodePath AutoEvoLabelPath;

    [Export]
    public NodePath ExternalEffectsLabelPath;

    [Export]
    public NodePath MapDrawerPath;

    [Export]
    public NodePath PatchNothingSelectedPath;

    [Export]
    public NodePath PatchDetailsPath;

    [Export]
    public NodePath PatchNamePath;

    [Export]
    public NodePath ReportTabPatchNamePath;

    [Export]
    public NodePath PatchPlayerHerePath;

    [Export]
    public NodePath PatchBiomePath;

    [Export]
    public NodePath PatchDepthPath;

    [Export]
    public NodePath PatchTemperaturePath;

    [Export]
    public NodePath PatchPressurePath;

    [Export]
    public NodePath PatchLightPath;

    [Export]
    public NodePath PatchOxygenPath;

    [Export]
    public NodePath PatchNitrogenPath;

    [Export]
    public NodePath PatchCO2Path;

    [Export]
    public NodePath PatchHydrogenSulfidePath;

    [Export]
    public NodePath PatchAmmoniaPath;

    [Export]
    public NodePath PatchGlucosePath;

    [Export]
    public NodePath PatchPhosphatePath;

    [Export]
    public NodePath PatchIronPath;

    [Export]
    public NodePath SpeciesCollapsibleBoxPath;

    [Export]
    public NodePath MoveToPatchButtonPath;

    [Export]
    public NodePath PatchTemperatureSituationPath;

    [Export]
    public NodePath PatchLightSituationPath;

    [Export]
    public NodePath PatchHydrogenSulfideSituationPath;

    [Export]
    public NodePath PatchGlucoseSituationPath;

    [Export]
    public NodePath PatchIronSituationPath;

    [Export]
    public NodePath PatchAmmoniaSituationPath;

    [Export]
    public NodePath PatchPhosphateSituationPath;

    [Export]
    public NodePath SpeedIndicatorPath;

    [Export]
    public NodePath HpIndicatorPath;

    [Export]
    public NodePath SizeIndicatorPath;

    [Export]
    public NodePath RigiditySliderPath;

    [Export]
    public NodePath NegativeAtpPopupPath;

    [Export]
    public NodePath IslandErrorPath;

    [Export]
    public NodePath OrganelleMenuPath;

    [Export]
    public NodePath SymmetryIconPath;

    [Export]
    public NodePath CompoundBalancePath;

    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");
    private readonly Compound ammonia = SimulationParameters.Instance.GetCompound("ammonia");
    private readonly Compound carbondioxide = SimulationParameters.Instance.GetCompound("carbondioxide");
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");
    private readonly Compound hydrogensulfide = SimulationParameters.Instance.GetCompound("hydrogensulfide");
    private readonly Compound iron = SimulationParameters.Instance.GetCompound("iron");
    private readonly Compound nitrogen = SimulationParameters.Instance.GetCompound("nitrogen");
    private readonly Compound oxygen = SimulationParameters.Instance.GetCompound("oxygen");
    private readonly Compound phosphates = SimulationParameters.Instance.GetCompound("phosphates");
    private readonly Compound sunlight = SimulationParameters.Instance.GetCompound("sunlight");

    private readonly OrganelleDefinition protoplasm = SimulationParameters.Instance.GetOrganelleType("protoplasm");
    private readonly OrganelleDefinition nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");

    private EnergyBalanceInfo energyBalanceInfo;

    [JsonProperty]
    private float initialCellSpeed;

    [JsonProperty]
    private int initialCellSize;

    [JsonProperty]
    private float initialCellHp;

    private MicrobeEditor editor;

    private Dictionary<OrganelleDefinition, MicrobePartSelection> placeablePartSelectionElements =
        new Dictionary<OrganelleDefinition, MicrobePartSelection>();

    private Dictionary<MembraneType, MicrobePartSelection> membraneSelectionElements =
        new Dictionary<MembraneType, MicrobePartSelection>();

    private PauseMenu menu;

    // Editor tab selector buttons
    private Button reportTabButton;
    private Button patchMapButton;
    private Button cellEditorButton;

    // Selection menu tab selector buttons
    private Button structureTabButton;
    private Button appearanceTabButton;

    private PanelContainer structureTab;
    private PanelContainer appearanceTab;

    private Label sizeLabel;
    private Label speedLabel;
    private Label hpLabel;
    private Label generationLabel;

    private Label currentMutationPointsLabel;
    private TextureRect mutationPointsArrow;
    private Label resultingMutationPointsLabel;
    private Label baseMutationPointsLabel;
    private ProgressBar mutationPointsBar;
    private ProgressBar mutationPointsSubtractBar;

    private Slider rigiditySlider;
    private TweakedColourPicker membraneColorPicker;

    private TextureButton undoButton;
    private TextureButton redoButton;
    private TextureButton newCellButton;
    private LineEdit speciesNameEdit;

    private Button finishButton;
    private Button cancelButton;

    // ReSharper disable once NotAccessedField.Local
    private TextureButton symmetryButton;
    private TextureRect symmetryIcon;

    private Label atpBalanceLabel;
    private Label atpProductionLabel;
    private Label atpConsumptionLabel;
    private SegmentedBar atpProductionBar;
    private SegmentedBar atpConsumptionBar;

    private Label timeIndicator;
    private Label glucoseReductionLabel;
    private Label autoEvoLabel;
    private Label externalEffectsLabel;
    private Label reportTabPatchNameLabel;

    private HBoxContainer physicalConditionsIconLegends;
    private LineChart temperatureChart;
    private LineChart sunlightChart;
    private LineChart atmosphericGassesChart;
    private LineChart compoundsChart;
    private LineChart speciesPopulationChart;

    private PatchMapDrawer mapDrawer;
    private Control patchNothingSelected;
    private Control patchDetails;
    private Control patchPlayerHere;
    private Label patchName;
    private Label patchBiome;
    private Label patchDepth;
    private Label patchTemperature;
    private Label patchPressure;
    private Label patchLight;
    private Label patchOxygen;
    private Label patchNitrogen;
    private Label patchCO2;
    private Label patchHydrogenSulfide;
    private Label patchAmmonia;
    private Label patchGlucose;
    private Label patchPhosphate;
    private Label patchIron;
    private CollapsibleList speciesListBox;
    private Button moveToPatchButton;

    private TextureRect patchTemperatureSituation;
    private TextureRect patchLightSituation;
    private TextureRect patchHydrogenSulfideSituation;
    private TextureRect patchGlucoseSituation;
    private TextureRect patchIronSituation;
    private TextureRect patchAmmoniaSituation;
    private TextureRect patchPhosphateSituation;

    private TextureRect speedIndicator;
    private TextureRect hpIndicator;
    private TextureRect sizeIndicator;

    private Texture symmetryIconDefault;
    private Texture symmetryIcon2X;
    private Texture symmetryIcon4X;
    private Texture symmetryIcon6X;
    private Texture increaseIcon;
    private Texture decreaseIcon;
    private AudioStream unableToPlaceHexSound;
    private Texture temperatureIcon;

    private ConfirmationDialog negativeAtpPopup;
    private AcceptDialog islandPopup;

    private OrganellePopupMenu organelleMenu;

    private TextureButton menuButton;
    private TextureButton helpButton;

    private CompoundBalanceDisplay compoundBalance;

    private IEnumerable<(Hex hex, int orientation)> mouseHoverHexes;

    [JsonProperty]
    private EditorTab selectedEditorTab = EditorTab.Report;

    [JsonProperty]
    private SelectionMenuTab selectedSelectionMenuTab = SelectionMenuTab.Structure;

    private MicrobeEditor.MicrobeSymmetry symmetry = MicrobeEditor.MicrobeSymmetry.None;

    public enum EditorTab
    {
        Report,
        PatchMap,
        CellEditor,
    }

    public enum SelectionMenuTab
    {
        Structure,
        Membrane,
        Behaviour,
    }

    public bool IsLoadedFromSave { get; set; }

    [JsonIgnore]
    public IEnumerable<(Hex hex, int orientation)> MouseHoverHexes
    {
        get
        {
            return mouseHoverHexes;
        }
        set
        {
            if (mouseHoverHexes == null && value == null)
                return;

            if (mouseHoverHexes != null && value != null && mouseHoverHexes.SequenceEqual(value))
                return;

            mouseHoverHexes = value;
            UpdateMutationPointsBar();
        }
    }

    public override void _Ready()
    {
        reportTabButton = GetNode<Button>(ReportTabButtonPath);
        patchMapButton = GetNode<Button>(PatchMapButtonPath);
        cellEditorButton = GetNode<Button>(CellEditorButtonPath);

        structureTab = GetNode<PanelContainer>(StructureTabPath);
        structureTabButton = GetNode<Button>(StructureTabButtonPath);

        appearanceTab = GetNode<PanelContainer>(AppearanceTabPath);
        appearanceTabButton = GetNode<Button>(AppearanceTabButtonPath);

        sizeLabel = GetNode<Label>(SizeLabelPath);
        speedLabel = GetNode<Label>(SpeedLabelPath);
        hpLabel = GetNode<Label>(HpLabelPath);
        generationLabel = GetNode<Label>(GenerationLabelPath);

        currentMutationPointsLabel = GetNode<Label>(CurrentMutationPointsLabelPath);
        mutationPointsArrow = GetNode<TextureRect>(MutationPointsArrowPath);
        resultingMutationPointsLabel = GetNode<Label>(ResultingMutationPointsLabelPath);
        baseMutationPointsLabel = GetNode<Label>(BaseMutationPointsLabelPath);
        mutationPointsBar = GetNode<ProgressBar>(MutationPointsBarPath);
        mutationPointsSubtractBar = GetNode<ProgressBar>(MutationPointsSubtractBarPath);

        rigiditySlider = GetNode<Slider>(RigiditySliderPath);
        membraneColorPicker = GetNode<TweakedColourPicker>(MembraneColorPickerPath);

        menuButton = GetNode<TextureButton>(MenuButtonPath);
        helpButton = GetNode<TextureButton>(HelpButtonPath);
        undoButton = GetNode<TextureButton>(UndoButtonPath);
        redoButton = GetNode<TextureButton>(RedoButtonPath);
        symmetryButton = GetNode<TextureButton>(SymmetryButtonPath);
        newCellButton = GetNode<TextureButton>(NewCellButtonPath);
        speciesNameEdit = GetNode<LineEdit>(SpeciesNameEditPath);
        finishButton = GetNode<Button>(FinishButtonPath);
        cancelButton = GetNode<Button>(CancelButtonPath);

        atpBalanceLabel = GetNode<Label>(ATPBalanceLabelPath);
        atpProductionLabel = GetNode<Label>(ATPProductionLabelPath);
        atpConsumptionLabel = GetNode<Label>(ATPConsumptionLabelPath);
        atpProductionBar = GetNode<SegmentedBar>(ATPProductionBarPath);
        atpConsumptionBar = GetNode<SegmentedBar>(ATPConsumptionBarPath);

        reportTabPatchNameLabel = GetNode<Label>(ReportTabPatchNamePath);
        timeIndicator = GetNode<Label>(TimeIndicatorPath);
        glucoseReductionLabel = GetNode<Label>(GlucoseReductionLabelPath);
        autoEvoLabel = GetNode<Label>(AutoEvoLabelPath);
        externalEffectsLabel = GetNode<Label>(ExternalEffectsLabelPath);
        mapDrawer = GetNode<PatchMapDrawer>(MapDrawerPath);
        patchNothingSelected = GetNode<Control>(PatchNothingSelectedPath);
        patchDetails = GetNode<Control>(PatchDetailsPath);
        patchName = GetNode<Label>(PatchNamePath);
        patchPlayerHere = GetNode<Control>(PatchPlayerHerePath);
        patchBiome = GetNode<Label>(PatchBiomePath);
        patchDepth = GetNode<Label>(PatchDepthPath);
        patchTemperature = GetNode<Label>(PatchTemperaturePath);
        patchPressure = GetNode<Label>(PatchPressurePath);
        patchLight = GetNode<Label>(PatchLightPath);
        patchOxygen = GetNode<Label>(PatchOxygenPath);
        patchNitrogen = GetNode<Label>(PatchNitrogenPath);
        patchCO2 = GetNode<Label>(PatchCO2Path);
        patchHydrogenSulfide = GetNode<Label>(PatchHydrogenSulfidePath);
        patchAmmonia = GetNode<Label>(PatchAmmoniaPath);
        patchGlucose = GetNode<Label>(PatchGlucosePath);
        patchPhosphate = GetNode<Label>(PatchPhosphatePath);
        patchIron = GetNode<Label>(PatchIronPath);
        speciesListBox = GetNode<CollapsibleList>(SpeciesCollapsibleBoxPath);
        moveToPatchButton = GetNode<Button>(MoveToPatchButtonPath);
        symmetryIcon = GetNode<TextureRect>(SymmetryIconPath);

        physicalConditionsIconLegends = GetNode<HBoxContainer>(PhysicalConditionsIconLegendPath);
        temperatureChart = GetNode<LineChart>(TemperatureChartPath);
        sunlightChart = GetNode<LineChart>(SunlightChartPath);
        atmosphericGassesChart = GetNode<LineChart>(AtmosphericGassesChartPath);
        compoundsChart = GetNode<LineChart>(CompoundsChartPath);
        speciesPopulationChart = GetNode<LineChart>(SpeciesPopulationChartPath);

        patchTemperatureSituation = GetNode<TextureRect>(PatchTemperatureSituationPath);
        patchLightSituation = GetNode<TextureRect>(PatchLightSituationPath);
        patchHydrogenSulfideSituation = GetNode<TextureRect>(PatchHydrogenSulfideSituationPath);
        patchGlucoseSituation = GetNode<TextureRect>(PatchGlucoseSituationPath);
        patchIronSituation = GetNode<TextureRect>(PatchIronSituationPath);
        patchAmmoniaSituation = GetNode<TextureRect>(PatchAmmoniaSituationPath);
        patchPhosphateSituation = GetNode<TextureRect>(PatchPhosphateSituationPath);

        speedIndicator = GetNode<TextureRect>(SpeedIndicatorPath);
        hpIndicator = GetNode<TextureRect>(HpIndicatorPath);
        sizeIndicator = GetNode<TextureRect>(SizeIndicatorPath);

        symmetryIconDefault = GD.Load<Texture>("res://assets/textures/gui/bevel/1xSymmetry.png");
        symmetryIcon2X = GD.Load<Texture>("res://assets/textures/gui/bevel/2xSymmetry.png");
        symmetryIcon4X = GD.Load<Texture>("res://assets/textures/gui/bevel/4xSymmetry.png");
        symmetryIcon6X = GD.Load<Texture>("res://assets/textures/gui/bevel/6xSymmetry.png");
        increaseIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/increase.png");
        decreaseIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/decrease.png");
        unableToPlaceHexSound = GD.Load<AudioStream>("res://assets/sounds/soundeffects/gui/click_place_blocked.ogg");
        temperatureIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/Temperature.png");

        negativeAtpPopup = GetNode<ConfirmationDialog>(NegativeAtpPopupPath);
        islandPopup = GetNode<AcceptDialog>(IslandErrorPath);
        organelleMenu = GetNode<OrganellePopupMenu>(OrganelleMenuPath);

        compoundBalance = GetNode<CompoundBalanceDisplay>(CompoundBalancePath);

        menu = GetNode<PauseMenu>(MenuPath);

        mapDrawer.OnSelectedPatchChanged = drawer => { UpdateShownPatchDetails(); };

        atpProductionBar.SelectedType = SegmentedBar.Type.ATP;
        atpProductionBar.IsProduction = true;
        atpConsumptionBar.SelectedType = SegmentedBar.Type.ATP;

        SetupMicrobePartSelections();
        UpdateMicrobePartSelections();

        RegisterTooltips();
    }

    public void Init(MicrobeEditor editor)
    {
        this.editor = editor ?? throw new ArgumentNullException(nameof(editor));

        // Set the right tabs if they aren't the defaults
        ApplyEditorTab();
        ApplySelectionMenuTab();

        UpdateMutationPointsBar();
    }

    public void SetMap(PatchMap map)
    {
        mapDrawer.Map = map;
    }

    public void UpdatePlayerPatch(Patch patch)
    {
        mapDrawer.PlayerPatch = patch ?? editor.CurrentPatch;

        // Just in case this didn't get called already. Note that this may result in duplicate calls here
        UpdateShownPatchDetails();
    }

    public void UpdateGlucoseReduction(float value)
    {
        var percentage = string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("PERCENTAGE_VALUE"),
            value * 100);

        // The amount of glucose has been reduced to {0} of the previous amount.
        glucoseReductionLabel.Text =
            string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("THE_AMOUNT_OF_GLUCOSE_HAS_BEEN_REDUCED"),
                percentage);
    }

    public void UpdateTimeIndicator(double value)
    {
        timeIndicator.Text = string.Format(CultureInfo.CurrentCulture, "{0:#,##0,,}", value) + " "
            + TranslationServer.Translate("MEGA_YEARS");

        ToolTipManager.Instance.GetToolTip("timeIndicator", "editor").Description = string.Format(
                CultureInfo.CurrentCulture, "{0:#,#}", editor.CurrentGame.GameWorld.TotalPassedTime) + " "
            + TranslationServer.Translate("YEARS");
    }

    public void SetInitialCellStats()
    {
        initialCellSpeed = editor.CalculateSpeed();
        initialCellHp = editor.CalculateHitpoints();
        initialCellSize = editor.MicrobeHexSize;
    }

    public void UpdateSize(int size)
    {
        sizeLabel.Text = size.ToString(CultureInfo.CurrentCulture);

        UpdateCellStatsIndicators();
    }

    public void UpdateGeneration(int generation)
    {
        generationLabel.Text = generation.ToString(CultureInfo.CurrentCulture);
    }

    public void UpdateSpeed(float speed)
    {
        speedLabel.Text = string.Format(CultureInfo.CurrentCulture, "{0:F1}", speed);

        UpdateCellStatsIndicators();
    }

    public void UpdateHitpoints(float hp)
    {
        hpLabel.Text = hp.ToString(CultureInfo.CurrentCulture);

        UpdateCellStatsIndicators();
    }

    public void UpdateEnergyBalance(EnergyBalanceInfo energyBalance)
    {
        energyBalanceInfo = energyBalance;

        if (energyBalance.FinalBalance > 0)
        {
            atpBalanceLabel.Text = TranslationServer.Translate("ATP_PRODUCTION");
            atpBalanceLabel.AddColorOverride("font_color", new Color(1.0f, 1.0f, 1.0f));
        }
        else
        {
            atpBalanceLabel.Text = TranslationServer.Translate("ATP_PRODUCTION") + " - " +
                TranslationServer.Translate("ATP_PRODUCTION_TOO_LOW");
            atpBalanceLabel.AddColorOverride("font_color", new Color(1.0f, 0.2f, 0.2f));
        }

        atpProductionLabel.Text = string.Format(CultureInfo.CurrentCulture, "{0:F1}", energyBalance.TotalProduction);
        atpConsumptionLabel.Text = string.Format(CultureInfo.CurrentCulture, "{0:F1}", energyBalance.TotalConsumption);

        float maxValue = Math.Max(energyBalance.TotalConsumption, energyBalance.TotalProduction);
        atpProductionBar.MaxValue = maxValue;
        atpConsumptionBar.MaxValue = maxValue;

        atpProductionBar.UpdateAndMoveBars(SortBarData(energyBalance.Production));
        atpConsumptionBar.UpdateAndMoveBars(SortBarData(energyBalance.Consumption));

        UpdateEnergyBalanceToolTips(energyBalance);
    }

    public void UpdateEnergyBalanceToolTips(EnergyBalanceInfo energyBalance)
    {
        foreach (var subBar in atpProductionBar.SubBars)
        {
            var tooltip = ToolTipManager.Instance.GetToolTip(subBar.Name, "processesProduction");

            subBar.RegisterToolTipForControl(tooltip);

            tooltip.Description = string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("ENERGY_BALANCE_TOOLTIP_PRODUCTION"),
                SimulationParameters.Instance.GetOrganelleType(subBar.Name).Name,
                energyBalance.Production[subBar.Name]);
        }

        foreach (var subBar in atpConsumptionBar.SubBars)
        {
            var tooltip = ToolTipManager.Instance.GetToolTip(subBar.Name, "processesConsumption");

            subBar.RegisterToolTipForControl(tooltip);

            string displayName;

            switch (subBar.Name)
            {
                case "osmoregulation":
                {
                    displayName = TranslationServer.Translate("OSMOREGULATION");
                    break;
                }

                case "baseMovement":
                {
                    displayName = TranslationServer.Translate("BASE_MOVEMENT");
                    break;
                }

                default:
                {
                    displayName = SimulationParameters.Instance.GetOrganelleType(subBar.Name).Name;
                    break;
                }
            }

            tooltip.Description = string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("ENERGY_BALANCE_TOOLTIP_CONSUMPTION"), displayName,
                energyBalance.Consumption[subBar.Name]);
        }
    }

    // Disable this because the cleanup and inspections disagree
    // ReSharper disable once RedundantNameQualifier
    /// <summary>
    ///   Updates the organelle efficiencies in tooltips.
    /// </summary>
    public void UpdateOrganelleEfficiencies(
        System.Collections.Generic.Dictionary<string, OrganelleEfficiency> organelleEfficiency)
    {
        foreach (var organelle in organelleEfficiency.Keys)
        {
            if (organelle == protoplasm.InternalName)
                continue;

            var tooltip = (SelectionMenuToolTip)ToolTipManager.Instance.GetToolTip(
                SimulationParameters.Instance.GetOrganelleType(organelle).InternalName, "organelleSelection");

            tooltip?.WriteOrganelleProcessList(organelleEfficiency[organelle].Processes);
        }
    }

    // Disable this because the cleanup and inspections disagree
    // ReSharper disable once RedundantNameQualifier
    public void UpdateCompoundBalances(System.Collections.Generic.Dictionary<Compound, CompoundBalance> balances)
    {
        compoundBalance.UpdateBalances(balances);
    }

    public void UpdateReportTabStatistics(Patch patch)
    {
        temperatureChart.ClearDataSets();
        sunlightChart.ClearDataSets();
        atmosphericGassesChart.ClearDataSets();
        compoundsChart.ClearDataSets();
        speciesPopulationChart.ClearDataSets();

        // Initialize datasets
        var temperatureData = new LineChartData
        {
            IconTexture = temperatureIcon,
            DataColour = new Color(0.67f, 1, 0.24f),
        };

        temperatureChart.AddDataSet(TranslationServer.Translate("TEMPERATURE"), temperatureData);

        foreach (var snapshot in patch.History)
        {
            foreach (var entry in snapshot.Biome.Compounds)
            {
                var dataset = new LineChartData
                {
                    IconTexture = entry.Key.LoadedIcon,
                    DataColour = entry.Key.Colour,
                };

                GetChartForCompound(entry.Key.InternalName)?.AddDataSet(entry.Key.Name, dataset);
            }

            foreach (var entry in snapshot.SpeciesInPatch)
            {
                var dataset = new LineChartData { DataColour = entry.Key.Colour };
                speciesPopulationChart.AddDataSet(entry.Key.FormattedName, dataset);
            }
        }

        // Populate charts with data from patch history
        foreach (var snapshot in patch.History)
        {
            temperatureData.AddPoint(new DataPoint
            {
                Value = new Vector2((float)snapshot.TimePeriod, snapshot.Biome.AverageTemperature),
                MarkerColour = temperatureData.DataColour,
            });

            foreach (var entry in snapshot.Biome.Compounds)
            {
                var dataset = GetChartForCompound(entry.Key.InternalName)?.GetDataSet(entry.Key.Name);

                if (dataset == null)
                    continue;

                var dataPoint = new DataPoint
                {
                    Value = new Vector2((float)snapshot.TimePeriod, GetCompoundAmount(
                        patch, snapshot.Biome, entry.Key.InternalName)),
                    MarkerColour = dataset.DataColour,
                };

                dataset.AddPoint(dataPoint);
            }

            foreach (var entry in snapshot.SpeciesInPatch)
            {
                var dataset = speciesPopulationChart.GetDataSet(entry.Key.FormattedName);

                var extinctInPatch = entry.Value <= 0;
                var extinctEverywhere = false;

                // We test if the species info was recorded before using it.
                // This is especially for compatibility with older versions, to avoid crashed due to an invalid key.
                // TODO: Use a proper save upgrade (e.g. summing population to generate info).
                if (snapshot.RecordedSpeciesInfo.TryGetValue(entry.Key, out SpeciesInfo speciesInfo))
                {
                    extinctEverywhere = speciesInfo.Population <= 0;
                }

                // Clamp population number so it doesn't go into the negatives
                var population = extinctInPatch ? 0 : entry.Value;

                var iconType = DataPoint.MarkerIcon.Circle;
                var iconSize = 7;

                if (extinctInPatch)
                {
                    if (extinctEverywhere)
                    {
                        iconType = DataPoint.MarkerIcon.Skull;
                        iconSize = 28;
                    }
                    else
                    {
                        iconType = DataPoint.MarkerIcon.Cross;
                        iconSize = 12;
                    }
                }

                var dataPoint = new DataPoint
                {
                    Value = new Vector2((float)snapshot.TimePeriod, population),
                    Size = iconSize,
                    IconType = iconType,
                    MarkerColour = dataset.DataColour,
                };

                dataset.AddPoint(dataPoint);
            }
        }

        var percentageFormat = TranslationServer.Translate("PERCENTAGE_VALUE");

        sunlightChart.TooltipYAxisFormat = percentageFormat + " lx";
        atmosphericGassesChart.TooltipYAxisFormat = percentageFormat;
        compoundsChart.TooltipYAxisFormat = percentageFormat;

        sunlightChart.Plot(TranslationServer.Translate("YEARS"), "% lx", 5);
        temperatureChart.Plot(TranslationServer.Translate("YEARS"), "°C", 5);
        atmosphericGassesChart.Plot(
            TranslationServer.Translate("YEARS"), "%", 5, TranslationServer.Translate("ATMOSPHERIC_GASSES"));
        speciesPopulationChart.Plot(
            TranslationServer.Translate("YEARS"), string.Empty, 5, TranslationServer.Translate("SPECIES_LIST"),
            editor.CurrentGame.GameWorld.PlayerSpecies.FormattedName);
        compoundsChart.Plot(
            TranslationServer.Translate("YEARS"), "%", 5, TranslationServer.Translate("COMPOUNDS"));

        OnPhysicalConditionsChartLegendPressed("temperature");
    }

    public void UpdateMutationPointsBar()
    {
        // Update mutation points
        float possibleMutationPoints = editor.FreeBuilding ?
            Constants.BASE_MUTATION_POINTS :
            editor.MutationPoints - editor.CalculateCurrentOrganelleCost(MouseHoverHexes);

        GUICommon.Instance.TweenBarValue(
            mutationPointsBar, possibleMutationPoints, Constants.BASE_MUTATION_POINTS, 0.5f);
        GUICommon.Instance.TweenBarValue(
            mutationPointsSubtractBar, editor.MutationPoints, Constants.BASE_MUTATION_POINTS, 0.7f);

        if (editor.FreeBuilding)
        {
            mutationPointsArrow.Hide();
            resultingMutationPointsLabel.Hide();
            baseMutationPointsLabel.Hide();

            currentMutationPointsLabel.Text = TranslationServer.Translate("FREEBUILDING");
        }
        else
        {
            if (editor.ShowHover && editor.MutationPoints > 0)
            {
                mutationPointsArrow.Show();
                resultingMutationPointsLabel.Show();

                currentMutationPointsLabel.Text = $"({editor.MutationPoints:F0}";
                resultingMutationPointsLabel.Text = $"{possibleMutationPoints:F0})";
                baseMutationPointsLabel.Text = $"/ {Constants.BASE_MUTATION_POINTS:F0}";
            }
            else
            {
                mutationPointsArrow.Hide();
                resultingMutationPointsLabel.Hide();

                currentMutationPointsLabel.Text = $"{editor.MutationPoints:F0}";
                baseMutationPointsLabel.Text = $"/ {Constants.BASE_MUTATION_POINTS:F0}";
            }
        }

        mutationPointsSubtractBar.SelfModulate = possibleMutationPoints < 0 ?
            new Color(0.72f, 0.19f, 0.19f) :
            new Color(0.72f, 0.72f, 0.72f);
    }

    public void SetMembraneTooltips(MembraneType referenceMembrane)
    {
        // Pass in a membrane that the values are taken as relative to
        foreach (var membraneType in SimulationParameters.Instance.GetAllMembranes())
        {
            var tooltip = (SelectionMenuToolTip)ToolTipManager.Instance.GetToolTip(
                membraneType.InternalName, "membraneSelection");

            tooltip?.WriteMembraneModifierList(referenceMembrane, membraneType);
        }
    }

    /// <summary>
    ///   Updates the fluidity / rigidity slider tooltip
    /// </summary>
    public void SetRigiditySliderTooltip(int rigidity)
    {
        float convertedRigidity = rigidity / Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO;

        var rigidityTooltip = (SelectionMenuToolTip)ToolTipManager.Instance.GetToolTip("rigiditySlider", "editor");

        var healthModifier = rigidityTooltip.GetModifierInfo("health");
        var mobilityModifier = rigidityTooltip.GetModifierInfo("mobility");

        float healthChange = convertedRigidity * Constants.MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER;
        float mobilityChange = -1 * convertedRigidity * Constants.MEMBRANE_RIGIDITY_MOBILITY_MODIFIER;

        healthModifier.ModifierValue = ((healthChange >= 0) ? "+" : string.Empty)
            + healthChange.ToString("F0", CultureInfo.CurrentCulture);

        mobilityModifier.ModifierValue = ((mobilityChange >= 0) ? "+" : string.Empty)
            + mobilityChange.ToString("P0", CultureInfo.CurrentCulture);

        healthModifier.AdjustValueColor(healthChange);
        mobilityModifier.AdjustValueColor(mobilityChange);
    }

    public void UpdateAutoEvoResults(string results, string external)
    {
        autoEvoLabel.Text = results;
        externalEffectsLabel.Text = external;
    }

    public void UpdateReportTabPatchName(string patch)
    {
        reportTabPatchNameLabel.Text = patch;
    }

    public void UpdateRigiditySliderState(int mutationPoints)
    {
        if (mutationPoints >= Constants.MEMBRANE_RIGIDITY_COST_PER_STEP && editor.MovingOrganelle == null)
        {
            rigiditySlider.Editable = true;
        }
        else
        {
            rigiditySlider.Editable = false;
        }
    }

    /// <summary>
    ///   Updates patch-specific GUI elements with data from a patch
    /// </summary>
    public void UpdatePatchDetails(Patch patch)
    {
        patchName.Text = TranslationServer.Translate(patch.Name);

        // Biome: {0}
        patchBiome.Text = string.Format(CultureInfo.CurrentCulture,
            TranslationServer.Translate("BIOME_LABEL"),
            patch.BiomeTemplate.Name);

        // {0}-{1}m below sea level
        patchDepth.Text = string.Format(CultureInfo.CurrentCulture,
            TranslationServer.Translate("BELOW_SEA_LEVEL"),
            patch.Depth[0], patch.Depth[1]);
        patchPlayerHere.Visible = editor.CurrentPatch == patch;

        var percentageFormat = TranslationServer.Translate("PERCENTAGE_VALUE");

        // Atmospheric gasses
        patchTemperature.Text = patch.Biome.AverageTemperature + " °C";
        patchPressure.Text = "20 bar";
        patchLight.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            GetCompoundAmount(patch, sunlight.InternalName)) + " lx";
        patchOxygen.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            GetCompoundAmount(patch, oxygen.InternalName));
        patchNitrogen.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            GetCompoundAmount(patch, nitrogen.InternalName));
        patchCO2.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            GetCompoundAmount(patch, carbondioxide.InternalName));

        // Compounds
        patchHydrogenSulfide.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            Math.Round(GetCompoundAmount(patch, hydrogensulfide.InternalName), 3));
        patchAmmonia.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            Math.Round(GetCompoundAmount(patch, ammonia.InternalName), 3));
        patchGlucose.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            Math.Round(GetCompoundAmount(patch, glucose.InternalName), 3));
        patchPhosphate.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            Math.Round(GetCompoundAmount(patch, phosphates.InternalName), 3));
        patchIron.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            GetCompoundAmount(patch, iron.InternalName));

        // Refresh species list
        speciesListBox.ClearItems();

        foreach (var species in patch.SpeciesInPatch.Keys)
        {
            var speciesLabel = new Label();
            speciesLabel.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
            speciesLabel.Autowrap = true;
            speciesLabel.Text = string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("WITH_POPULATION"), species.FormattedName,
                patch.GetSpeciesPopulation(species));
            speciesListBox.AddItem(speciesLabel);
        }

        UpdateConditionDifferencesBetweenPatches(patch, editor.CurrentPatch);

        UpdateReportTabStatistics(patch);

        UpdateReportTabPatchName(TranslationServer.Translate(patch.Name));
    }

    /// <summary>
    ///   Updates the values of all part selections from their associated part types.
    /// </summary>
    public void UpdateMicrobePartSelections()
    {
        foreach (var entry in placeablePartSelectionElements)
        {
            entry.Value.PartName = entry.Key.Name;
            entry.Value.MPCost = entry.Key.MPCost;
            entry.Value.PartIcon = entry.Key.LoadedIcon;
        }

        foreach (var entry in membraneSelectionElements)
        {
            entry.Value.PartName = entry.Key.Name;
            entry.Value.MPCost = entry.Key.EditorCost;
            entry.Value.PartIcon = entry.Key.LoadedIcon;
        }
    }

    /// <summary>
    ///   Updates the visibility of the current action cancel button.
    /// </summary>
    public void UpdateCancelButtonVisibility()
    {
        cancelButton.Visible = editor.CanCancelAction;
    }

    public void ShowOrganelleMenu(OrganelleTemplate selectedOrganelle)
    {
        organelleMenu.SelectedOrganelle = selectedOrganelle;
        organelleMenu.ShowPopup = true;

        // Disable delete for nucleus or the last organelle.
        if (editor.MicrobeSize < 2 || selectedOrganelle.Definition == nucleus)
        {
            organelleMenu.EnableDeleteOption = false;
        }
        else
        {
            organelleMenu.EnableDeleteOption = true;
        }

        // Move enabled only when microbe has more than one organelle
        organelleMenu.EnableMoveOption = editor.MicrobeSize > 1;
    }

    public void OnMovePressed()
    {
        editor.StartOrganelleMove(organelleMenu.SelectedOrganelle);

        // Once an organelle move has begun, the button visibility should be updated so it becomes visible
        UpdateCancelButtonVisibility();
    }

    public void OnDeletePressed()
    {
        editor.RemoveOrganelle(organelleMenu.SelectedOrganelle.Position);
    }

    /// <summary>
    ///   Called once when the mouse enters the editor GUI.
    /// </summary>
    internal void OnMouseEnter()
    {
        editor.ShowHover = false;
        UpdateMutationPointsBar();
    }

    /// <summary>
    ///   Called when the mouse is no longer hovering the editor GUI.
    /// </summary>
    internal void OnMouseExit()
    {
        editor.ShowHover = selectedEditorTab == EditorTab.CellEditor;
        UpdateMutationPointsBar();
    }

    internal void SetUndoButtonStatus(bool enabled)
    {
        undoButton.Disabled = !enabled;
    }

    internal void SetRedoButtonStatus(bool enabled)
    {
        redoButton.Disabled = !enabled;
    }

    internal void NotifyFreebuild(bool freebuilding)
    {
        if (freebuilding)
        {
            newCellButton.Disabled = false;
        }
        else
        {
            newCellButton.Disabled = true;
        }
    }

    internal void OnNewCellClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();

        editor.CreateNewMicrobe();
    }

    internal void OnInvalidHexLocationSelected()
    {
        if (selectedEditorTab != EditorTab.CellEditor)
            return;

        GUICommon.Instance.PlayCustomSound(unableToPlaceHexSound);
    }

    internal void OnInsufficientMp(bool playSound = true)
    {
        if (selectedEditorTab != EditorTab.CellEditor)
            return;

        var animationPlayer = mutationPointsBar.GetNode<AnimationPlayer>("FlashAnimation");
        animationPlayer.Play("FlashBar");

        if (playSound)
            PlayInvalidActionSound();
    }

    internal void OnActionBlockedWhileMoving()
    {
        PlayInvalidActionSound();
    }

    internal void PlayInvalidActionSound()
    {
        GUICommon.Instance.PlayCustomSound(unableToPlaceHexSound);
    }

    /// <summary>
    ///   Lock / unlock the organelles that need a nucleus
    /// </summary>
    internal void UpdatePartsAvailability(List<OrganelleDefinition> placedUniqueOrganelleNames)
    {
        foreach (var organelle in placeablePartSelectionElements.Keys)
        {
            UpdatePartAvailability(placedUniqueOrganelleNames, organelle);
        }
    }

    internal void OnOrganelleToPlaceSelected(string organelle)
    {
        editor.ActiveActionName = organelle;

        // Update the icon highlightings
        foreach (var element in placeablePartSelectionElements.Values)
        {
            element.Selected = element.Name == organelle;
        }

        GD.Print("Editor action is now: " + editor.ActiveActionName);
    }

    internal void OnCancelActionClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();
        editor.CancelCurrentAction();
    }

    internal void OnFinishEditingClicked()
    {
        // Prevent exiting when the transition hasn't finished
        if (!editor.TransitionFinished)
        {
            return;
        }

        // Can't finish an organism edit if an organelle is being moved
        if (editor.MovingOrganelle != null)
        {
            OnActionBlockedWhileMoving();
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();

        // Can't exit the editor with disconnected organelles
        if (editor.HasIslands)
        {
            islandPopup.PopupCenteredShrink();
            return;
        }

        // Show warning popup if trying to exit with negative atp production
        if (energyBalanceInfo != null &&
            energyBalanceInfo.TotalProduction < energyBalanceInfo.TotalConsumptionStationary)
        {
            negativeAtpPopup.PopupCenteredShrink();
            return;
        }

        // To prevent being clicked twice
        finishButton.MouseFilter = Control.MouseFilterEnum.Ignore;

        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeOut, 0.3f, false);
        TransitionManager.Instance.StartTransitions(editor, nameof(MicrobeEditor.OnFinishEditing));
    }

    internal void ConfirmFinishEditingPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeOut, 0.3f, false);
        TransitionManager.Instance.StartTransitions(editor, nameof(MicrobeEditor.OnFinishEditing));
    }

    internal void OnSymmetryClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (symmetry == MicrobeEditor.MicrobeSymmetry.SixWaySymmetry)
        {
            ResetSymmetryButton();
        }
        else if (symmetry == MicrobeEditor.MicrobeSymmetry.None)
        {
            symmetry = MicrobeEditor.MicrobeSymmetry.XAxisSymmetry;
        }
        else if (symmetry == MicrobeEditor.MicrobeSymmetry.XAxisSymmetry)
        {
            symmetry = MicrobeEditor.MicrobeSymmetry.FourWaySymmetry;
        }
        else if (symmetry == MicrobeEditor.MicrobeSymmetry.FourWaySymmetry)
        {
            symmetry = MicrobeEditor.MicrobeSymmetry.SixWaySymmetry;
        }

        editor.Symmetry = symmetry;
        UpdateSymmetryIcon();
    }

    internal void OnSymmetryHold()
    {
        symmetryIcon.Modulate = new Color(0, 0, 0);
    }

    internal void OnSymmetryReleased()
    {
        symmetryIcon.Modulate = new Color(1, 1, 1);
    }

    internal void ResetSymmetryButton()
    {
        symmetryIcon.Texture = symmetryIconDefault;
        symmetry = 0;
    }

    internal void SetSymmetry(MicrobeEditor.MicrobeSymmetry newSymmetry)
    {
        symmetry = newSymmetry;
        editor.Symmetry = newSymmetry;

        UpdateSymmetryIcon();
    }

    internal void HelpButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        OpenMenu();
        menu.ShowHelpScreen();
    }

    internal void OnMembraneSelected(string membrane)
    {
        editor.SetMembrane(membrane);
    }

    internal void SetSpeciesInfo(string name, MembraneType membrane, Color colour,
        float rigidity)
    {
        speciesNameEdit.Text = name;
        membraneColorPicker.Color = colour;

        // Callback is manually called because the function isn't called automatically here
        OnSpeciesNameTextChanged(name);

        UpdateMembraneButtons(membrane.InternalName);
        SetMembraneTooltips(membrane);

        UpdateRigiditySlider((int)Math.Round(rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO));
    }

    internal void UpdateMembraneButtons(string membrane)
    {
        // Update the icon highlightings
        foreach (var selection in membraneSelectionElements.Values)
        {
            selection.Selected = selection.Name == membrane;
        }
    }

    internal void UpdateRigiditySlider(int value)
    {
        rigiditySlider.Value = value;
        SetRigiditySliderTooltip(value);
    }

    internal void SendUndoToTutorial(TutorialState tutorial)
    {
        if (tutorial.EditorUndoTutorial == null)
            return;

        tutorial.EditorUndoTutorial.EditorUndoButtonControl = undoButton;
    }

    private void UpdateSymmetryIcon()
    {
        switch (symmetry)
        {
            case MicrobeEditor.MicrobeSymmetry.None:
                symmetryIcon.Texture = symmetryIconDefault;
                break;
            case MicrobeEditor.MicrobeSymmetry.XAxisSymmetry:
                symmetryIcon.Texture = symmetryIcon2X;
                break;
            case MicrobeEditor.MicrobeSymmetry.FourWaySymmetry:
                symmetryIcon.Texture = symmetryIcon4X;
                break;
            case MicrobeEditor.MicrobeSymmetry.SixWaySymmetry:
                symmetryIcon.Texture = symmetryIcon6X;
                break;
        }
    }

    /// <summary>
    ///   Lock / unlock a single organelle that need a nucleus
    /// </summary>
    private void UpdatePartAvailability(List<OrganelleDefinition> placedUniqueOrganelleNames,
        OrganelleDefinition organelle)
    {
        var item = placeablePartSelectionElements[organelle];

        if (organelle.Unique && placedUniqueOrganelleNames.Contains(organelle))
        {
            item.Locked = true;
        }
        else if (organelle.RequiresNucleus)
        {
            var hasNucleus = placedUniqueOrganelleNames.Contains(nucleus);
            item.Locked = !hasNucleus;
        }
        else
        {
            item.Locked = false;
        }
    }

    /// <summary>
    ///   Associates all existing cell part selections with their respective part types based on their Node names.
    /// </summary>
    private void SetupMicrobePartSelections()
    {
        var organelleSelections = GetTree().GetNodesInGroup(
            "PlaceablePartSelectionElement").Cast<MicrobePartSelection>().ToList();
        var membraneSelections = GetTree().GetNodesInGroup(
            "MembraneSelectionElement").Cast<MicrobePartSelection>().ToList();

        foreach (var entry in organelleSelections)
        {
            // Special case with registering the tooltip here for item with no associated organelle
            entry.RegisterToolTipForControl(entry.Name, "organelleSelection");

            if (!SimulationParameters.Instance.DoesOrganelleExist(entry.Name))
            {
                entry.Locked = true;
                continue;
            }

            var organelle = SimulationParameters.Instance.GetOrganelleType(entry.Name);

            // Only add items with valid organelles to dictionary
            placeablePartSelectionElements.Add(organelle, entry);

            entry.Connect(
                nameof(MicrobePartSelection.OnPartSelected), this, nameof(OnOrganelleToPlaceSelected));
        }

        foreach (var entry in membraneSelections)
        {
            // Special case with registering the tooltip here for item with no associated membrane
            entry.RegisterToolTipForControl(entry.Name, "membraneSelection");

            if (!SimulationParameters.Instance.DoesMembraneExist(entry.Name))
            {
                entry.Locked = true;
                continue;
            }

            var membrane = SimulationParameters.Instance.GetMembrane(entry.Name);

            // Only add items with valid membranes to dictionary
            membraneSelectionElements.Add(membrane, entry);

            entry.Connect(
                nameof(MicrobePartSelection.OnPartSelected), this, nameof(OnMembraneSelected));
        }
    }

    private void OnRigidityChanged(int value)
    {
        editor.SetRigidity(value);
    }

    private void OnColorChanged(Color color)
    {
        editor.Colour = color;
    }

    private void MoveToPatchClicked()
    {
        var target = mapDrawer.SelectedPatch;

        if (editor.IsPatchMoveValid(target))
            editor.SetPlayerPatch(target);
    }

    private void SetEditorTab(string tab)
    {
        var selection = (EditorTab)Enum.Parse(typeof(EditorTab), tab);

        if (selection == selectedEditorTab)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        selectedEditorTab = selection;

        ApplyEditorTab();

        editor.TutorialState.SendEvent(TutorialEventType.MicrobeEditorTabChanged, new StringEventArgs(tab), this);
    }

    private void ApplyEditorTab()
    {
        // Hide all
        var cellEditor = GetNode<Control>("CellEditor");
        var report = GetNode<Control>("Report");
        var patchMap = GetNode<Control>("PatchMap");

        report.Hide();
        patchMap.Hide();
        cellEditor.Hide();

        // Show selected
        switch (selectedEditorTab)
        {
            case EditorTab.Report:
            {
                report.Show();
                reportTabButton.Pressed = true;
                editor.SetEditorCellVisibility(false);
                break;
            }

            case EditorTab.PatchMap:
            {
                patchMap.Show();
                patchMapButton.Pressed = true;
                editor.SetEditorCellVisibility(false);
                break;
            }

            case EditorTab.CellEditor:
            {
                cellEditor.Show();
                cellEditorButton.Pressed = true;
                editor.SetEditorCellVisibility(true);
                break;
            }

            default:
                throw new Exception("Invalid editor tab");
        }
    }

    private void SetSelectionMenuTab(string tab)
    {
        var selection = (SelectionMenuTab)Enum.Parse(typeof(SelectionMenuTab), tab);

        if (selection == selectedSelectionMenuTab)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        selectedSelectionMenuTab = selection;
        ApplySelectionMenuTab();
    }

    private void ApplySelectionMenuTab()
    {
        // Hide all
        structureTab.Hide();
        appearanceTab.Hide();

        // Show selected
        switch (selectedSelectionMenuTab)
        {
            case SelectionMenuTab.Structure:
            {
                structureTab.Show();
                structureTabButton.Pressed = true;
                editor.MicrobePreviewMode = false;
                break;
            }

            case SelectionMenuTab.Membrane:
            {
                appearanceTab.Show();
                appearanceTabButton.Pressed = true;
                editor.MicrobePreviewMode = true;
                break;
            }

            default:
                throw new Exception("Invalid selection menu tab");
        }
    }

    private void MenuButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        OpenMenu();
    }

    private void OpenMenu()
    {
        menu.Show();
        GetTree().Paused = true;
    }

    private void CloseMenu()
    {
        menu.Hide();
        GetTree().Paused = false;
    }

    private void ExitPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        GetTree().Quit();
    }

    private void UpdateCellStatsIndicators()
    {
        sizeIndicator.Show();

        if (editor.MicrobeHexSize > initialCellSize)
        {
            sizeIndicator.Texture = increaseIcon;
        }
        else if (editor.MicrobeHexSize < initialCellSize)
        {
            sizeIndicator.Texture = decreaseIcon;
        }
        else
        {
            sizeIndicator.Hide();
        }

        speedIndicator.Show();

        if (editor.CalculateSpeed() > initialCellSpeed)
        {
            speedIndicator.Texture = increaseIcon;
        }
        else if (editor.CalculateSpeed() < initialCellSpeed)
        {
            speedIndicator.Texture = decreaseIcon;
        }
        else
        {
            speedIndicator.Hide();
        }

        hpIndicator.Show();

        if (editor.CalculateHitpoints() > initialCellHp)
        {
            hpIndicator.Texture = increaseIcon;
        }
        else if (editor.CalculateHitpoints() < initialCellHp)
        {
            hpIndicator.Texture = decreaseIcon;
        }
        else
        {
            hpIndicator.Hide();
        }
    }

    /// <remarks>
    ///   TODO: this function should be cleaned up by generalizing the adding
    ///   the increase or decrease icons in order to remove the duplicated
    ///   logic here
    /// </remarks>
    private void UpdateConditionDifferencesBetweenPatches(Patch selectedPatch, Patch currentPatch)
    {
        var nextCompound = selectedPatch.Biome.AverageTemperature;

        if (nextCompound > currentPatch.Biome.AverageTemperature)
        {
            patchTemperatureSituation.Texture = increaseIcon;
        }
        else if (nextCompound < currentPatch.Biome.AverageTemperature)
        {
            patchTemperatureSituation.Texture = decreaseIcon;
        }
        else
        {
            patchTemperatureSituation.Texture = null;
        }

        nextCompound = selectedPatch.Biome.Compounds[sunlight].Dissolved;

        if (nextCompound > currentPatch.Biome.Compounds[sunlight].Dissolved)
        {
            patchLightSituation.Texture = increaseIcon;
        }
        else if (nextCompound < currentPatch.Biome.Compounds[sunlight].Dissolved)
        {
            patchLightSituation.Texture = decreaseIcon;
        }
        else
        {
            patchLightSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(selectedPatch, hydrogensulfide.InternalName);

        if (nextCompound > GetCompoundAmount(currentPatch, hydrogensulfide.InternalName))
        {
            patchHydrogenSulfideSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(currentPatch, hydrogensulfide.InternalName))
        {
            patchHydrogenSulfideSituation.Texture = decreaseIcon;
        }
        else
        {
            patchHydrogenSulfideSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(selectedPatch, glucose.InternalName);

        if (nextCompound > GetCompoundAmount(currentPatch, glucose.InternalName))
        {
            patchGlucoseSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(currentPatch, glucose.InternalName))
        {
            patchGlucoseSituation.Texture = decreaseIcon;
        }
        else
        {
            patchGlucoseSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(selectedPatch, iron.InternalName);

        if (nextCompound > GetCompoundAmount(currentPatch, iron.InternalName))
        {
            patchIronSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(currentPatch, iron.InternalName))
        {
            patchIronSituation.Texture = decreaseIcon;
        }
        else
        {
            patchIronSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(selectedPatch, ammonia.InternalName);

        if (nextCompound > GetCompoundAmount(currentPatch, ammonia.InternalName))
        {
            patchAmmoniaSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(currentPatch, ammonia.InternalName))
        {
            patchAmmoniaSituation.Texture = decreaseIcon;
        }
        else
        {
            patchAmmoniaSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(selectedPatch, phosphates.InternalName);

        if (nextCompound > GetCompoundAmount(currentPatch, phosphates.InternalName))
        {
            patchPhosphateSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(currentPatch, phosphates.InternalName))
        {
            patchPhosphateSituation.Texture = decreaseIcon;
        }
        else
        {
            patchPhosphateSituation.Texture = null;
        }
    }

    private void UpdateShownPatchDetails()
    {
        var patch = mapDrawer.SelectedPatch;

        editor.TutorialState.SendEvent(TutorialEventType.MicrobeEditorPatchSelected, new PatchEventArgs(patch), this);

        if (patch == null)
        {
            patchDetails.Visible = false;
            patchNothingSelected.Visible = true;

            return;
        }

        patchDetails.Visible = true;
        patchNothingSelected.Visible = false;

        UpdatePatchDetails(patch);

        // Enable move to patch button if this is a valid move
        moveToPatchButton.Disabled = !editor.IsPatchMoveValid(patch);
    }

    /// <summary>
    ///   Registers tooltip for the already existing Controls
    /// </summary>
    private void RegisterTooltips()
    {
        rigiditySlider.RegisterToolTipForControl("rigiditySlider", "editor");
        helpButton.RegisterToolTipForControl("helpButton");
        symmetryButton.RegisterToolTipForControl("symmetryButton", "editor");
        undoButton.RegisterToolTipForControl("undoButton", "editor");
        redoButton.RegisterToolTipForControl("redoButton", "editor");
        newCellButton.RegisterToolTipForControl("newCellButton", "editor");
        timeIndicator.RegisterToolTipForControl("timeIndicator", "editor");
        finishButton.RegisterToolTipForControl("finishButton", "editor");
        cancelButton.RegisterToolTipForControl("cancelButton", "editor");
        menuButton.RegisterToolTipForControl("menuButton");

        var temperatureButton = physicalConditionsIconLegends.GetNode<TextureButton>("temperature");
        var sunlightButton = physicalConditionsIconLegends.GetNode<TextureButton>("sunlight");

        temperatureButton.RegisterToolTipForControl("temperature", "chartLegendPhysicalConditions");
        sunlightButton.RegisterToolTipForControl("sunlight", "chartLegendPhysicalConditions");
    }

    private void OnSpeciesNameTextChanged(string newText)
    {
        if (newText.Split(" ").Length != 2)
        {
            speciesNameEdit.Set("custom_colors/font_color", new Color(1.0f, 0.3f, 0.3f));
        }
        else
        {
            speciesNameEdit.Set("custom_colors/font_color", new Color(1, 1, 1));
        }

        editor.NewName = newText;
    }

    private void OnSpeciesNameTextEntered(string newText)
    {
        // In case the text is not stored
        editor.NewName = newText;

        // Only defocus if the name is valid to indicate invalid namings to the player
        if (newText.Split(" ").Length == 2)
            speciesNameEdit.ReleaseFocus();
    }

    /// <summary>
    ///   "Searches" an organelle selection button by hiding the ones
    ///   whose name doesn't include the input substring
    /// </summary>
    private void OnSearchBoxTextChanged(string newText)
    {
        var input = newText.ToLower(CultureInfo.InvariantCulture);

        var organelles = SimulationParameters.Instance.GetAllOrganelles().Where(
            organelle => organelle.Name.ToLower(CultureInfo.CurrentCulture).Contains(input)).ToList();

        foreach (var node in placeablePartSelectionElements.Values)
        {
            // To show back organelles that simulation parameters didn't include
            if (string.IsNullOrEmpty(input))
            {
                node.Show();
                continue;
            }

            node.Hide();

            foreach (var organelle in organelles)
            {
                if (node.Name == organelle.InternalName)
                {
                    node.Show();
                }
            }
        }
    }

    private void OnPhysicalConditionsChartLegendPressed(string name)
    {
        var temperatureButton = physicalConditionsIconLegends.GetNode<TextureButton>("temperature");
        var sunlightButton = physicalConditionsIconLegends.GetNode<TextureButton>("sunlight");
        var tween = physicalConditionsIconLegends.GetNode<Tween>("Tween");

        if (name == "temperature")
        {
            temperatureButton.Modulate = Colors.White;
            sunlightButton.Modulate = Colors.DarkGray;
            sunlightChart.Hide();
            temperatureChart.Show();

            tween.InterpolateProperty(temperatureButton, "rect_scale", new Vector2(0.8f, 0.8f), Vector2.One, 0.1f);
            tween.Start();
        }
        else if (name == "sunlight")
        {
            temperatureButton.Modulate = Colors.DarkGray;
            sunlightButton.Modulate = Colors.White;
            sunlightChart.Show();
            temperatureChart.Hide();

            tween.InterpolateProperty(sunlightButton, "rect_scale", new Vector2(0.8f, 0.8f), Vector2.One, 0.1f);
            tween.Start();
        }
    }

    private void OnPhysicalConditionsChartLegendMoused(string name, bool hover)
    {
        var button = physicalConditionsIconLegends.GetNode<TextureButton>(name);
        var tween = physicalConditionsIconLegends.GetNode<Tween>("Tween");

        if (hover)
        {
            tween.InterpolateProperty(button, "rect_scale", Vector2.One, new Vector2(1.1f, 1.1f), 0.1f);
            tween.Start();

            button.Modulate = Colors.LightGray;
        }
        else
        {
            tween.InterpolateProperty(button, "rect_scale", new Vector2(1.1f, 1.1f), Vector2.One, 0.1f);
            tween.Start();

            button.Modulate = button.Pressed ? Colors.White : Colors.DarkGray;
        }
    }

    /// <summary>
    ///   Returns a chart which should contain the given compound.
    /// </summary>
    private LineChart GetChartForCompound(string compoundName)
    {
        switch (compoundName)
        {
            case "atp":
                return null;
            case "oxytoxy":
                return null;
            case "sunlight":
                return sunlightChart;
            case "oxygen":
                return atmosphericGassesChart;
            case "carbondioxide":
                return atmosphericGassesChart;
            case "nitrogen":
                return atmosphericGassesChart;
            default:
                return compoundsChart;
        }
    }

    private float GetCompoundAmount(Patch patch, string compoundName)
    {
        return GetCompoundAmount(patch, patch.Biome, compoundName);
    }

    private float GetCompoundAmount(Patch patch, BiomeConditions biome, string compoundName)
    {
        var compound = SimulationParameters.Instance.GetCompound(compoundName);

        switch (compoundName)
        {
            case "sunlight":
                return biome.Compounds[compound].Dissolved * 100;
            case "oxygen":
                return biome.Compounds[compound].Dissolved * 100;
            case "carbondioxide":
                return biome.Compounds[compound].Dissolved * 100;
            case "nitrogen":
                return biome.Compounds[compound].Dissolved * 100;
            case "iron":
                return patch.GetTotalChunkCompoundAmount(compound);
            default:
                return biome.Compounds[compound].Density *
                    biome.Compounds[compound].Amount + patch.GetTotalChunkCompoundAmount(
                        compound);
        }
    }

    // ReSharper disable once RedundantNameQualifier
    private List<KeyValuePair<string, float>> SortBarData(System.Collections.Generic.Dictionary<string, float> bar)
    {
        var comparer = new ATPComparer();

        var result = bar.OrderBy(
                i => i.Key, comparer)
            .ToList();

        return result;
    }

    private class ATPComparer : IComparer<string>
    {
        /// <summary>
        ///   Compares ATP production / consumption items
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Only works if there aren't duplicate entries of osmoregulation or baseMovement.
        ///   </para>
        /// </remarks>
        public int Compare(string stringA, string stringB)
        {
            if (stringA == "osmoregulation")
            {
                return -1;
            }

            if (stringB == "osmoregulation")
            {
                return 1;
            }

            if (stringA == "baseMovement")
            {
                return -1;
            }

            if (stringB == "baseMovement")
            {
                return 1;
            }

            return string.Compare(stringA, stringB, StringComparison.InvariantCulture);
        }
    }
}
