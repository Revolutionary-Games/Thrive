using System;
using System.Collections.Generic;
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

    private readonly StringName toleranceFlashName = new("FlashPressureRange");
    private readonly StringName tooWideRangeName = new("PopupPressureRangeWarning");

    private readonly CompoundDefinition temperature = SimulationParameters.GetCompound(Compound.Temperature);

    private readonly Dictionary<OrganelleDefinition, float> tempToleranceModifiers = new();

#pragma warning disable CA2213

    [Export]
    private Container temperatureContainer = null!;

    [Export]
    private Container temperatureToleranceContainer = null!;

    [Export]
    private Container pressureContainer = null!;

    [Export]
    private Container pressureRangeContainer = null!;

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
    private Slider pressureMinSlider = null!;

    [Export]
    private Slider pressureMaxSlider = null!;

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
    private Label temperatureToleranceModifierLabel = null!;

    [Export]
    private Label pressureMinLabel = null!;

    [Export]
    private Label pressureMaxLabel = null!;

    [Export]
    private Label pressureToleranceModifierLabel = null!;

    [Export]
    private Label oxygenResistanceLabel = null!;

    [Export]
    private Label oxygenResistanceModifierLabel = null!;

    [Export]
    private Label uvResistanceLabel = null!;

    [Export]
    private Label uvResistanceModifierLabel = null!;

    [Export]
    private ToleranceOptimalMarker temperatureToleranceMarker = null!;

    [Export]
    private ToleranceOptimalMarker minPressureToleranceMarker = null!;

    [Export]
    private ToleranceOptimalMarker maxPressureToleranceMarker = null!;

    [Export]
    private ToleranceOptimalMarker oxygenToleranceMarker = null!;

    [Export]
    private ToleranceOptimalMarker uvToleranceMarker = null!;

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

    /// <summary>
    ///   When true, links the max and min pressure sliders to keep a consistent range
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
        originalModifierFont = temperatureToleranceModifierLabel.LabelSettings;

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
        UpdateAdditionalSliderInfo();
    }

    public override void OnFinishEditing()
    {
        // Apply the tolerances
        Editor.EditedBaseSpecies.Tolerances.CopyFrom(CurrentTolerances);
    }

    public void OnDataTolerancesDependOnChanged()
    {
        UpdateCurrentValueDisplays();
        UpdateToolTipStats();

        UpdateAdditionalSliderInfo();
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

        UpdateEffectiveValueToolTips();
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

        temperatureContainer.RegisterToolTipForControl("temperature", "tolerances");
        temperatureToleranceContainer.RegisterToolTipForControl("temperatureRangeModifier", "tolerances");
        pressureContainer.RegisterToolTipForControl("pressure", "tolerances");
        pressureRangeContainer.RegisterToolTipForControl("pressureRangeModifier", "tolerances");
        oxygenResistanceContainer.RegisterToolTipForControl("oxygenResistance", "tolerances");
        oxygenResistanceModifierLabel.RegisterToolTipForControl("oxygenResistanceModifier", "tolerances");
        uvResistanceContainer.RegisterToolTipForControl("uvResistance", "tolerances");
        uvResistanceModifierLabel.RegisterToolTipForControl("uvResistanceModifier", "tolerances");

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
            temperatureRangeToolTip.ValueSuffix = SimulationParameters.GetCompound(Compound.Temperature).Unit;
        }
        else
        {
            GD.PrintErr("Tooltips not correctly found for tolerances editor");
        }
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

        var rawTolerances =
            MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(calculationTolerances,
                Editor.EditedCellOrganelles, Editor.CurrentPatch.Biome);

        var resolvedTolerances = MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(rawTolerances);

        toolTip.UpdateStats(resolvedTolerances);
    }

    private void UpdateEffectiveValueToolTips()
    {
        if (Editor.EditedCellOrganelles == null)
        {
            GD.PrintErr("no cell edited organelles set, cannot update effective value tooltips");
            return;
        }

        var tolerances = default(MicrobeEnvironmentalToleranceCalculations.ToleranceValues);

        // Take the current values as base and apply the organelle modifiers on top to get effective values
        tolerances.CopyFrom(CurrentTolerances);

        MicrobeEnvironmentalToleranceCalculations.ApplyOrganelleEffectsOnTolerances(Editor.EditedCellOrganelles,
            ref tolerances);

        if (temperatureRangeToolTip != null)
        {
            temperatureRangeToolTip.DisplayedValue = tolerances.TemperatureTolerance;

            // Calculate organelle summaries so that the info can be shown in the tooltips
            MicrobeEnvironmentalToleranceCalculations.GenerateToleranceEffectSummariesByOrganelle(
                Editor.EditedCellOrganelles, ToleranceModifier.TemperatureRange, tempToleranceModifiers);

            temperatureRangeToolTip.DisplayOrganelleBreakdown(tempToleranceModifiers);
        }

        if (pressureRangeToolTip != null)
        {
            pressureRangeToolTip.DisplayedValue = tolerances.PressureMaximum - tolerances.PressureMinimum;

            MicrobeEnvironmentalToleranceCalculations.GenerateToleranceEffectSummariesByOrganelle(
                Editor.EditedCellOrganelles, ToleranceModifier.PressureRange, tempToleranceModifiers);
            pressureRangeToolTip.DisplayOrganelleBreakdown(tempToleranceModifiers);
        }

        if (oxygenResistanceModifierToolTip != null)
        {
            oxygenResistanceModifierToolTip.DisplayedValue = tolerances.OxygenResistance;

            MicrobeEnvironmentalToleranceCalculations.GenerateToleranceEffectSummariesByOrganelle(
                Editor.EditedCellOrganelles, ToleranceModifier.Oxygen, tempToleranceModifiers);
            oxygenResistanceModifierToolTip.DisplayOrganelleBreakdown(tempToleranceModifiers);
        }

        if (uvResistanceModifierToolTip != null)
        {
            uvResistanceModifierToolTip.DisplayedValue = tolerances.UVResistance;

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

        var organelleModifiers = default(MicrobeEnvironmentalToleranceCalculations.ToleranceValues);
        bool gotOrganelles = false;

        if (Editor.EditedCellOrganelles != null)
        {
            gotOrganelles = true;
            MicrobeEnvironmentalToleranceCalculations.ApplyOrganelleEffectsOnTolerances(Editor.EditedCellOrganelles,
                ref organelleModifiers);
        }

        var preferredTemperatureWithOrganelles =
            CurrentTolerances.PreferredTemperature + organelleModifiers.PreferredTemperature;

        var temperatureToleranceWithOrganelles =
            CurrentTolerances.TemperatureTolerance + organelleModifiers.TemperatureTolerance;

        temperatureMinLabel.Text =
            unitFormat.FormatSafe(
                Math.Round(preferredTemperatureWithOrganelles - temperatureToleranceWithOrganelles, 1),
                temperature.Unit);
        temperatureMaxLabel.Text =
            unitFormat.FormatSafe(
                Math.Round(preferredTemperatureWithOrganelles + temperatureToleranceWithOrganelles, 1),
                temperature.Unit);

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

        var pressureMinWithOrganelles =
            Math.Max(0, CurrentTolerances.PressureMinimum + organelleModifiers.PressureMinimum);
        var pressureMaxWithOrganelles = CurrentTolerances.PressureMaximum + organelleModifiers.PressureMaximum;

        pressureMinLabel.Text = unitFormat.FormatSafe(Math.Round(pressureMinWithOrganelles / 1000), "kPa");
        pressureMaxLabel.Text = unitFormat.FormatSafe(Math.Round(pressureMaxWithOrganelles / 1000), "kPa");

        if (patchPressure > pressureMaxWithOrganelles)
        {
            pressureMaxLabel.LabelSettings = badValueFont;
            pressureMinLabel.LabelSettings = originalPressureFont;
        }
        else if (patchPressure < pressureMinWithOrganelles)
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

        if (!gotOrganelles)
        {
            GD.PrintErr("Cannot update effective tolerance values without organelles");
            return;
        }

        // Update then the effective ranges and modifier values

        // Temperature
        temperatureToleranceLabel.Text =
            unitFormat.FormatSafe(Math.Round(CurrentTolerances.TemperatureTolerance, 1), temperature.Unit);

        var value = unitFormat.FormatSafe(Math.Round(organelleModifiers.TemperatureTolerance, 1), temperature.Unit);

        value = organelleModifiers.TemperatureTolerance >= 0 ? "+" + value : value;

        if (ShowZeroModifiers || organelleModifiers.TemperatureTolerance != 0)
        {
            temperatureToleranceModifierLabel.Text = $"({value})";
            temperatureToleranceModifierLabel.Visible = true;

            if (organelleModifiers.TemperatureTolerance < 0)
            {
                temperatureToleranceModifierLabel.LabelSettings = modifierBadFont;
            }
            else
            {
                temperatureToleranceModifierLabel.LabelSettings = originalModifierFont;
            }
        }
        else
        {
            temperatureToleranceModifierLabel.Visible = false;
        }

        // Pressure. This is slightly different in that we only have this one display, so it does double duty to show
        // the bonus as well as the current range
        value = unitFormat.FormatSafe(Math.Round(
            (Math.Abs(CurrentTolerances.PressureMaximum - CurrentTolerances.PressureMinimum) +
                organelleModifiers.PressureMaximum) / 1000), "kPa");
        pressureToleranceModifierLabel.Text = value;

        // Make pressure green if within the perfect adaptation range as this is a total range display and not just
        // the modifier like the other values
        if (Math.Abs(CurrentTolerances.PressureMaximum - CurrentTolerances.PressureMinimum) <=
            Constants.TOLERANCE_PERFECT_THRESHOLD_PRESSURE)
        {
            pressureToleranceModifierLabel.LabelSettings = modifierGoodFont;
        }
        else
        {
            pressureToleranceModifierLabel.LabelSettings = originalModifierFont;
        }

        // Oxygen
        value = percentageFormat.FormatSafe(Math.Round(organelleModifiers.OxygenResistance * 100, 1));
        value = organelleModifiers.OxygenResistance >= 0 ? "+" + value : value;

        if (ShowZeroModifiers || organelleModifiers.OxygenResistance != 0)
        {
            oxygenResistanceModifierLabel.Text = $"({value})";
            oxygenResistanceModifierLabel.Visible = true;

            if (organelleModifiers.OxygenResistance < 0)
            {
                oxygenResistanceModifierLabel.LabelSettings = modifierBadFont;
            }
            else
            {
                oxygenResistanceModifierLabel.LabelSettings = originalModifierFont;
            }
        }
        else
        {
            oxygenResistanceModifierLabel.Visible = false;
        }

        // UV
        value = percentageFormat.FormatSafe(Math.Round(organelleModifiers.UVResistance * 100, 1));
        value = organelleModifiers.UVResistance >= 0 ? "+" + value : value;

        if (ShowZeroModifiers || organelleModifiers.UVResistance != 0)
        {
            uvResistanceModifierLabel.Text = $"({value})";
            uvResistanceModifierLabel.Visible = true;

            if (organelleModifiers.UVResistance < 0)
            {
                uvResistanceModifierLabel.LabelSettings = modifierBadFont;
            }
            else
            {
                uvResistanceModifierLabel.LabelSettings = originalModifierFont;
            }
        }
        else
        {
            uvResistanceModifierLabel.Visible = false;
        }
    }

    private void UpdateAdditionalSliderInfo()
    {
        var patch = Editor.CurrentPatch;
        var patchTemperature = patch.Biome.GetCompound(Compound.Temperature, CompoundAmountType.Biome).Ambient;
        var patchPressure = patch.Biome.Pressure;
        var requiredOxygenResistance = patch.Biome.CalculateOxygenResistanceFactor();
        var requiredUVResistance = patch.Biome.CalculateUVFactor();

        var unitFormat = Localization.Translate("VALUE_WITH_UNIT");
        var percentageFormat = Localization.Translate("PERCENTAGE_VALUE");

        temperatureToleranceMarker.UpdateBoundaryLabels(unitFormat.FormatSafe(0, temperature.Unit),
            unitFormat.FormatSafe(100, temperature.Unit));
        temperatureToleranceMarker.UpdateMarker(patchTemperature / 100.0f);

        string minPressure = unitFormat.FormatSafe(0, "kPa");
        string maxPressure = unitFormat.FormatSafe(70000, "kPa");

        minPressureToleranceMarker.UpdateBoundaryLabels(minPressure, maxPressure);
        minPressureToleranceMarker.UpdateMarker(patchPressure / 70000000);

        maxPressureToleranceMarker.UpdateBoundaryLabels(minPressure, maxPressure);
        maxPressureToleranceMarker.UpdateMarker(patchPressure / 70000000);

        string zeroPercents = percentageFormat.FormatSafe(0);
        string hundredPercents = percentageFormat.FormatSafe(100);

        // Don't show markers when they are at 0% as it looks confusing
        oxygenToleranceMarker.ShowMarker = requiredOxygenResistance > MathUtils.EPSILON;
        uvToleranceMarker.ShowMarker = requiredUVResistance > MathUtils.EPSILON;

        oxygenToleranceMarker.UpdateBoundaryLabels(zeroPercents, hundredPercents);
        oxygenToleranceMarker.UpdateMarker(requiredOxygenResistance);

        uvToleranceMarker.UpdateBoundaryLabels(zeroPercents, hundredPercents);
        uvToleranceMarker.UpdateMarker(requiredUVResistance);
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
