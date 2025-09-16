﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Handles showing tolerance adaptation controls (sliders) and applying their changes
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
[DeserializedCallbackTarget]
[IgnoreNoMethodsTakingInput]
[SceneLoadedClass("res://src/microbe_stage/editor/TolerancesEditorSubComponent.tscn", UsesEarlyResolve = false)]
public partial class TolerancesEditorSubComponent : EditorComponentBase<ICellEditorData>
{
    [Export]
    [ExportCategory("Config")]
    public bool ShowZeroModifiers;

    private readonly Dictionary<OrganelleDefinition, float> tempToleranceModifiers = new();

    private CompoundDefinition temperature = null!;

#pragma warning disable CA2213

    [Export]
    private Container temperatureToolTipContainer = null!;

    [Export]
    private Container pressureToolTipContainer = null!;

    [Export]
    private Container oxygenResistanceToolTipContainer = null!;

    [Export]
    private Container uvResistanceToolTipContainer = null!;

    [Export]
    [ExportCategory("Inputs")]
    private Slider temperatureSlider = null!;

    [Export]
    private Slider temperatureToleranceRangeSlider = null!;

    [Export]
    private Slider pressureSlider = null!;

    [Export]
    private Slider pressureToleranceRangeSlider = null!;

    [Export]
    private Slider oxygenResistanceSlider = null!;

    [Export]
    private Slider uvResistanceSlider = null!;

    [Export]
    [ExportCategory("Displays")]
    private Label temperatureMinLabel = null!;

    [Export]
    private Label temperatureMaxLabel = null!;

    [Export]
    private Label temperatureToleranceLabel = null!;

    [Export]
    private Label temperatureModifierLabel = null!;

    [Export]
    private ToleranceRangeDisplay temperatureRangeDisplay = null!;

    [Export]
    private Label pressureMinLabel = null!;

    [Export]
    private Label pressureMaxLabel = null!;

    [Export]
    private Label pressureToleranceLabel = null!;

    [Export]
    private Label pressureModifierLabel = null!;

    [Export]
    private ToleranceRangeDisplay pressureRangeDisplay = null!;

    [Export]
    private Label oxygenResistanceModifierLabel = null!;

    [Export]
    private Label oxygenResistanceTotalLabel = null!;

    [Export]
    private ToleranceRangeDisplay oxygenResistanceRangeDisplay = null!;

    [Export]
    private Label uvResistanceModifierLabel = null!;

    [Export]
    private Label uvResistanceTotalLabel = null!;

    [Export]
    private ToleranceRangeDisplay uvResistanceRangeDisplay = null!;

    [Export]
    [ExportCategory("Style")]
    private LabelSettings badValueFont = null!;

    [Export]
    private LabelSettings perfectValueFont = null!;

    [Export]
    private LabelSettings badValueFontTiny = null!;

    [Export]
    private LabelSettings perfectValueFontTiny = null!;

    [Export]
    private LabelSettings modifierBadFont = null!;

    [Export]
    private LabelSettings modifierGoodFont = null!;

    [Export]
    private Color optimalDisplayBadColor;

    [Export]
    private Color optimalDisplayGoodColor;

    private LabelSettings? originalTemperatureFont;
    private LabelSettings? originalPressureFont;

    private LabelSettings? originalModifierFont;

    private EnvironmentalToleranceToolTip? temperatureToolTip;
    private StatModifierToolTip? temperatureRangeToolTip;
    private EnvironmentalToleranceToolTip? pressureToolTip;
    private StatModifierToolTip? pressureRangeToolTip;
    private EnvironmentalToleranceToolTip? oxygenResistanceToolTip;
    private StatModifierToolTip? oxygenResistanceModifierToolTip;
    private EnvironmentalToleranceToolTip? uvResistanceToolTip;
    private StatModifierToolTip? uvResistanceModifierToolTip;
#pragma warning restore CA2213

    private bool automaticallyChanging;

    private bool wasFreshInit;

    /// <summary>
    ///   Reusable tolerances object for checking things until it is consumed by using it up in an action
    /// </summary>
    private EnvironmentalTolerances? reusableTolerances;

    private MicrobeEnvironmentalToleranceCalculations.ToleranceValues organelleModifiers;

