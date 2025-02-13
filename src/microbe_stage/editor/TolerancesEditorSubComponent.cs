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

            CalculateToleranceRangeForGUI();
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
        CurrentTolerances.CopyFrom(Editor.EditedBaseSpecies.Tolerances);

        // Send it to GUI
        CalculateToleranceRangeForGUI();
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
            CurrentTolerances.PressureToleranceMax - CurrentTolerances.PressureToleranceMin;
    }

    private void ApplyCurrentValuesToGUI()
    {
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

        // TODO: would there be a way to avoid this clone? Might need some reworking of the general actions system
        var action = new ToleranceActionData(CurrentTolerances.Clone(), reusableTolerances);

        // And try to apply it. The actual value change will come from the do action callback.
        if (!Editor.EnqueueAction(new SingleEditorAction<ToleranceActionData>(DoToleranceChangeAction,
                UndoToleranceChangeAction,
                action)))
        {
            // Rollback value if not enough MP
            temperatureSlider.Value = CurrentTolerances.PreferredTemperature;
        }
        else
        {
            // This is now eaten up by the action
            reusableTolerances = null;
        }
    }

    private void OnTemperatureToleranceRangeSliderChanged(float value)
    {
        if (automaticallyChanging)
            return;
    }

    private void OnPressureSliderChanged(float value)
    {
        if (automaticallyChanging)
            return;
    }

    private void OnPressureToleranceRangeSliderChanged(float value)
    {
        if (automaticallyChanging)
            return;
    }

    private void OnOxygenResistanceSliderChanged(float value)
    {
        if (automaticallyChanging)
            return;
    }

    private void OnUVResistanceSliderChanged(float value)
    {
        if (automaticallyChanging)
            return;
    }

    /// <summary>
    ///   Applies all new values and notifies the data has changed
    /// </summary>
    private void OnChanged()
    {
        EmitSignal(SignalName.OnTolerancesChanged);
        UpdateCurrentValueDisplays();
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
            temperatureMinLabel.LabelSettings = badValueFont;
            temperatureMaxLabel.LabelSettings = badValueFont;
        }
        else
        {
            temperatureMinLabel.LabelSettings = originalTemperatureFont;
            temperatureMaxLabel.LabelSettings = originalTemperatureFont;
        }

        pressureMinLabel.Text =
            unitFormat.FormatSafe(
                Math.Round((CurrentTolerances.PreferredPressure - currentPressureToleranceRange) / 1000), "kPa");
        pressureMaxLabel.Text =
            unitFormat.FormatSafe(
                Math.Round((CurrentTolerances.PreferredPressure + currentPressureToleranceRange) / 1000), "kPa");

        if (Math.Abs(patchPressure - CurrentTolerances.PreferredPressure) > currentPressureToleranceRange)
        {
            pressureMinLabel.LabelSettings = badValueFont;
            pressureMaxLabel.LabelSettings = badValueFont;
        }
        else
        {
            pressureMinLabel.LabelSettings = originalPressureFont;
            pressureMaxLabel.LabelSettings = originalPressureFont;
        }

        oxygenResistanceLabel.Text = percentageFormat.FormatSafe(CurrentTolerances.OxygenResistance);

        if (CurrentTolerances.OxygenResistance < requiredOxygenResistance)
        {
            oxygenResistanceLabel.LabelSettings = badValueFont;
        }
        else
        {
            oxygenResistanceLabel.LabelSettings = originalTemperatureFont;
        }

        uvResistanceLabel.Text = percentageFormat.FormatSafe(CurrentTolerances.UVResistance);

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
        OnChanged();
    }

    [DeserializedCallbackAllowed]
    private void UndoToleranceChangeAction(ToleranceActionData data)
    {
        CurrentTolerances.CopyFrom(data.OldTolerances);
        OnChanged();
    }
}
