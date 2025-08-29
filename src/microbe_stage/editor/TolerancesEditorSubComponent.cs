using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Newtonsoft.Json;
using Calculations = MicrobeEnvironmentalToleranceCalculations;

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

    private readonly StringName toleranceFlashName = new("FlashPressureRange");
    private readonly StringName tooWideRangeName = new("PopupPressureRangeWarning");

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
    private Slider pressureMinSlider = null!;

    [Export]
    private Slider pressureMaxSlider = null!;

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
    private Label temperatureBaseLabel = null!;

    [Export]
    private Container temperatureToleranceBaseLabelContainer = null!;

    [Export]
    private Label temperatureToleranceBaseLabel = null!;

    [Export]
    private ToleranceOptimalDisplay temperatureOptimalDisplay = null!;

    [Export]
    private Label pressureMinLabel = null!;

    [Export]
    private Label pressureMaxLabel = null!;

    [Export]
    private Label oxygenResistanceLabel = null!;

    [Export]
    private Label oxygenResistanceBaseLabel = null!;

    [Export]
    private Label oxygenResistanceTotalLabel = null!;

    [Export]
    private ToleranceOptimalDisplay oxygenResistanceOptimalDisplay = null!;

    [Export]
    private Label uvResistanceLabel = null!;

    [Export]
    private Label uvResistanceBaseLabel = null!;

    [Export]
    private Label uvResistanceTotalLabel = null!;

    [Export]
    [ExportCategory("Style")]
    private LabelSettings badValueFont = null!;

    [Export]
    private LabelSettings perfectValueFont = null!;

    [Export]
    private LabelSettings modifierBadFont = null!;

    [Export]
    private LabelSettings modifierGoodFont = null!;

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

    private Calculations.ToleranceValues organelleModifiers;

    [Signal]
    public delegate void OnTolerancesChangedEventHandler();

    [JsonProperty]
    public EnvironmentalTolerances CurrentTolerances { get; private set; } = new();

    [JsonIgnore]
    public override bool IsSubComponent => true;

    public float MPDisplayCostMultiplier { get; set; } = 1;

    private float PressureMaxSliderActualValue
    {
        get => PressureLogScaleToValue((float)pressureMaxSlider.Value);

        set => pressureMaxSlider.Value = PressureValueToLogScale(value);
    }

    private float PressureMinSliderActualValue
    {
        get => PressureLogScaleToValue((float)pressureMinSlider.Value);

        set => pressureMinSlider.Value = PressureValueToLogScale(value);
    }

    public override void _Ready()
    {
        originalTemperatureFont = temperatureMinLabel.LabelSettings;
        originalPressureFont = pressureMinLabel.LabelSettings;
        originalModifierFont = temperatureBaseLabel.LabelSettings;

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
            CalculateOrganelleModifers();

        UpdateCurrentValueDisplays();
        UpdateToolTipStats();
    }

    public void CalculateOrganelleModifers()
    {
        if (Editor.EditedCellOrganelles == null)
            return;

        organelleModifiers = default;

        Calculations.ApplyOrganelleEffectsOnTolerances(Editor.EditedCellOrganelles,
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
            var pressureCost = Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE_STEP * MPDisplayCostMultiplier;

            pressureToolTip.MPCost = pressureCost;
        }

        if (oxygenResistanceToolTip != null)
        {
            var oxygenCost = Constants.TOLERANCE_OXYGEN_STEP * Constants.TOLERANCE_CHANGE_MP_PER_OXYGEN *
                MPDisplayCostMultiplier;

            oxygenResistanceToolTip.MPCost = oxygenCost;
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
        optimal.PressureMinimum = 0;
        optimal.PressureMaximum += Constants.TOLERANCE_PERFECT_THRESHOLD_PRESSURE * 2;
        optimal.TemperatureTolerance += Constants.TOLERANCE_PERFECT_THRESHOLD_TEMPERATURE * 2;

        var tempTolerances = CurrentTolerances.Clone();

#if DEBUG
        tempTolerances.CopyFrom(optimal);
        var optimalTest = Calculations.CalculateTolerances(tempTolerances,
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
            tempTolerances.PressureMaximum = CurrentTolerances.PressureMaximum;

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
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            toleranceFlashName.Dispose();
            tooWideRangeName.Dispose();
        }

        base.Dispose(disposing);
    }

    private void CalculateStatsAndShow(EnvironmentalTolerances calculationTolerances,
        EnvironmentalToleranceToolTip toolTip)
    {
        if (Editor.EditedCellOrganelles == null)
        {
            GD.PrintErr("No cell edited organelles set, tolerances editor cannot update!");
            return;
        }

        var rawTolerances = Calculations.CalculateTolerances(calculationTolerances,
            Editor.EditedCellOrganelles, Editor.CurrentPatch.Biome);

        var resolvedTolerances = Calculations.ResolveToleranceValues(rawTolerances);

        toolTip.UpdateStats(resolvedTolerances);
    }

    private void ApplyCurrentValuesToGUI()
    {
        automaticallyChanging = true;

        temperatureSlider.Value = CurrentTolerances.PreferredTemperature;
        temperatureToleranceRangeSlider.Value = CurrentTolerances.TemperatureTolerance;
        PressureMinSliderActualValue = CurrentTolerances.PressureMinimum;
        PressureMaxSliderActualValue = CurrentTolerances.PressureMaximum;

        UpdateOxygenResistanceLabel(CurrentTolerances.OxygenResistance);

        uvResistanceSlider.Value = CurrentTolerances.UVResistance;

        automaticallyChanging = false;

        UpdateCurrentValueDisplays();
    }

    private void OnTemperatureSliderChanged(float value)
    {
        // Ignore changes triggered by our code to avoid infinite loops
        if (automaticallyChanging)
            return;

        temperatureOptimalDisplay.SetBoundPositions(
            value + organelleModifiers.PreferredTemperature,
            CurrentTolerances.TemperatureTolerance + organelleModifiers.TemperatureTolerance);

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

        temperatureOptimalDisplay.SetBoundPositions(
            CurrentTolerances.PreferredTemperature + organelleModifiers.PreferredTemperature,
            value + organelleModifiers.TemperatureTolerance);

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

    private void OnPressureSliderMinChanged(float logValue)
    {
        if (automaticallyChanging)
            return;

        automaticallyChanging = true;

        var previousRange = Math.Abs(CurrentTolerances.PressureMaximum - CurrentTolerances.PressureMinimum);

        var value = PressureLogScaleToValue(logValue);

        var newRange = Math.Abs(PressureMaxSliderActualValue - value);

        // Ensure flexibility doesn't go above the configured limit
        if (newRange > previousRange && newRange > Constants.TOLERANCE_PRESSURE_RANGE_MAX)
        {
            PressureMinSliderActualValue = CurrentTolerances.PressureMinimum;
            automaticallyChanging = false;
            return;
        }

        // Min can't go above the max
        if (value > PressureMaxSliderActualValue)
        {
            PressureMaxSliderActualValue = value;
        }

        TryApplyPressureChange(value, PressureMaxSliderActualValue);
        automaticallyChanging = false;
    }

    private void OnPressureSliderMaxChanged(float logValue)
    {
        if (automaticallyChanging)
            return;

        automaticallyChanging = true;

        var previousRange = Math.Abs(CurrentTolerances.PressureMaximum - CurrentTolerances.PressureMinimum);

        var value = PressureLogScaleToValue(logValue);

        var newRange = Math.Abs(value - PressureMinSliderActualValue);

        if (newRange > previousRange && newRange > Constants.TOLERANCE_PRESSURE_RANGE_MAX)
        {
            PressureMaxSliderActualValue = CurrentTolerances.PressureMaximum;
            automaticallyChanging = false;
            return;
        }

        // Max can't go below the min
        if (value < PressureMinSliderActualValue)
        {
            PressureMinSliderActualValue = value;
        }

        TryApplyPressureChange(PressureMinSliderActualValue, value);
        automaticallyChanging = false;
    }

    private float PressureLogScaleToValue(float logValue)
    {
        return MathF.Pow(10, logValue / 20 + Constants.TOLERANCE_PRESSURE_LOG_SCALE_OFFSET);
    }

    private float PressureValueToLogScale(float rawValue)
    {
        return (MathF.Log10(rawValue) - Constants.TOLERANCE_PRESSURE_LOG_SCALE_OFFSET) * 20;
    }

    private void TryApplyPressureChange(float min, float max)
    {
        reusableTolerances ??= new EnvironmentalTolerances();
        reusableTolerances.CopyFrom(CurrentTolerances);
        reusableTolerances.PressureMinimum = min;
        reusableTolerances.PressureMaximum = max;
        reusableTolerances.PressureMinLogScale = PressureValueToLogScale(min);
        reusableTolerances.PressureMaxLogScale = PressureValueToLogScale(max);

        if (!TriggerChangeIfPossible())
        {
            PressureMinSliderActualValue = CurrentTolerances.PressureMinimum;
            PressureMaxSliderActualValue = CurrentTolerances.PressureMaximum;
        }
    }

    private void OnOxygenResistancePlusButtonPressed()
    {
        if (automaticallyChanging)
            return;

        // Make sure the tolerance doesn't go above 100%
        if (CurrentTolerances.OxygenResistance + Constants.TOLERANCE_OXYGEN_STEP > 1)
            return;

        reusableTolerances ??= new EnvironmentalTolerances();
        reusableTolerances.CopyFrom(CurrentTolerances);
        reusableTolerances.OxygenResistance += Constants.TOLERANCE_OXYGEN_STEP;

        automaticallyChanging = true;

        TriggerChangeIfPossible();

        // As pressing the button doesn't update the label by itself
        // we have to update it here no matter what the result of TriggerChangeIfPossible is
        UpdateOxygenResistanceLabel(CurrentTolerances.OxygenResistance);

        automaticallyChanging = false;
    }

    private void OnOxygenResistanceMinusButtonPressed()
    {
        if (automaticallyChanging)
            return;

        // Make sure the tolerance doesn't go below 0%
        if (CurrentTolerances.OxygenResistance - Constants.TOLERANCE_OXYGEN_STEP < 0)
            return;

        reusableTolerances ??= new EnvironmentalTolerances();
        reusableTolerances.CopyFrom(CurrentTolerances);
        reusableTolerances.OxygenResistance -= Constants.TOLERANCE_OXYGEN_STEP;

        automaticallyChanging = true;

        TriggerChangeIfPossible();

        UpdateOxygenResistanceLabel(CurrentTolerances.OxygenResistance);

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

    private void UpdateOxygenResistanceLabel(float value)
    {
        var percentageFormat = Localization.Translate("PERCENTAGE_VALUE");
        var percentage = percentageFormat.FormatSafe(Math.Round(value * 100, 1));
        oxygenResistanceLabel.Text = "+" + percentage;
    }

    private void UpdateCurrentValueDisplays()
    {
        var patch = Editor.CurrentPatch;
        var patchTemperature = patch.Biome.GetCompound(Compound.Temperature, CompoundAmountType.Biome).Ambient;
        var patchPressure = patch.Biome.Pressure;
        var requiredOxygenResistance = patch.Biome.CalculateOxygenResistanceFactor();
        var requiredUVResistance = patch.Biome.CalculateUVFactor();

        // This relies on CalculateToleranceRangeForGUI having been called when necessary

        var unitFormat = Localization.Translate("VALUE_WITH_UNIT");
        var percentageFormat = Localization.Translate("PERCENTAGE_VALUE");
        var plusMinusUnitFormat = Localization.Translate("PLUS_MINUS_VALUE_WITH_UNIT");

        // Temperature

        var preferredTemperatureWithOrganelles =
            CurrentTolerances.PreferredTemperature + organelleModifiers.PreferredTemperature;

        var temperatureToleranceWithOrganelles =
            CurrentTolerances.TemperatureTolerance + organelleModifiers.TemperatureTolerance;

        temperatureOptimalDisplay.SetBoundPositions(preferredTemperatureWithOrganelles, temperatureToleranceWithOrganelles);
        temperatureOptimalDisplay.UpdateMarker(patchTemperature);

        temperatureMinLabel.Text =
            unitFormat.FormatSafe(
                Math.Round(preferredTemperatureWithOrganelles - temperatureToleranceWithOrganelles, 1),
                temperature.Unit);
        temperatureMaxLabel.Text =
            unitFormat.FormatSafe(
                Math.Round(preferredTemperatureWithOrganelles + temperatureToleranceWithOrganelles, 1),
                temperature.Unit);

        temperatureBaseLabel.Text =
            unitFormat.FormatSafe(
                Math.Round(organelleModifiers.PreferredTemperature, 1),
                temperature.Unit);

        temperatureToleranceLabel.Text =
            plusMinusUnitFormat.FormatSafe(
                Math.Round(CurrentTolerances.TemperatureTolerance, 1),
                temperature.Unit);

        if (ShowZeroModifiers || organelleModifiers.TemperatureTolerance != 0)
        {
            var modifierWithUnit = plusMinusUnitFormat.FormatSafe(
                Math.Round(organelleModifiers.TemperatureTolerance, 1),
                temperature.Unit);

            temperatureToleranceBaseLabel.Text = $"({modifierWithUnit})";

            temperatureToleranceBaseLabelContainer.Visible = true;
        }
        else
        {
            temperatureToleranceBaseLabelContainer.Visible = false;
        }

        // Show in red the conditions that are not matching to make them easier to notice
        if (Math.Abs(patchTemperature - preferredTemperatureWithOrganelles) >
            temperatureToleranceWithOrganelles)
        {
            // Mark the direction that is bad as the one having the problem to make it easier for the player to see
            // what is wrong
            if (patchTemperature > preferredTemperatureWithOrganelles)
            {
                temperatureMaxLabel.LabelSettings = badValueFont;
                temperatureMinLabel.LabelSettings = originalTemperatureFont;
            }
            else
            {
                temperatureMinLabel.LabelSettings = badValueFont;
                temperatureMaxLabel.LabelSettings = originalTemperatureFont;
            }
        }
        else if (Math.Abs(CurrentTolerances.TemperatureTolerance) < Constants.TOLERANCE_PERFECT_THRESHOLD_TEMPERATURE)
        {
            // Perfectly adapted
            temperatureMinLabel.LabelSettings = perfectValueFont;
            temperatureMaxLabel.LabelSettings = perfectValueFont;
        }
        else
        {
            temperatureMinLabel.LabelSettings = originalTemperatureFont;
            temperatureMaxLabel.LabelSettings = originalTemperatureFont;
        }

        // Oxygen Resistance

        var oxygenResistanceWithOrganelles = CurrentTolerances.OxygenResistance + organelleModifiers.OxygenResistance;

        oxygenResistanceOptimalDisplay.SetBoundPositionsManual(0, oxygenResistanceWithOrganelles);
        oxygenResistanceOptimalDisplay.UpdateMarker(requiredOxygenResistance);

        oxygenResistanceTotalLabel.Text =
            percentageFormat.FormatSafe(Math.Round(oxygenResistanceWithOrganelles * 100, 1));

        if (oxygenResistanceWithOrganelles < requiredOxygenResistance)
        {
            oxygenResistanceTotalLabel.LabelSettings = badValueFont;
        }
        else
        {
            oxygenResistanceTotalLabel.LabelSettings = originalTemperatureFont;
        }

        var oxygenResistanceBase = percentageFormat.FormatSafe(Math.Round(organelleModifiers.OxygenResistance * 100, 1));
        oxygenResistanceBase = organelleModifiers.OxygenResistance >= 0 ? "+" + oxygenResistanceBase : oxygenResistanceBase;

        oxygenResistanceBaseLabel.Text = oxygenResistanceBase;
        oxygenResistanceBaseLabel.LabelSettings = organelleModifiers.OxygenResistance < 0 ? modifierBadFont : originalModifierFont;

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