    [Signal]
    public delegate void OnTolerancesChangedEventHandler();

    [JsonProperty]
    public EnvironmentalTolerances CurrentTolerances { get; private set; } = new();

    [JsonIgnore]
    public override bool IsSubComponent => true;

    public float MPDisplayCostMultiplier { get; set; } = 1;

    public override void _Ready()
    {
        originalTemperatureFont = temperatureMinLabel.LabelSettings;
        originalPressureFont = pressureMinLabel.LabelSettings;
        originalModifierFont = temperatureModifierLabel.LabelSettings;

        temperature = SimulationParameters.GetCompound(Compound.Temperature);

        RegisterTooltips();
    }

    public override void Init(ICellEditorData owningEditor, bool fresh)
    {
        base.Init(owningEditor, fresh);

        wasFreshInit = fresh;
    }

    public override void OnEditorSpeciesSetup(Species species)
    {
        base.OnEditorSpeciesSetup(species);

        if (!wasFreshInit)
        {
            // The latest data should have already been loaded into CurrentTolerances

            ApplyCurrentValuesToGUI();
        }
        else
        {
            // Load data for the species being edited
            var speciesTolerance = Editor.EditedBaseSpecies.Tolerances;
            CurrentTolerances.CopyFrom(speciesTolerance);

            ResetToCurrentSpeciesTolerances();
        }

        UpdateToolTipStats();
    }

    public override void OnFinishEditing()
    {
        // Apply the tolerances
        Editor.EditedBaseSpecies.Tolerances.CopyFrom(CurrentTolerances);
    }

    public void OnDataTolerancesDependOnChanged(bool organellesChanged)
    {
        if (organellesChanged)
            CalculateOrganelleModifiers();

        UpdateCurrentValueDisplays();
        UpdateToolTipStats();
    }

    public void CalculateOrganelleModifiers()
    {
        if (Editor.EditedCellOrganelles == null)
            return;

        organelleModifiers = default;

        MicrobeEnvironmentalToleranceCalculations.ApplyOrganelleEffectsOnTolerances(Editor.EditedCellOrganelles,
            ref organelleModifiers);
    }

    public void ResetToCurrentSpeciesTolerances()
    {
        // Read the species data
        ResetToTolerances(Editor.EditedBaseSpecies.Tolerances);
    }

    public void ResetToTolerances(EnvironmentalTolerances tolerances)
    {
        CurrentTolerances.CopyFrom(tolerances);

        // Send it to GUI
        ApplyCurrentValuesToGUI();
    }

    public void UpdateMPCostInToolTips()
    {
        if (temperatureToolTip != null)
        {
            // Multiply costs by minimum step size to make the costs more reasonably display
            var temperatureCost = temperatureSlider.Step * Constants.TOLERANCE_CHANGE_MP_PER_TEMPERATURE *
                MPDisplayCostMultiplier;

            temperatureToolTip.MPCost = (float)temperatureCost;
        }

        if (pressureToolTip != null)
        {
            var pressureCost = pressureSlider.Step * Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE *
                MPDisplayCostMultiplier;

            pressureToolTip.MPCost = (float)pressureCost;
        }

        if (oxygenResistanceToolTip != null)
        {
            var oxygenCost = oxygenResistanceSlider.Step * Constants.TOLERANCE_CHANGE_MP_PER_OXYGEN *
                MPDisplayCostMultiplier;

            oxygenResistanceToolTip.MPCost = (float)oxygenCost;
        }

        if (uvResistanceToolTip != null)
        {
            var uvCost = uvResistanceSlider.Step * Constants.TOLERANCE_CHANGE_MP_PER_UV * MPDisplayCostMultiplier;

            uvResistanceToolTip.MPCost = (float)uvCost;
        }
    }

