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
    private readonly CompoundDefinition temperature = SimulationParameters.GetCompound(Compound.Temperature);

#pragma warning disable CA2213
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
#pragma warning restore CA2213

    private bool automaticallyChanging;

    [JsonProperty]
    private float currentPressureToleranceRange;

    private bool wasFreshInit;

    /// <summary>
    ///   Reusable tolerances object for checking things until it is consumed by using it up in an action
    /// </summary>
    private EnvironmentalTolerances? reusableTolerances;

    [Signal]
    public delegate void OnTolerancesChangedEventHandler();

    [JsonProperty]
    public EnvironmentalTolerances CurrentTolerances { get; private set; } = new();

    [JsonIgnore]
    public override bool IsSubComponent => true;

    public override void _Ready()
    {
        originalTemperatureFont = temperatureMinLabel.LabelSettings;
        originalPressureFont = pressureMinLabel.LabelSettings;
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
    }

    public override void OnFinishEditing()
    {
        // Apply the tolerances
        Editor.EditedBaseSpecies.Tolerances.CopyFrom(CurrentTolerances);
    }

    public void OnPatchChanged()
    {
        UpdateCurrentValueDisplays();
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

    protected override void OnTranslationsChanged()
    {
    }

    // TODO: tooltips explaining the tolerance stuff
    /*protected override void RegisterTooltips()
    {
        base.RegisterTooltips();

        // aggressionSlider.RegisterToolTipForControl("aggressionSlider", "editor");
    }*/

    private void CalculateToleranceRangeForGUI()
    {
        // TODO: maybe the GUI should have separate sliders as well?
        currentPressureToleranceRange =
            (CurrentTolerances.PressureMaximum - CurrentTolerances.PressureMinimum) * 0.5f;
    }

    private void ApplyCurrentValuesToGUI()
    {
        CalculateToleranceRangeForGUI();

        automaticallyChanging = true;

        temperatureSlider.Value = CurrentTolerances.PreferredTemperature;
        temperatureToleranceRangeSlider.Value = CurrentTolerances.TemperatureTolerance;
        pressureSlider.Value = CurrentTolerances.PreferredPressure;
        pressureToleranceRangeSlider.Value = currentPressureToleranceRange;
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
        reusableTolerances.PreferredPressure = value;

        automaticallyChanging = true;

        if (!TriggerChangeIfPossible())
        {
            pressureSlider.Value = CurrentTolerances.PreferredPressure;
        }

        automaticallyChanging = false;
    }

    private void OnPressureToleranceRangeSliderChanged(float value)
    {
        if (automaticallyChanging)
            return;

        // This is a bit of a special case as this is a derived property not directly on the slider
        var min = CurrentTolerances.PreferredPressure - value;
        var max = CurrentTolerances.PreferredPressure + value;

        reusableTolerances ??= new EnvironmentalTolerances();
        reusableTolerances.CopyFrom(CurrentTolerances);
        reusableTolerances.PressureMinimum = Math.Max(min, 0);
        reusableTolerances.PressureMaximum = max;

        automaticallyChanging = true;

        if (!TriggerChangeIfPossible())
        {
            CalculateToleranceRangeForGUI();
            pressureToleranceRangeSlider.Value = currentPressureToleranceRange;
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

        CalculateToleranceRangeForGUI();

        automaticallyChanging = true;
        pressureToleranceRangeSlider.Value = currentPressureToleranceRange;
        automaticallyChanging = false;

        UpdateCurrentValueDisplays();
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

        // TODO: rather than using Max here, would it be better if this used the actual min and max as calculated?
        pressureMinLabel.Text =
            unitFormat.FormatSafe(
                Math.Round(Math.Max(CurrentTolerances.PreferredPressure - currentPressureToleranceRange, 0) / 1000),
                "kPa");
        pressureMaxLabel.Text =
            unitFormat.FormatSafe(
                Math.Round((CurrentTolerances.PreferredPressure + currentPressureToleranceRange) / 1000), "kPa");

        if (Math.Abs(patchPressure - CurrentTolerances.PreferredPressure) > currentPressureToleranceRange)
        {
            if (patchPressure > CurrentTolerances.PreferredPressure)
            {
                pressureMaxLabel.LabelSettings = badValueFont;
                pressureMinLabel.LabelSettings = originalPressureFont;
            }
            else
            {
                pressureMinLabel.LabelSettings = badValueFont;
                pressureMaxLabel.LabelSettings = originalPressureFont;
            }
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
