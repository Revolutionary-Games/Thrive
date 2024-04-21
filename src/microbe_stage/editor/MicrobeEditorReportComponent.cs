using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   The report tab of the microbe editor
/// </summary>
[IgnoreNoMethodsTakingInput]
[SceneLoadedClass("res://src/microbe_stage/editor/MicrobeEditorReportComponent.tscn", UsesEarlyResolve = false)]
public partial class MicrobeEditorReportComponent : EditorComponentBase<IEditorReportData>
{
    [Export]
    public NodePath? AutoEvoSubtabButtonPath;

    [Export]
    public NodePath TimelineSubtabButtonPath = null!;

    [Export]
    public NodePath AutoEvoSubtabPath = null!;

    [Export]
    public NodePath TimelineSubtabPath = null!;

    [Export]
    public NodePath TimelineEventsContainerPath = null!;

    [Export]
    public NodePath TimeIndicatorPath = null!;

    [Export]
    public NodePath GlucoseReductionLabelPath = null!;

    [Export]
    public NodePath AutoEvoLabelPath = null!;

    [Export]
    public NodePath ExternalEffectsLabelPath = null!;

    [Export]
    public NodePath ReportTabPatchNamePath = null!;

    [Export]
    public NodePath ReportTabPatchSelectorPath = null!;

    private readonly NodePath scaleReference = new("scale");

#pragma warning disable CA2213
    private Button autoEvoSubtabButton = null!;
    private Button timelineSubtabButton = null!;

    private PanelContainer autoEvoSubtab = null!;
    private TimelineTab timelineSubtab = null!;

    private Label timeIndicator = null!;
    private Label glucoseReductionLabel = null!;
    private CustomRichTextLabel autoEvoLabel = null!;
    private CustomRichTextLabel externalEffectsLabel = null!;
    private Label reportTabPatchName = null!;
    private OptionButton reportTabPatchSelector = null!;

    [Export]
    private CollapsibleList speciesChartContainer = null!;

    [Export]
    private CollapsibleList physicalConditionsChartContainer = null!;

    [Export]
    private CollapsibleList atmosphereChartContainer = null!;

    [Export]
    private CollapsibleList compoundsChartContainer = null!;

    private HBoxContainer physicalConditionsIconLegends = null!;
    private LineChart temperatureChart = null!;
    private LineChart sunlightChart = null!;
    private LineChart atmosphericGassesChart = null!;
    private LineChart compoundsChart = null!;
    private LineChart speciesPopulationChart = null!;

    private Texture2D temperatureIcon = null!;
#pragma warning restore CA2213

    [JsonProperty]
    private ReportSubtab selectedReportSubtab = ReportSubtab.AutoEvo;

    public enum ReportSubtab
    {
        AutoEvo,
        Timeline,
    }

    public override void _Ready()
    {
        base._Ready();

        autoEvoSubtab = GetNode<PanelContainer>(AutoEvoSubtabPath);
        autoEvoSubtabButton = GetNode<Button>(AutoEvoSubtabButtonPath);

        timelineSubtab = GetNode<TimelineTab>(TimelineSubtabPath);
        timelineSubtabButton = GetNode<Button>(TimelineSubtabButtonPath);

        reportTabPatchName = GetNode<Label>(ReportTabPatchNamePath);
        reportTabPatchSelector = GetNode<OptionButton>(ReportTabPatchSelectorPath);
        timeIndicator = GetNode<Label>(TimeIndicatorPath);
        glucoseReductionLabel = GetNode<Label>(GlucoseReductionLabelPath);
        autoEvoLabel = GetNode<CustomRichTextLabel>(AutoEvoLabelPath);
        externalEffectsLabel = GetNode<CustomRichTextLabel>(ExternalEffectsLabelPath);

        physicalConditionsIconLegends = physicalConditionsChartContainer.GetItem<Container>("LegendContainer")
            .GetChild<HBoxContainer>(0);
        temperatureChart = physicalConditionsChartContainer.GetItem<LineChart>("Temperature");
        sunlightChart = physicalConditionsChartContainer.GetItem<LineChart>("Sunlight");
        atmosphericGassesChart = atmosphereChartContainer.GetItem<LineChart>("AtmosphereChart");
        compoundsChart = compoundsChartContainer.GetItem<LineChart>("CompoundsChart");
        speciesPopulationChart = speciesChartContainer.GetItem<LineChart>("SpeciesChart");

        reportTabPatchSelector.GetPopup().HideOnCheckableItemSelection = false;

        temperatureIcon = GD.Load<Texture2D>("res://assets/textures/gui/bevel/Temperature.png");

        ApplyReportSubtab();
        RegisterTooltips();
    }

    public override void OnFinishEditing()
    {
        // Report has no effect so there's nothing to do here
    }

    public void UpdateReportTabPatchSelector()
    {
        UpdateReportTabPatchName(Editor.CurrentPatch);

        reportTabPatchSelector.Clear();

        foreach (var patch in Editor.CurrentPatch.GetClosestConnectedPatches())
        {
            if (patch.Visibility != MapElementVisibility.Shown)
                continue;

            reportTabPatchSelector.AddItem(patch.Name.ToString(), patch.ID);
        }

        reportTabPatchSelector.Select(reportTabPatchSelector.GetItemIndex(Editor.CurrentPatch.ID));
    }