    public void UpdateToolTipStats()
    {
        if (Editor.EditedCellOrganelles == null)
        {
            GD.PrintErr("No cell edited organelles set, tolerances editor cannot update!");
            return;
        }

        // Calculate one stat at a time to get the individual changes per type instead of all being combined.
        // And for that we need an optimal baseline that guarantees no other stat-related debuffs / buffs are mixed in.
        var optimal = Editor.CurrentPatch.GenerateTolerancesForMicrobe(Editor.EditedCellOrganelles);

        // Set huge ranges so that there is no threat of optimal bonuses triggering with the default calculations
        optimal.PressureTolerance += Constants.TOLERANCE_PERFECT_THRESHOLD_PRESSURE * 2;
        optimal.TemperatureTolerance += Constants.TOLERANCE_PERFECT_THRESHOLD_TEMPERATURE * 2;

        var tempTolerances = CurrentTolerances.Clone();

#if DEBUG
        tempTolerances.CopyFrom(optimal);
        var optimalTest = MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(tempTolerances,
            Editor.EditedCellOrganelles ?? throw new Exception("Organelles not set"), Editor.CurrentPatch.Biome);

        if (optimalTest.OverallScore is < 1 or > 1 + MathUtils.EPSILON)
        {
            GD.PrintErr("Optimal tolerance calculation failed, score: " + optimalTest.OverallScore);

            if (Debugger.IsAttached)
                Debugger.Break();
        }
#endif

        if (temperatureToolTip != null)
        {
            // Optimal values for other than temperature
            tempTolerances.CopyFrom(optimal);
            tempTolerances.PreferredTemperature = CurrentTolerances.PreferredTemperature;
            tempTolerances.TemperatureTolerance = CurrentTolerances.TemperatureTolerance;

            CalculateStatsAndShow(tempTolerances, temperatureToolTip);
        }

        if (pressureToolTip != null)
        {
            tempTolerances.CopyFrom(optimal);
            tempTolerances.PressureMinimum = CurrentTolerances.PressureMinimum;
            tempTolerances.PressureTolerance = CurrentTolerances.PressureTolerance;

            CalculateStatsAndShow(tempTolerances, pressureToolTip);
        }

        if (oxygenResistanceToolTip != null)
        {
            tempTolerances.CopyFrom(optimal);
            tempTolerances.OxygenResistance = CurrentTolerances.OxygenResistance;

            CalculateStatsAndShow(tempTolerances, oxygenResistanceToolTip);
        }

        if (uvResistanceToolTip != null)
        {
            tempTolerances.CopyFrom(optimal);
            tempTolerances.UVResistance = CurrentTolerances.UVResistance;

            CalculateStatsAndShow(tempTolerances, uvResistanceToolTip);
        }

        UpdateTotalValuesInToolTips();
    }

    protected override void OnTranslationsChanged()
    {
        UpdateCurrentValueDisplays();
        UpdateMPCostInToolTips();
        UpdateToolTipStats();
    }

    protected override void RegisterTooltips()
    {
        base.RegisterTooltips();

        temperatureToolTipContainer.RegisterToolTipForControl("temperature", "tolerances");
        pressureToolTipContainer.RegisterToolTipForControl("pressure", "tolerances");
        oxygenResistanceToolTipContainer.RegisterToolTipForControl("oxygenResistance", "tolerances");
        uvResistanceToolTipContainer.RegisterToolTipForControl("uvResistance", "tolerances");

        temperatureModifierLabel.GetParent<Control>().RegisterToolTipForControl("temperatureRangeModifier", "tolerances");
        pressureModifierLabel.GetParent<Control>().RegisterToolTipForControl("pressureRangeModifier", "tolerances");
        oxygenResistanceModifierLabel.GetParent<Control>().RegisterToolTipForControl("oxygenResistanceModifier", "tolerances");
        uvResistanceModifierLabel.GetParent<Control>().RegisterToolTipForControl("uvResistanceModifier", "tolerances");

        var toolTipManager = ToolTipManager.Instance;
        temperatureToolTip = toolTipManager.GetToolTip<EnvironmentalToleranceToolTip>("temperature", "tolerances");
        temperatureRangeToolTip =
            toolTipManager.GetToolTip<StatModifierToolTip>("temperatureRangeModifier", "tolerances");
        pressureToolTip = toolTipManager.GetToolTip<EnvironmentalToleranceToolTip>("pressure", "tolerances");
        pressureRangeToolTip = toolTipManager.GetToolTip<StatModifierToolTip>("pressureRangeModifier", "tolerances");
        oxygenResistanceToolTip =
            toolTipManager.GetToolTip<EnvironmentalToleranceToolTip>("oxygenResistance", "tolerances");
        oxygenResistanceModifierToolTip =
            toolTipManager.GetToolTip<StatModifierToolTip>("oxygenResistanceModifier", "tolerances");
        uvResistanceToolTip = toolTipManager.GetToolTip<EnvironmentalToleranceToolTip>("uvResistance", "tolerances");
        uvResistanceModifierToolTip =
            toolTipManager.GetToolTip<StatModifierToolTip>("uvResistanceModifier", "tolerances");

        // Copy the units here automatically
        if (temperatureRangeToolTip != null)
        {
            temperatureRangeToolTip.ValueSuffix = temperature.Unit;
        }
        else
        {
            GD.PrintErr("Tooltips not correctly found for tolerances editor");
        }
    }

