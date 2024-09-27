using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoEvo;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   The report tab of the microbe editor
/// </summary>
[IgnoreNoMethodsTakingInput]
[SceneLoadedClass("res://src/microbe_stage/editor/MicrobeEditorReportComponent.tscn", UsesEarlyResolve = false)]
public partial class MicrobeEditorReportComponent : EditorComponentBase<IEditorReportData>
{
    private readonly NodePath scaleReference = new("scale");

#pragma warning disable CA2213
    [Export]
    private Button autoEvoSubtabButton = null!;

    [Export]
    private Button timelineSubtabButton = null!;

    [Export]
    private Button foodChainSubtabButton = null!;

    [Export]
    private PanelContainer autoEvoSubtab = null!;

    [Export]
    private TimelineTab timelineSubtab = null!;

    [Export]
    private Container foodChainSubtab = null!;

    [Export]
    private FoodChainDisplay foodChainData = null!;

    [Export]
    private Label timeIndicator = null!;

    [Export]
    private Label glucoseReductionLabel = null!;

    [Export]
    private CustomRichTextLabel externalEffectsLabel = null!;

    [Export]
    private Label reportTabPatchName = null!;

    [Export]
    private OptionButton reportTabPatchSelector = null!;

    [Export]
    private CollapsibleList speciesChartContainer = null!;

    [Export]
    private CollapsibleList physicalConditionsChartContainer = null!;

    [Export]
    private CollapsibleList atmosphereChartContainer = null!;

    [Export]
    private CollapsibleList compoundsChartContainer = null!;

    [Export]
    private Control noAutoEvoResultData = null!;

    [Export]
    private Container graphicalResultsContainer = null!;

    [Export]
    private LabelSettings autoEvoReportSegmentTitleFont = null!;

    private HBoxContainer physicalConditionsIconLegends = null!;
    private LineChart temperatureChart = null!;
    private LineChart sunlightChart = null!;
    private LineChart atmosphericGassesChart = null!;
    private LineChart compoundsChart = null!;
    private LineChart speciesPopulationChart = null!;

    private Texture2D temperatureIcon = null!;

    private PackedScene speciesResultButtonScene = null!;
#pragma warning restore CA2213

    [JsonProperty]
    private ReportSubtab selectedReportSubtab = ReportSubtab.AutoEvo;

    private bool queuedAutoEvoReportUpdate;

    private Patch? currentlyDisplayedPatch;

    private RunResults? autoEvoResults;

    public enum ReportSubtab
    {
        AutoEvo,
        Timeline,
        FoodChain,
    }

    private Patch PatchToShowInfoFor => currentlyDisplayedPatch ?? Editor.CurrentPatch;

    public override void _Ready()
    {
        base._Ready();

        physicalConditionsIconLegends = physicalConditionsChartContainer.GetItem<Container>("LegendContainer")
            .GetChild<HBoxContainer>(0);
        temperatureChart = physicalConditionsChartContainer.GetItem<LineChart>("Temperature");
        sunlightChart = physicalConditionsChartContainer.GetItem<LineChart>("Sunlight");
        atmosphericGassesChart = atmosphereChartContainer.GetItem<LineChart>("AtmosphereChart");
        compoundsChart = compoundsChartContainer.GetItem<LineChart>("CompoundsChart");
        speciesPopulationChart = speciesChartContainer.GetItem<LineChart>("SpeciesChart");

        reportTabPatchSelector.GetPopup().HideOnCheckableItemSelection = false;

        temperatureIcon = GD.Load<Texture2D>("res://assets/textures/gui/bevel/Temperature.png");

        speciesResultButtonScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/SpeciesResultButton.tscn");

        ApplyReportSubtab();
        RegisterTooltips();
    }

    public override void OnFinishEditing()
    {
        // Report has no effect so there's nothing to do here
    }

    public void UpdateReportTabPatchSelector()
    {
        var patchSelected = PatchToShowInfoFor;

        UpdateReportTabPatchName();

        reportTabPatchSelector.Clear();

        foreach (var patch in patchSelected.GetClosestConnectedPatches())
        {
            if (patch.Visibility != MapElementVisibility.Shown)
                continue;

            reportTabPatchSelector.AddItem(patch.Name.ToString(), patch.ID);
        }

        reportTabPatchSelector.Select(reportTabPatchSelector.GetItemIndex(patchSelected.ID));
    }