    public void UpdatePatchDetails(Patch currentOrSelectedPatch, Patch? selectedPatch = null)
    {
        selectedPatch ??= currentOrSelectedPatch;

        UpdateReportTabStatistics(currentOrSelectedPatch);

        UpdateTimeline(selectedPatch);

        UpdateReportTabPatchName(currentOrSelectedPatch);

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

    public void UpdateAutoEvoResults(string results, string external)
    {
        autoEvoLabel.ExtendedBbcode = results;
        externalEffectsLabel.ExtendedBbcode = external;
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

    public override void OnValidAction()
    {
    }

    protected override void OnTranslationsChanged()
    {
        Editor.SendAutoEvoResultsToReportComponent();
        UpdateTimeIndicator(Editor.CurrentGame.GameWorld.TotalPassedTime);
        UpdateGlucoseReduction(Editor.CurrentGame.GameWorld.WorldSettings.GlucoseDecay);
        UpdateTimeline(Editor.SelectedPatch);
        UpdateReportTabPatchSelector();
        UpdateReportTabStatistics(Editor.CurrentPatch);
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
            if (AutoEvoSubtabButtonPath != null)
            {
                AutoEvoSubtabButtonPath.Dispose();
                TimelineSubtabButtonPath.Dispose();
                AutoEvoSubtabPath.Dispose();
                TimelineSubtabPath.Dispose();
                TimelineEventsContainerPath.Dispose();
                TimeIndicatorPath.Dispose();
                GlucoseReductionLabelPath.Dispose();
                AutoEvoLabelPath.Dispose();
                ExternalEffectsLabelPath.Dispose();
                ReportTabPatchNamePath.Dispose();
                ReportTabPatchSelectorPath.Dispose();
            }

            scaleReference.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateReportTabPatchSelectorSelection(int patchID)
    {
        reportTabPatchSelector.Select(reportTabPatchSelector.GetItemIndex(patchID));
    }

    private void UpdateReportTabPatchName(Patch patch)
    {
        reportTabPatchName.Text = patch.Name.ToString();
    }

    private void UpdateReportTabStatistics(Patch patch)
    {
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

        foreach (var snapshot in patch.History)
        {
            foreach (var entry in snapshot.Biome.CombinedCompounds)
            {
                var dataset = new LineChartData
                {
                    Icon = entry.Key.LoadedIcon,
                    Colour = entry.Key.Colour,
                };

                GetChartForCompound(entry.Key.InternalName)?.AddDataSet(entry.Key.Name, dataset);
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
            var temperature = SimulationParameters.Instance.GetCompound("temperature");
            var combinedCompounds = snapshot.Biome.CombinedCompounds;

            temperatureData.AddPoint(DataPoint.GetDataPoint(snapshot.TimePeriod,
                combinedCompounds[temperature].Ambient, markerColour: temperatureData.Colour));

            foreach (var entry in combinedCompounds)
            {
                var dataset = GetChartForCompound(entry.Key.InternalName)?.GetDataSet(entry.Key.Name);

                if (dataset == null)
                    continue;

                var dataPoint = DataPoint.GetDataPoint(snapshot.TimePeriod,
                    Math.Round(patch.GetCompoundAmountInSnapshot(snapshot, entry.Key.InternalName), 3));
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
            var datasets = extinctSpecies.Distinct().ToList();
            speciesPopDatasetsLegend = new SpeciesPopulationDatasetsLegend(datasets, speciesPopulationChart);
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

    private void UpdateTimeline(Patch? mapSelectedPatch, Patch? patch = null)
    {
        timelineSubtab.UpdateTimeline(Editor, mapSelectedPatch, patch);
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

        switch (selectedReportSubtab)
        {
            case ReportSubtab.AutoEvo:
                autoEvoSubtab.Show();
                autoEvoSubtabButton.ButtonPressed = true;
                break;
            case ReportSubtab.Timeline:
                timelineSubtab.Show();
                timelineSubtabButton.ButtonPressed = true;
                Invoke.Instance.Queue(timelineSubtab.TimelineAutoScrollToCurrentTimePeriod);
                break;
            default:
                throw new Exception("Invalid report subtab");
        }
    }

    private void OnReportTabPatchListSelected(int index)
    {
        var patch = Editor.CurrentGame.GameWorld.Map.GetPatch(reportTabPatchSelector.GetItemId(index));
        UpdateReportTabStatistics(patch);
        UpdateTimeline(patch);
        UpdateReportTabPatchName(patch);
    }

    /// <summary>
    ///   Returns a chart which should contain the given compound.
    /// </summary>
    /// <returns>Null if the given compound shouldn't be included in any chart.</returns>
    private LineChart? GetChartForCompound(string compoundName)
    {
        switch (compoundName)
        {
            case "atp":
            case "oxytoxy":
            case "temperature":
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