    private void CalculateStatsAndShow(EnvironmentalTolerances calculationTolerances,
        EnvironmentalToleranceToolTip toolTip)
    {
        if (Editor.EditedCellOrganelles == null)
        {
            GD.PrintErr("No cell edited organelles set, tolerances editor cannot update!");
            return;
        }

        var rawTolerances = MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(calculationTolerances,
            Editor.EditedCellOrganelles, Editor.CurrentPatch.Biome);

        var resolvedTolerances = MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(rawTolerances);

        toolTip.UpdateStats(resolvedTolerances);
    }

    private void UpdateTotalValuesInToolTips()
    {
        if (Editor.EditedCellOrganelles == null)
        {
            GD.PrintErr("no cell edited organelles set, cannot update effective value tooltips");
            return;
        }

        if (temperatureRangeToolTip != null)
        {
            temperatureRangeToolTip.DisplayedValue =
                CurrentTolerances.TemperatureTolerance + organelleModifiers.TemperatureTolerance;

            // Calculate organelle summaries so that the info can be shown in the tooltips
            MicrobeEnvironmentalToleranceCalculations.GenerateToleranceEffectSummariesByOrganelle(
                Editor.EditedCellOrganelles, ToleranceModifier.TemperatureRange, tempToleranceModifiers);

            temperatureRangeToolTip.DisplayOrganelleBreakdown(tempToleranceModifiers);
        }

        if (pressureRangeToolTip != null)
        {
            pressureRangeToolTip.DisplayedValue =
                CurrentTolerances.PressureTolerance + organelleModifiers.PressureTolerance;

            MicrobeEnvironmentalToleranceCalculations.GenerateToleranceEffectSummariesByOrganelle(
                Editor.EditedCellOrganelles, ToleranceModifier.PressureRange, tempToleranceModifiers);
            pressureRangeToolTip.DisplayOrganelleBreakdown(tempToleranceModifiers);
        }

        if (oxygenResistanceModifierToolTip != null)
        {
            oxygenResistanceModifierToolTip.DisplayedValue =
                CurrentTolerances.OxygenResistance + organelleModifiers.OxygenResistance;

            MicrobeEnvironmentalToleranceCalculations.GenerateToleranceEffectSummariesByOrganelle(
                Editor.EditedCellOrganelles, ToleranceModifier.Oxygen, tempToleranceModifiers);
            oxygenResistanceModifierToolTip.DisplayOrganelleBreakdown(tempToleranceModifiers);
        }

        if (uvResistanceModifierToolTip != null)
        {
            uvResistanceModifierToolTip.DisplayedValue =
                CurrentTolerances.UVResistance + organelleModifiers.UVResistance;

            MicrobeEnvironmentalToleranceCalculations.GenerateToleranceEffectSummariesByOrganelle(
                Editor.EditedCellOrganelles, ToleranceModifier.UV, tempToleranceModifiers);
            uvResistanceModifierToolTip.DisplayOrganelleBreakdown(tempToleranceModifiers);
        }
    }

    private void ApplyCurrentValuesToGUI()
    {
        automaticallyChanging = true;

        temperatureSlider.Value = CurrentTolerances.PreferredTemperature;
        temperatureToleranceRangeSlider.Value = CurrentTolerances.TemperatureTolerance;

        pressureSlider.Value = CurrentTolerances.PressureMinimum;
        pressureToleranceRangeSlider.Value = CurrentTolerances.PressureTolerance;

        oxygenResistanceSlider.Value = CurrentTolerances.OxygenResistance;
        uvResistanceSlider.Value = CurrentTolerances.UVResistance;

        automaticallyChanging = false;

        UpdateCurrentValueDisplays();
    }