    public void UpdatePatchDetailsIfNeeded(Patch selectedPatch)
    {
        if (currentlyDisplayedPatch != null && currentlyDisplayedPatch == selectedPatch)
            return;

        UpdatePatchDetails(selectedPatch);

        if (PatchToShowInfoFor != selectedPatch)
            throw new InvalidOperationException("Expected patch set to apply to property value");

        // Update the report. This is not in UpdatePatchDetails to avoid duplicate update of that expensive
        // component when initializing the editor (but only after displaying them once)
        if (autoEvoResults != null)
        {
            if (selectedReportSubtab == ReportSubtab.AutoEvo)
            {
                CreateGraphicalReportForPatch();
            }
            else
            {
                queuedAutoEvoReportUpdate = true;
            }
        }

        // Refresh this expensive graphical report only if it is visible
        if (selectedReportSubtab == ReportSubtab.FoodChain && autoEvoResults != null)
        {
            foodChainData.DisplayFoodChainIfRequired(autoEvoResults, PatchToShowInfoFor);
        }
    }

    public void UpdatePatchDetails(Patch currentOrSelectedPatch, Patch? selectedPatch = null)
    {
        currentlyDisplayedPatch = currentOrSelectedPatch;

        UpdateReportTabStatistics();

        UpdateTimeline();

        UpdateReportTabPatchName();

        UpdateReportTabPatchSelectorSelection(currentOrSelectedPatch.ID);
    }

    public void UpdateTimeIndicator(double value)
    {
        timeIndicator.Text = string.Format(CultureInfo.CurrentCulture, "{0:#,##0,,}", value) + " "
            + Localization.Translate("MEGA_YEARS");

        var tooltip = ToolTipManager.Instance.GetToolTip("timeIndicator", "editor");

        if (tooltip == null)
            throw new InvalidOperationException("Could not find time indicator tooltip");

        tooltip.Description = Localization.Translate("TIME_INDICATOR_TOOLTIP")
            .FormatSafe(Editor.CurrentGame.GameWorld.TotalPassedTime);
    }

    public void UpdateGlucoseReduction(float value)
    {
        var percentage = Localization.Translate("PERCENTAGE_VALUE").FormatSafe(Math.Round(value * 100));

        // The amount of glucose has been reduced to {0} of the previous amount.
        glucoseReductionLabel.Text = Localization.Translate("THE_AMOUNT_OF_GLUCOSE_HAS_BEEN_REDUCED")
            .FormatSafe(percentage);
    }

    public void UpdateAutoEvoResults(RunResults results, string external)
    {
        noAutoEvoResultData.Visible = false;
        graphicalResultsContainer.Visible = true;

        // TODO: should this also have some graphical representation?
        externalEffectsLabel.ExtendedBbcode = external;

        autoEvoResults = results;

        CreateGraphicalReportForPatch();
    }

