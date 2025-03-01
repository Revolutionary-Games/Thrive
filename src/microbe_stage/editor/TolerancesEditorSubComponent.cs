using System;
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
    private readonly StringName toleranceFlashName = new("FlashPressureRange");
    private readonly StringName tooWideRangeName = new("PopupPressureRangeWarning");

    private readonly CompoundDefinition temperature = SimulationParameters.GetCompound(Compound.Temperature);

#pragma warning disable CA2213

    [Export]
    private Container temperatureContainer = null!;

    [Export]
    private Container pressureContainer = null!;

    [Export]
    private Container oxygenResistanceContainer = null!;

    [Export]
    private Container uvResistanceContainer = null!;

    [Export]
    private AnimationPlayer invalidChangeAnimation = null!;

    [Export]
    [ExportCategory("Inputs")]
    private Slider temperatureSlider = null!;

    [Export]
    private Slider temperatureToleranceRangeSlider = null!;

    [Export]
    private ToleranceInfo temperatureToleranceInfo = null!;

    [Export]
    private Slider pressureMinSlider = null!;

    [Export]
    private Slider pressureMaxSlider = null!;

    [Export]
    private ToleranceInfo minPressureToleranceInfo = null!;

    [Export]
    private ToleranceInfo maxPressureToleranceInfo = null!;

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
    private Label pressureMinLabel = null!;

    [Export]
    private Label pressureMaxLabel = null!;

    [Export]
    private Label oxygenResistanceLabel = null!;

    [Export]
    private Label uvResistanceLabel = null!;

    [Export]
    [ExportCategory("Style")]
    private LabelSettings badValueFont = null!;

    [Export]
    private LabelSettings perfectValueFont = null!;

    private LabelSettings? originalTemperatureFont;
    private LabelSettings? originalPressureFont;

    private EnvironmentalToleranceToolTip? temperatureToolTip;
    private EnvironmentalToleranceToolTip? pressureToolTip;
    private EnvironmentalToleranceToolTip? oxygenResistanceToolTip;
    private EnvironmentalToleranceToolTip? uvResistanceToolTip;