    private void OnTemperatureSliderChanged(float value)
    {
        // Ignore changes triggered by our code to avoid infinite loops
        if (automaticallyChanging)
            return;

        reusableTolerances ??= new EnvironmentalTolerances();

        // Create a change action for the new value
        reusableTolerances.CopyFrom(CurrentTolerances);
        reusableTolerances.PreferredTemperature = value;

        automaticallyChanging = true;

        if (!TriggerChangeIfPossible())
        {
            // Rollback value if not enough MP
            temperatureSlider.Value = CurrentTolerances.PreferredTemperature;
        }

        automaticallyChanging = false;
    }

    private void OnTemperatureToleranceRangeSliderChanged(float value)
    {
        if (automaticallyChanging)
            return;

        reusableTolerances ??= new EnvironmentalTolerances();
        reusableTolerances.CopyFrom(CurrentTolerances);
        reusableTolerances.TemperatureTolerance = value;

        automaticallyChanging = true;

        if (!TriggerChangeIfPossible())
        {
            temperatureToleranceRangeSlider.Value = CurrentTolerances.TemperatureTolerance;
        }

        automaticallyChanging = false;
    }

    private void OnPressureSliderChanged(float value)
    {
        if (automaticallyChanging)
            return;

        reusableTolerances ??= new EnvironmentalTolerances();
        reusableTolerances.CopyFrom(CurrentTolerances);
        reusableTolerances.PressureMinimum = value;

        automaticallyChanging = true;

        if (!TriggerChangeIfPossible())
        {
            pressureSlider.Value = CurrentTolerances.PressureMinimum;
        }

        automaticallyChanging = false;
    }

    private void OnPressureToleranceRangeSliderChanged(float value)
    {
        if (automaticallyChanging)
            return;

        reusableTolerances ??= new EnvironmentalTolerances();
        reusableTolerances.CopyFrom(CurrentTolerances);
        reusableTolerances.PressureTolerance = value;

        automaticallyChanging = true;

        if (!TriggerChangeIfPossible())
        {
            pressureToleranceRangeSlider.Value = CurrentTolerances.PressureTolerance;
        }

        automaticallyChanging = false;
    }

    private void OnOxygenResistanceSliderChanged(float value)
    {
        if (automaticallyChanging)
            return;

        reusableTolerances ??= new EnvironmentalTolerances();
        reusableTolerances.CopyFrom(CurrentTolerances);
        reusableTolerances.OxygenResistance = value;

        automaticallyChanging = true;

        if (!TriggerChangeIfPossible())
        {
            oxygenResistanceSlider.Value = CurrentTolerances.OxygenResistance;
        }

        automaticallyChanging = false;
    }

    private void OnUVResistanceSliderChanged(float value)
    {
        if (automaticallyChanging)
            return;

        reusableTolerances ??= new EnvironmentalTolerances();
        reusableTolerances.CopyFrom(CurrentTolerances);
        reusableTolerances.UVResistance = value;

        automaticallyChanging = true;

        if (!TriggerChangeIfPossible())
        {
            uvResistanceSlider.Value = CurrentTolerances.UVResistance;
        }

        automaticallyChanging = false;
    }

    private bool TriggerChangeIfPossible()
    {
        if (reusableTolerances == null)
            throw new InvalidOperationException("Tolerances data should be set");

        // TODO: would there be a way to avoid this clone? Might need some reworking of the general actions system
        var action = new ToleranceActionData(CurrentTolerances.Clone(), reusableTolerances);

        // And try to apply it. The actual value change will come from the do action callback.
        if (!Editor.EnqueueAction(new SingleEditorAction<ToleranceActionData>(DoToleranceChangeAction,
                UndoToleranceChangeAction,
                action)))
        {
            return false;
        }

        // The action has now eaten the reusable object
        reusableTolerances = null;
        return true;
    }

    /// <summary>
    ///   Applies all new values to the GUI and notifies the data has changed
    /// </summary>
    private void OnChanged()
    {
        EmitSignal(SignalName.OnTolerancesChanged);

        UpdateCurrentValueDisplays();
        UpdateToolTipStats();
    }