    public void DisplayAutoEvoFailure(string extra)
    {
        graphicalResultsContainer.QueueFreeChildren();
        graphicalResultsContainer.AddChild(new Label
        {
            Text = Localization.Translate("AUTO_EVO_FAILED"),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(200, 15),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        externalEffectsLabel.ExtendedBbcode = Localization.Translate("AUTO_EVO_RUN_STATUS") + " " + extra;
    }

    public void ShowErrorAboutOldSave()
    {
        GD.PrintErr("There is no existing full auto-evo results data to show new auto-evo report with");
        noAutoEvoResultData.Visible = true;
        graphicalResultsContainer.Visible = false;

        externalEffectsLabel.Visible = false;
    }

    public override void OnInsufficientMP(bool playSound = true)
    {
        // This component doesn't use actions
    }

    public override void OnActionBlockedWhileAnotherIsInProgress()
    {
    }

    public override void OnMutationPointsChanged(int mutationPoints)
    {
    }

    public override void UpdateUndoRedoButtons(bool canUndo, bool canRedo)
    {
    }

    public override void OnValidAction(IEnumerable<CombinableActionData> actions)
    {
    }

    protected override void OnTranslationsChanged()
    {
        Editor.SendAutoEvoResultsToReportComponent();
        UpdateTimeIndicator(Editor.CurrentGame.GameWorld.TotalPassedTime);
        UpdateGlucoseReduction(Editor.CurrentGame.GameWorld.WorldSettings.GlucoseDecay);
        UpdateTimeline();
        UpdateReportTabPatchSelector();
        UpdateReportTabStatistics();
    }

    protected override void RegisterTooltips()
    {
        base.RegisterTooltips();

        timeIndicator.RegisterToolTipForControl("timeIndicator", "editor");

        var temperatureButton = physicalConditionsIconLegends.GetNode<TextureButton>("temperature");
        var sunlightButton = physicalConditionsIconLegends.GetNode<TextureButton>("sunlight");

        temperatureButton.RegisterToolTipForControl("temperature", "chartLegendPhysicalConditions");
        sunlightButton.RegisterToolTipForControl("sunlight", "chartLegendPhysicalConditions");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            scaleReference.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateReportTabPatchSelectorSelection(int patchID)
    {
        reportTabPatchSelector.Select(reportTabPatchSelector.GetItemIndex(patchID));
    }

    private void UpdateReportTabPatchName()
    {
        reportTabPatchName.Text = PatchToShowInfoFor.Name.ToString();
    }

    /// <summary>
    ///   Generates a new graphics-based representation of the auto-evo report
    /// </summary>
    private void CreateGraphicalReportForPatch()
    {
        if (autoEvoResults == null)
        {
            GD.PrintErr("No auto-evo results data passed to the report component");
            return;
        }

        graphicalResultsContainer.FreeChildren();
        autoEvoResults.MakeGraphicalSummary(graphicalResultsContainer, PatchToShowInfoFor, true,
            speciesResultButtonScene,
            autoEvoReportSegmentTitleFont, new Callable(this, nameof(ShowExtraInfoOnSpecies)));
    }

    private void ShowExtraInfoOnSpecies(uint id)
    {
        if (!Editor.CurrentGame.GameWorld.TryGetSpecies(id, out var species))
        {
            GD.PrintErr("Species not found for displaying extra info");
            return;
        }

        Editor.OpenSpeciesInfoFor(species);
    }

    private void UpdateReportTabStatistics()
    {
        var patch = PatchToShowInfoFor;

        temperatureChart.ClearDataSets();
        sunlightChart.ClearDataSets();
        atmosphericGassesChart.ClearDataSets();
        compoundsChart.ClearDataSets();
        speciesPopulationChart.ClearDataSets();

        // Initialize datasets
        var temperatureData = new LineChartData
        {
            Icon = temperatureIcon,
            Colour = new Color(0.67f, 1, 0.24f),
        };

        temperatureChart.AddDataSet(Localization.Translate("TEMPERATURE"), temperatureData);

        var simulationParameters = SimulationParameters.Instance;

        foreach (var snapshot in patch.History)
        {
            foreach (var entry in snapshot.Biome.CombinedCompounds)
            {
                var compound = simulationParameters.GetCompoundDefinition(entry.Key);

                var dataset = new LineChartData
                {
                    Icon = compound.LoadedIcon,
                    Colour = compound.Colour,
                };

                GetChartForCompound(compound.ID)?.AddDataSet(compound.Name, dataset);
            }

            foreach (var entry in snapshot.SpeciesInPatch)
            {
                var dataset = new LineChartData { Colour = entry.Key.GUIColour };
                speciesPopulationChart.AddDataSet(entry.Key.FormattedName, dataset);
            }
        }

        var extinctSpecies = new List<KeyValuePair<string, ChartDataSet>>();
        var extinctPoints =
            new List<(string Name, DataPoint ExtinctPoint, double TimePeriod, bool ExtinctEverywhere)>();

        // Populate charts with data from patch history. We use reverse loop here because the original collection is
        // reversed (iterating from 500 myr to 100 myr) so it messes up any ordering dependent code
        for (int i = patch.History.Count - 1; i >= 0; i--)
        {
            var snapshot = patch.History.ElementAt(i);
            var combinedCompounds = snapshot.Biome.CombinedCompounds;

            temperatureData.AddPoint(DataPoint.GetDataPoint(snapshot.TimePeriod,
                combinedCompounds[Compound.Temperature].Ambient, markerColour: temperatureData.Colour));

            foreach (var entry in combinedCompounds)
            {
                var compound = simulationParameters.GetCompoundDefinition(entry.Key);

                var dataset = GetChartForCompound(compound.ID)?.GetDataSet(compound.Name);

                if (dataset == null)
                    continue;

                var dataPoint = DataPoint.GetDataPoint(snapshot.TimePeriod,
                    Math.Round(patch.GetCompoundAmountInSnapshotForDisplay(snapshot, compound.ID), 3));
                dataPoint.MarkerColour = dataset.Colour;

                dataset.AddPoint(dataPoint);
            }

            foreach (var entry in snapshot.SpeciesInPatch)
            {
                var dataset = speciesPopulationChart.GetDataSet(entry.Key.FormattedName);

                if (dataset == null)
                {
                    GD.PrintErr("Could not find species population dataset for: ", entry.Key.FormattedName);
                    continue;
                }

                var extinctInPatch = entry.Value <= 0;
                var extinctEverywhere = false;

                // We test if the species info was recorded before using it.
                // This is especially for compatibility with older versions, to avoid crashed due to an invalid key.
                // TODO: Use a proper save upgrade (e.g. summing population to generate info).
                if (snapshot.RecordedSpeciesInfo.TryGetValue(entry.Key, out var speciesInfo))
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

                var dataPoint = DataPoint.GetDataPoint(snapshot.TimePeriod, population,
                    iconType, iconSize, dataset.Colour);

                if (extinctInPatch)
                {
                    extinctSpecies.Add(new KeyValuePair<string, ChartDataSet>(entry.Key.FormattedName, dataset));
                    extinctPoints.Add((entry.Key.FormattedName, dataPoint, snapshot.TimePeriod, extinctEverywhere));
                }

                if (!extinctInPatch && extinctSpecies.Any(e =>
                        e.Key == entry.Key.FormattedName && e.Value == dataset))
                {
                    // No longer extinct in later time period so remove it from the list
                    extinctSpecies.RemoveAll(e => e.Key == entry.Key.FormattedName && e.Value == dataset);
                }

                dataset.AddPoint(dataPoint);
            }
        }

        var percentageFormat = Localization.Translate("PERCENTAGE_VALUE");

        sunlightChart.TooltipYAxisFormat = percentageFormat + " lx";
        atmosphericGassesChart.TooltipYAxisFormat = percentageFormat;
        compoundsChart.TooltipYAxisFormat = percentageFormat;

        speciesPopulationChart.LegendMode = LineChart.LegendDisplayMode.DropDown;

        SpeciesPopulationDatasetsLegend? speciesPopDatasetsLegend = null;

        // The following operation might be expensive so we only do this if any extinction occured
        if (extinctSpecies.Any())
        {
            var seenEntries = new List<KeyValuePair<string, ChartDataSet>>();

            // Need to manually make this list distinct as otherwise an inefficient default comparer is used
            foreach (var pair in extinctSpecies)
            {
                bool exist = false;

                // A linear search should be fine as there shouldn't be that many items
                foreach (var seenEntry in seenEntries)
                {
                    if (seenEntry.Key == pair.Key)
                    {
                        exist = true;
                        break;
                    }
                }

                if (!exist)
                    seenEntries.Add(pair);
            }

            speciesPopDatasetsLegend = new SpeciesPopulationDatasetsLegend(seenEntries, speciesPopulationChart);
            speciesPopulationChart.LegendMode = LineChart.LegendDisplayMode.CustomOrNone;
        }

        sunlightChart.Plot(Localization.Translate("YEARS"), "% lx", 5, null, null, null, 5);
        temperatureChart.Plot(Localization.Translate("YEARS"), "°C", 5, null, null, null, 5);
        atmosphericGassesChart.Plot(Localization.Translate("YEARS"), "%", 5,
            Localization.Translate("ATMOSPHERIC_GASSES"), null, null, 5);
        speciesPopulationChart.Plot(Localization.Translate("YEARS"), string.Empty, 5,
            Localization.Translate("SPECIES_LIST"), speciesPopDatasetsLegend,
            Editor.CurrentGame.GameWorld.PlayerSpecies.FormattedName, 5);
        compoundsChart.Plot(Localization.Translate("YEARS"), "%", 5, Localization.Translate("COMPOUNDS"),
            null, null, 5);

        OnPhysicalConditionsChartLegendPressed("temperature");

        foreach (var point in extinctPoints)
        {
            var extinctionType = point.ExtinctEverywhere ?
                Localization.Translate("EXTINCT_FROM_THE_PLANET") :
                Localization.Translate("EXTINCT_FROM_PATCH");

            // Override datapoint tooltip to show extinction type instead of just zero.
            // Doesn't need to account for ToolTipAxesFormat as we don't have it for species pop graph
            speciesPopulationChart.OverrideDataPointToolTipDescription(point.Name, point.ExtinctPoint,
                $"{point.Name}\n{point.TimePeriod.FormatNumber()}\n{extinctionType}");
        }

        var cross = GD.Load<Texture2D>("res://assets/textures/gui/bevel/graphMarkerCross.png");
        var skull = GD.Load<Texture2D>("res://assets/textures/gui/bevel/SuicideIcon.png");

        speciesPopulationChart.AddIconLegend(cross, Localization.Translate("EXTINCT_FROM_PATCH"));
        speciesPopulationChart.AddIconLegend(skull, Localization.Translate("EXTINCT_FROM_THE_PLANET"), 25);
    }

    private void UpdateTimeline()
    {
        timelineSubtab.UpdateTimeline(Editor, PatchToShowInfoFor);
    }

    private void SetReportSubtab(string tab)
    {
        var selection = (ReportSubtab)Enum.Parse(typeof(ReportSubtab), tab);

        if (selection == selectedReportSubtab)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        selectedReportSubtab = selection;
        ApplyReportSubtab();
    }

    private void ApplyReportSubtab()
    {
        autoEvoSubtab.Hide();
        timelineSubtab.Hide();
        foodChainSubtab.Hide();

        switch (selectedReportSubtab)
        {
            case ReportSubtab.AutoEvo:
            {
                // Refresh the report if it should now show different data than when it was last visible
                if (queuedAutoEvoReportUpdate)
                {
                    CreateGraphicalReportForPatch();
                    queuedAutoEvoReportUpdate = false;
                }

                autoEvoSubtab.Show();
                autoEvoSubtabButton.ButtonPressed = true;
                break;
            }

            case ReportSubtab.Timeline:
            {
                timelineSubtab.Show();
                timelineSubtabButton.ButtonPressed = true;
                Invoke.Instance.Queue(timelineSubtab.TimelineAutoScrollToCurrentTimePeriod);
                break;
            }

            case ReportSubtab.FoodChain:
            {
                foodChainSubtab.Show();
                foodChainSubtabButton.ButtonPressed = true;

                if (autoEvoResults != null)
                {
                    foodChainData.DisplayFoodChainIfRequired(autoEvoResults, PatchToShowInfoFor);
                }

                break;
            }

            default:
                throw new Exception("Invalid report subtab");
        }
    }

    private void OnReportTabPatchListSelected(int index)
    {
        currentlyDisplayedPatch = Editor.CurrentGame.GameWorld.Map.GetPatch(reportTabPatchSelector.GetItemId(index));

        UpdateReportTabStatistics();
        UpdateTimeline();
        UpdateReportTabPatchName();

        if (autoEvoResults != null)
        {
            if (selectedReportSubtab == ReportSubtab.AutoEvo)
            {
                CreateGraphicalReportForPatch();
            }
            else
            {
                queuedAutoEvoReportUpdate = true;
            }
        }

        if (selectedReportSubtab == ReportSubtab.FoodChain && autoEvoResults != null)
        {
            foodChainData.DisplayFoodChainIfRequired(autoEvoResults, PatchToShowInfoFor);
        }
    }

    /// <summary>
    ///   Returns a chart which should contain the given compound.
    /// </summary>
    /// <returns>Null if the given compound shouldn't be included in any chart.</returns>
    private LineChart? GetChartForCompound(Compound compound)
    {
        switch (compound)
        {
            case Compound.ATP:
            case Compound.Oxytoxy:
            case Compound.Temperature:
                return null;
            case Compound.Sunlight:
                return sunlightChart;
            case Compound.Oxygen:
                return atmosphericGassesChart;
            case Compound.Carbondioxide:
                return atmosphericGassesChart;
            case Compound.Nitrogen:
                return atmosphericGassesChart;
            default:
                return compoundsChart;
        }
    }

    private void OnPhysicalConditionsChartLegendPressed(string name)
    {
        var temperatureButton = physicalConditionsIconLegends.GetNode<TextureButton>("temperature");
        var sunlightButton = physicalConditionsIconLegends.GetNode<TextureButton>("sunlight");

        if (name == "temperature")
        {
            temperatureButton.Modulate = Colors.White;
            sunlightButton.Modulate = Colors.DarkGray;
            sunlightChart.Hide();
            temperatureChart.Show();
        }
        else if (name == "sunlight")
        {
            temperatureButton.Modulate = Colors.DarkGray;
            sunlightButton.Modulate = Colors.White;
            sunlightChart.Show();
            temperatureChart.Hide();
        }
    }

    private void OnPhysicalConditionsChartLegendMoused(string name, bool hover)
    {
        var button = physicalConditionsIconLegends.GetNode<TextureButton>(name);
        var tween = CreateTween();

        if (hover)
        {
            tween.TweenProperty(button, scaleReference, new Vector2(1.1f, 1.1f), 0.1);

            button.Modulate = Colors.LightGray;
        }
        else
        {
            tween.TweenProperty(button, scaleReference, Vector2.One, 0.1);

            button.Modulate = button.ButtonPressed ? Colors.White : Colors.DarkGray;
        }
    }
}