#pragma warning restore CA2213

    private bool automaticallyChanging;

    private bool wasFreshInit;

    /// <summary>
    ///   Reusable tolerances object for checking things until it is consumed by using it up in an action
    /// </summary>
    private EnvironmentalTolerances? reusableTolerances;

    /// <summary>
    ///   When true links the max and min pressure sliders to keep a consistent range
    /// </summary>
    private bool keepSamePressureFlexibility = true;

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

    public void OnPatchChanged()
    {
        UpdateCurrentValueDisplays();
        UpdateToolTipStats();
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
            var pressureCost = pressureMinSlider.Step * Constants.TOLERANCE_CHANGE_MP_PER_PRESSURE *
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
        // Calculate one stat at a time to get the individual changes per type instead of all combined
        var optimal = Editor.CurrentPatch.GenerateTolerancesForMicrobe();

        // Set huge ranges so that there is no threat of optimal bonuses triggering with the default calculations
        optimal.PressureMinimum = 0;
        optimal.PressureMaximum += Constants.TOLERANCE_PERFECT_THRESHOLD_PRESSURE * 2;
        optimal.TemperatureTolerance += Constants.TOLERANCE_PERFECT_THRESHOLD_TEMPERATURE * 2;

        var tempTolerances = CurrentTolerances.Clone();

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
        UpdateMPCostInToolTips();
        UpdateToolTipStats();
    }

    protected override void RegisterTooltips()
    {
        base.RegisterTooltips();

        temperatureContainer.RegisterToolTipForControl("temperature", "tolerances");
        pressureContainer.RegisterToolTipForControl("pressure", "tolerances");
        oxygenResistanceContainer.RegisterToolTipForControl("oxygenResistance", "tolerances");
        uvResistanceContainer.RegisterToolTipForControl("uvResistance", "tolerances");

        var toolTipManager = ToolTipManager.Instance;
        temperatureToolTip = toolTipManager.GetToolTip<EnvironmentalToleranceToolTip>("temperature", "tolerances");
        pressureToolTip = toolTipManager.GetToolTip<EnvironmentalToleranceToolTip>("pressure", "tolerances");
        oxygenResistanceToolTip =
            toolTipManager.GetToolTip<EnvironmentalToleranceToolTip>("oxygenResistance", "tolerances");
        uvResistanceToolTip = toolTipManager.GetToolTip<EnvironmentalToleranceToolTip>("uvResistance", "tolerances");
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
        var rawTolerances =
            MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(calculationTolerances,
                Editor.CurrentPatch.Biome);

        var resolvedTolerances = MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(rawTolerances);

        toolTip.UpdateStats(resolvedTolerances);
    }

    private void ApplyCurrentValuesToGUI()
    {
        automaticallyChanging = true;

        temperatureSlider.Value = CurrentTolerances.PreferredTemperature;
        temperatureToleranceRangeSlider.Value = CurrentTolerances.TemperatureTolerance;
        pressureMinSlider.Value = CurrentTolerances.PressureMinimum;
        pressureMaxSlider.Value = CurrentTolerances.PressureMaximum;
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

    private void OnPressureSliderMinChanged(float value)
    {
        if (automaticallyChanging)
            return;

        automaticallyChanging = true;

        var previousRange = Math.Abs(CurrentTolerances.PressureMaximum - CurrentTolerances.PressureMinimum);

        if (keepSamePressureFlexibility)
        {
            // Update the other slider to keep the current flexibility range
            var maxShouldBe = value + previousRange;

            // If not possible restricted the movement and flash the toggle button red
            if (maxShouldBe > pressureMaxSlider.MaxValue)
            {
                // Not possible to change
                pressureMaxSlider.Value = pressureMaxSlider.MaxValue;
                invalidChangeAnimation.Play(toleranceFlashName);

                // This will trigger the signal again for processing retry
                automaticallyChanging = false;
                pressureMinSlider.Value = pressureMaxSlider.MaxValue - previousRange;
                return;
            }

            // Adjust the dependent slider
            pressureMaxSlider.Value = maxShouldBe;
        }
        else
        {
            var newRange = Math.Abs(pressureMaxSlider.Value - value);

            // Ensure flexibility doesn't go above the configured limit
            if (newRange > previousRange && newRange > Constants.TOLERANCE_PRESSURE_RANGE_MAX)
            {
                pressureMinSlider.Value = CurrentTolerances.PressureMinimum;

                invalidChangeAnimation.Play(tooWideRangeName);
                automaticallyChanging = false;
                return;
            }

            // Min can't go above the max
            if (value > pressureMaxSlider.Value)
            {
                pressureMaxSlider.Value = value;
            }
        }

        TryApplyPressureChange(value, (float)pressureMaxSlider.Value);
        automaticallyChanging = false;
    }

    private void OnPressureSliderMaxChanged(float value)
    {
        if (automaticallyChanging)
            return;

        automaticallyChanging = true;

        var previousRange = Math.Abs(CurrentTolerances.PressureMaximum - CurrentTolerances.PressureMinimum);

        if (keepSamePressureFlexibility)
        {
            // Update the other slider to keep the current flexibility range
            var minShouldBe = value - previousRange;

            if (minShouldBe < pressureMinSlider.MinValue)
            {
                // Not possible to change
                pressureMinSlider.Value = pressureMinSlider.MinValue;
                invalidChangeAnimation.Play(toleranceFlashName);

                // This will trigger the signal again for processing retry
                automaticallyChanging = false;
                pressureMaxSlider.Value = pressureMinSlider.MinValue + previousRange;
                return;
            }

            pressureMinSlider.Value = minShouldBe;
        }
        else
        {
            var newRange = Math.Abs(value - pressureMinSlider.Value);

            if (newRange > previousRange && newRange > Constants.TOLERANCE_PRESSURE_RANGE_MAX)
            {
                pressureMaxSlider.Value = CurrentTolerances.PressureMaximum;

                invalidChangeAnimation.Play(tooWideRangeName);

                automaticallyChanging = false;
                return;
            }

            // Max can't go below the min
            if (value < pressureMinSlider.Value)
            {
                pressureMinSlider.Value = value;
            }
        }

        TryApplyPressureChange((float)pressureMinSlider.Value, value);
        automaticallyChanging = false;
    }

    private void OnKeepPressureFlexibilityToggled(bool keepCurrent)
    {
        keepSamePressureFlexibility = keepCurrent;
    }

    private void TryApplyPressureChange(float min, float max)
    {
        reusableTolerances ??= new EnvironmentalTolerances();
        reusableTolerances.CopyFrom(CurrentTolerances);
        reusableTolerances.PressureMinimum = min;
        reusableTolerances.PressureMaximum = max;

        if (!TriggerChangeIfPossible())
        {
            pressureMinSlider.Value = CurrentTolerances.PressureMinimum;
            pressureMaxSlider.Value = CurrentTolerances.PressureMaximum;
        }
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

        // TODO: green for perfectly being adapted?

        temperatureMinLabel.Text =
            unitFormat.FormatSafe(
                Math.Round(CurrentTolerances.PreferredTemperature - CurrentTolerances.TemperatureTolerance, 1),
                temperature.Unit);
        temperatureMaxLabel.Text =
            unitFormat.FormatSafe(
                Math.Round(CurrentTolerances.PreferredTemperature + CurrentTolerances.TemperatureTolerance, 1),
                temperature.Unit);

        // Show in red the conditions that are not matching to make them easier to notice
        if (Math.Abs(patchTemperature - CurrentTolerances.PreferredTemperature) >
            CurrentTolerances.TemperatureTolerance)
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

        pressureMinLabel.Text = unitFormat.FormatSafe(Math.Round(CurrentTolerances.PressureMinimum / 1000), "kPa");
        pressureMaxLabel.Text = unitFormat.FormatSafe(Math.Round(CurrentTolerances.PressureMaximum / 1000), "kPa");

        if (patchPressure > CurrentTolerances.PressureMaximum)
        {
            pressureMaxLabel.LabelSettings = badValueFont;
            pressureMinLabel.LabelSettings = originalPressureFont;
        }
        else if (patchPressure < CurrentTolerances.PressureMinimum)
        {
            pressureMinLabel.LabelSettings = badValueFont;
            pressureMaxLabel.LabelSettings = originalPressureFont;
        }
        else if (Math.Abs(CurrentTolerances.PressureMaximum - CurrentTolerances.PressureMinimum) <
                 Constants.TOLERANCE_PERFECT_THRESHOLD_PRESSURE)
        {
            pressureMinLabel.LabelSettings = perfectValueFont;
            pressureMaxLabel.LabelSettings = perfectValueFont;
        }
        else
        {
            pressureMinLabel.LabelSettings = originalPressureFont;
            pressureMaxLabel.LabelSettings = originalPressureFont;
        }

        oxygenResistanceLabel.Text =
            percentageFormat.FormatSafe(Math.Round(CurrentTolerances.OxygenResistance * 100, 1));

        if (CurrentTolerances.OxygenResistance < requiredOxygenResistance)
        {
            oxygenResistanceLabel.LabelSettings = badValueFont;
        }
        else
        {
            oxygenResistanceLabel.LabelSettings = originalTemperatureFont;
        }

        uvResistanceLabel.Text = percentageFormat.FormatSafe(Math.Round(CurrentTolerances.UVResistance * 100, 1));

        if (CurrentTolerances.UVResistance < requiredUVResistance)
        {
            uvResistanceLabel.LabelSettings = badValueFont;
        }
        else
        {
            uvResistanceLabel.LabelSettings = originalTemperatureFont;
        }

        temperatureToleranceInfo.UpdateInfo(0, 100, patchTemperature / 100.0f);
        minPressureToleranceInfo.UpdateInfo(0, 70000000, patchPressure / 70000000);
        maxPressureToleranceInfo.UpdateInfo(0, 70000000, patchPressure / 70000000);
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