    private void UpdateSliderPositionForDisplay(Slider slider, ToleranceRangeDisplay display)
    {
        var fraction = (float)((slider.Value - slider.MinValue) / (slider.MaxValue - slider.MinValue));
        display.SliderGrabberPosX = slider.Size.X * fraction;
    }

    private void UpdateCurrentValueDisplays()
    {
        var patch = Editor.CurrentPatch;
        var patchTemperature = patch.Biome.GetCompound(Compound.Temperature, CompoundAmountType.Biome).Ambient;
        var patchPressure = patch.Biome.Pressure;
        var requiredOxygenResistance = patch.Biome.CalculateOxygenResistanceFactor();
        var requiredUVResistance = patch.Biome.CalculateUVFactor();

        var unitFormat = Localization.Translate("VALUE_WITH_UNIT");
        var percentageFormat = Localization.Translate("PERCENTAGE_VALUE");

        // Temperature

        var temperatureToleranceWithOrganelles =
            CurrentTolerances.TemperatureTolerance + organelleModifiers.TemperatureTolerance;

        temperatureRangeDisplay.SetBoundPositions(CurrentTolerances.PreferredTemperature, temperatureToleranceWithOrganelles);
        temperatureRangeDisplay.UpdateMarker(patchTemperature);
        UpdateSliderPositionForDisplay(temperatureSlider, temperatureRangeDisplay);

        temperatureMinLabel.Text =
            unitFormat.FormatSafe(
                Math.Round(CurrentTolerances.PreferredTemperature - temperatureToleranceWithOrganelles, 1),
                temperature.Unit);
        temperatureMaxLabel.Text =
            unitFormat.FormatSafe(
                Math.Round(CurrentTolerances.PreferredTemperature + temperatureToleranceWithOrganelles, 1),
                temperature.Unit);

        temperatureToleranceLabel.Text = "±" + unitFormat.FormatSafe(
            Math.Round(CurrentTolerances.TemperatureTolerance, 1), temperature.Unit);

        if (ShowZeroModifiers || organelleModifiers.TemperatureTolerance != 0)
        {
            temperatureModifierLabel.GetParent<Control>().Visible = true;

            temperatureModifierLabel.Text = "±" + unitFormat.FormatSafe(
                Math.Round(organelleModifiers.TemperatureTolerance, 1), temperature.Unit);

            temperatureModifierLabel.LabelSettings = organelleModifiers.TemperatureTolerance switch
            {
                > 0 => modifierGoodFont,
                0 => originalModifierFont,
                < 0 => modifierBadFont,
                _ => originalModifierFont,
            };
        }
        else
        {
            temperatureModifierLabel.GetParent<Control>().Visible = false;
        }

        // Show in red the conditions that are not matching to make them easier to notice
        if (Math.Abs(patchTemperature - CurrentTolerances.PreferredTemperature) >
            temperatureToleranceWithOrganelles)
        {
            // Mark the direction that is bad as the one having the problem to make it easier for the player to see
            // what is wrong
            if (patchTemperature > CurrentTolerances.PreferredTemperature)
            {
                temperatureMaxLabel.LabelSettings = badValueFont;
                temperatureMinLabel.LabelSettings = originalTemperatureFont;
            }
            else
            {
                temperatureMinLabel.LabelSettings = badValueFont;
                temperatureMaxLabel.LabelSettings = originalTemperatureFont;
            }

            temperatureRangeDisplay.SetColorsAndRedraw(optimalDisplayBadColor);
        }
        else if (Math.Abs(CurrentTolerances.TemperatureTolerance) < Constants.TOLERANCE_PERFECT_THRESHOLD_TEMPERATURE)
        {
            // Perfectly adapted
            temperatureMinLabel.LabelSettings = perfectValueFont;
            temperatureMaxLabel.LabelSettings = perfectValueFont;

            temperatureRangeDisplay.SetColorsAndRedraw(optimalDisplayGoodColor);
        }
        else
        {
            temperatureMinLabel.LabelSettings = originalTemperatureFont;
            temperatureMaxLabel.LabelSettings = originalTemperatureFont;
            temperatureRangeDisplay.SetColorsAndRedraw(null);
        }

        // Pressure

        var pressureToleranceWithOrganelles = CurrentTolerances.PressureTolerance + organelleModifiers.PressureTolerance;

        pressureRangeDisplay.SetBoundPositions(CurrentTolerances.PressureMinimum, pressureToleranceWithOrganelles, 0);
        pressureRangeDisplay.UpdateMarker(patchPressure);
        UpdateSliderPositionForDisplay(pressureSlider, pressureRangeDisplay);

        var pressureMin = CurrentTolerances.PressureMinimum;
        var pressureMax = Math.Min(CurrentTolerances.PressureMinimum + pressureToleranceWithOrganelles, Constants.TOLERANCE_PRESSURE_MAX);

        pressureMinLabel.Text = unitFormat.FormatSafe(Math.Round(pressureMin / 1000), "kPa");
        pressureMaxLabel.Text = unitFormat.FormatSafe(Math.Round(pressureMax / 1000), "kPa");

        pressureToleranceLabel.Text = "+" + unitFormat.FormatSafe(Math.Round(CurrentTolerances.PressureTolerance / 1000), "kPa");

        if (ShowZeroModifiers || organelleModifiers.PressureTolerance != 0)
        {
            pressureModifierLabel.GetParent<Control>().Visible = true;

            pressureModifierLabel.Text = "+" + unitFormat.FormatSafe(Math.Round(organelleModifiers.PressureTolerance / 1000), "kPa");

            pressureModifierLabel.LabelSettings = organelleModifiers.PressureTolerance switch
            {
                > 0 => modifierGoodFont,
                0 => originalModifierFont,
                < 0 => modifierBadFont,
                _ => originalModifierFont,
            };
        }
        else
        {
            pressureModifierLabel.GetParent<Control>().Visible = false;
        }

        // Show in red the conditions that are not matching to make them easier to notice
        if (patchPressure < CurrentTolerances.PressureMinimum)
        {
            pressureMinLabel.LabelSettings = badValueFontTiny;
            pressureMaxLabel.LabelSettings = originalPressureFont;
            pressureRangeDisplay.SetColorsAndRedraw(optimalDisplayBadColor);
        }
        else if (patchPressure > CurrentTolerances.PressureMaximum)
        {
            pressureMaxLabel.LabelSettings = badValueFontTiny;
            pressureMinLabel.LabelSettings = originalPressureFont;
            pressureRangeDisplay.SetColorsAndRedraw(optimalDisplayBadColor);
        }
        else if (Math.Abs(CurrentTolerances.PressureTolerance) < Constants.TOLERANCE_PERFECT_THRESHOLD_PRESSURE)
        {
            // Perfectly adapted
            pressureMinLabel.LabelSettings = perfectValueFontTiny;
            pressureMaxLabel.LabelSettings = perfectValueFontTiny;
            pressureRangeDisplay.SetColorsAndRedraw(optimalDisplayGoodColor);
        }
        else
        {
            pressureMinLabel.LabelSettings = originalPressureFont;
            pressureMaxLabel.LabelSettings = originalPressureFont;
            pressureRangeDisplay.SetColorsAndRedraw(null);
        }

        // Oxygen Resistance

        var oxygenResistanceWithOrganelles = Math.Max(CurrentTolerances.OxygenResistance + organelleModifiers.OxygenResistance, 0);

        oxygenResistanceRangeDisplay.SetBoundPositionsManual(0, oxygenResistanceWithOrganelles);
        oxygenResistanceRangeDisplay.UpdateMarker(requiredOxygenResistance);
        UpdateSliderPositionForDisplay(oxygenResistanceSlider, oxygenResistanceRangeDisplay);

        oxygenResistanceTotalLabel.Text = percentageFormat.FormatSafe(Math.Round(oxygenResistanceWithOrganelles * 100, 1));

        // Epsilon is subtracted here to avoid -0 triggering this
        if (oxygenResistanceWithOrganelles < requiredOxygenResistance - Mathf.Epsilon)
        {
            oxygenResistanceTotalLabel.LabelSettings = badValueFont;
            oxygenResistanceRangeDisplay.SetColorsAndRedraw(optimalDisplayBadColor);
        }
        else
        {
            oxygenResistanceTotalLabel.LabelSettings = originalTemperatureFont;
            oxygenResistanceRangeDisplay.SetColorsAndRedraw(null);
        }

        if (ShowZeroModifiers || organelleModifiers.OxygenResistance != 0)
        {
            oxygenResistanceModifierLabel.GetParent<Control>().Visible = true;

            var oxygenResistanceBase = percentageFormat.FormatSafe(Math.Round(organelleModifiers.OxygenResistance * 100, 1));
            oxygenResistanceBase = organelleModifiers.OxygenResistance >= 0 ? "+" + oxygenResistanceBase : oxygenResistanceBase;

            oxygenResistanceModifierLabel.Text = oxygenResistanceBase;
            oxygenResistanceModifierLabel.LabelSettings = organelleModifiers.OxygenResistance switch
            {
                > 0 => modifierGoodFont,
                0 => originalModifierFont,
                < 0 => modifierBadFont,
                _ => originalModifierFont,
            };
        }
        else
        {
            oxygenResistanceModifierLabel.GetParent<Control>().Visible = false;
        }

        // UV Resistance

        var uvResistanceWithOrganelles = Math.Max(CurrentTolerances.UVResistance + organelleModifiers.UVResistance, 0);

        uvResistanceRangeDisplay.SetBoundPositionsManual(0, uvResistanceWithOrganelles);
        uvResistanceRangeDisplay.UpdateMarker(requiredUVResistance);
        UpdateSliderPositionForDisplay(uvResistanceSlider, uvResistanceRangeDisplay);

        uvResistanceTotalLabel.Text = percentageFormat.FormatSafe(Math.Round(uvResistanceWithOrganelles * 100, 1));

        if (uvResistanceWithOrganelles < requiredUVResistance - Mathf.Epsilon)
        {
            uvResistanceTotalLabel.LabelSettings = badValueFont;
            uvResistanceRangeDisplay.SetColorsAndRedraw(optimalDisplayBadColor);
        }
        else
        {
            uvResistanceTotalLabel.LabelSettings = originalTemperatureFont;
            uvResistanceRangeDisplay.SetColorsAndRedraw(null);
        }

        if (ShowZeroModifiers || organelleModifiers.UVResistance != 0)
        {
            uvResistanceModifierLabel.GetParent<Control>().Visible = true;

            var uvResistanceBase = percentageFormat.FormatSafe(Math.Round(organelleModifiers.UVResistance * 100, 1));
            uvResistanceBase = organelleModifiers.UVResistance >= 0 ? "+" + uvResistanceBase : uvResistanceBase;

            uvResistanceModifierLabel.Text = uvResistanceBase;
            uvResistanceModifierLabel.LabelSettings = organelleModifiers.UVResistance switch
            {
                > 0 => modifierGoodFont,
                0 => originalModifierFont,
                < 0 => modifierBadFont,
                _ => originalModifierFont,
            };
        }
        else
        {
            uvResistanceModifierLabel.GetParent<Control>().Visible = false;
        }
    }

    private void OnResized()
    {
        UpdateSliderPositionForDisplay(temperatureSlider, temperatureRangeDisplay);
        UpdateSliderPositionForDisplay(pressureSlider, pressureRangeDisplay);
        UpdateSliderPositionForDisplay(oxygenResistanceSlider, oxygenResistanceRangeDisplay);
        UpdateSliderPositionForDisplay(uvResistanceSlider, uvResistanceRangeDisplay);
    }

    [DeserializedCallbackAllowed]
    private void DoToleranceChangeAction(ToleranceActionData data)
    {
        CurrentTolerances.CopyFrom(data.NewTolerances);

        if (!automaticallyChanging)
        {
            // Need to reapply state to the sliders
            automaticallyChanging = true;
            ApplyCurrentValuesToGUI();
            automaticallyChanging = false;
        }

        OnChanged();
    }

    [DeserializedCallbackAllowed]
    private void UndoToleranceChangeAction(ToleranceActionData data)
    {
        CurrentTolerances.CopyFrom(data.OldTolerances);

        if (!automaticallyChanging)
        {
            // Need to reapply state to the sliders
            automaticallyChanging = true;
            ApplyCurrentValuesToGUI();
            automaticallyChanging = false;
        }

        OnChanged();
    }
}
