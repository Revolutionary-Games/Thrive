using System;
using Godot;

/// <summary>
///   Handles showing tolerance adaptation controls (sliders) and applying their changes
/// </summary>
public partial class TolerancesEditor : VBoxContainer
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

    private float currentTemperature;
    private float currentTemperatureToleranceRange;
    private float currentPressure;
    private float currentPressureToleranceRange;
    private float currentOxygenResistance;
    private float currentUVResistance;

    /// <summary>
    ///   Editor where we apply our changes
    /// </summary>
    private ICellEditorData? editor;

    [Signal]
    public delegate void OnTolerancesChangedEventHandler();

    public override void _Ready()
    {
        originalTemperatureFont = temperatureMinLabel.LabelSettings;
        originalPressureFont = pressureMinLabel.LabelSettings;
    }

    public void Init(ICellEditorData owningEditor, bool fresh)
    {
        editor = owningEditor;

        if (!fresh)
        {
            // Grab the latest applied tolerance edit, if any

            // If no edits exist, then fallback to the default values
        }

        // Load data for the species being edited
        var speciesTolerance = editor.EditedBaseSpecies.Tolerances;

        currentTemperature = speciesTolerance.PreferredTemperature;
        currentTemperatureToleranceRange = speciesTolerance.TemperatureTolerance;
        currentPressure = speciesTolerance.PreferredPressure;

        // TODO: maybe the GUI should have separate sliders as well?
        currentPressureToleranceRange = speciesTolerance.PressureToleranceMax - speciesTolerance.PressureToleranceMin;

        currentOxygenResistance = speciesTolerance.OxygenResistance;
        currentUVResistance = speciesTolerance.UVResistance;

        ApplyCurrentValuesToGUI();
    }

    public void OnPatchChanged()
    {
        UpdateCurrentValueDisplays();
    }

    private void ApplyCurrentValuesToGUI()
    {
        automaticallyChanging = true;

        temperatureSlider.Value = currentTemperature;
        temperatureToleranceRangeSlider.Value = currentTemperatureToleranceRange;
        pressureSlider.Value = currentPressure;
        pressureToleranceRangeSlider.Value = currentPressureToleranceRange;
        oxygenResistanceSlider.Value = currentOxygenResistance;
        uvResistanceSlider.Value = currentUVResistance;

        automaticallyChanging = false;

        UpdateCurrentValueDisplays();
    }

    private void OnTemperatureSliderChanged(float value)
    {
        // Ignore changes triggered by our code to avoid infinite loops
        if (automaticallyChanging)
            return;

        // Create change action for the new value

        // And try to apply it

        // Rollback if not enough MP
        if (false)
        {
            temperatureSlider.Value = currentTemperature;
        }
        else
        {
            OnChanged();
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
        if (editor == null)
            return;

        var patch = editor.CurrentPatch;
        var patchTemperature = patch.Biome.GetCompound(Compound.Temperature, CompoundAmountType.Biome).Ambient;
        var patchPressure = patch.Biome.Pressure;
        var requiredOxygenResistance = patch.Biome.CalculateOxygenResistanceFactor();
        var requiredUVResistance = patch.Biome.CalculateUVFactor();

        var unitFormat = Localization.Translate("VALUE_WITH_UNIT");
        var percentageFormat = Localization.Translate("PERCENTAGE_VALUE");

        // TODO: green for perfectly being adapted?

        temperatureMinLabel.Text =
            unitFormat.FormatSafe(Math.Round(currentTemperature - currentTemperatureToleranceRange, 1),
                temperature.Unit);
        temperatureMaxLabel.Text =
            unitFormat.FormatSafe(Math.Round(currentTemperature + currentTemperatureToleranceRange, 1),
                temperature.Unit);

        // Show in red the conditions that are not matching to make them easier to notice
        if (Math.Abs(patchTemperature - currentTemperature) > currentTemperatureToleranceRange)
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
            unitFormat.FormatSafe(Math.Round((currentPressure - currentPressureToleranceRange) / 1000), "kPa");
        pressureMaxLabel.Text =
            unitFormat.FormatSafe(Math.Round((currentPressure + currentPressureToleranceRange) / 1000), "kPa");

        if (Math.Abs(patchPressure - currentPressure) > currentPressureToleranceRange)
        {
            pressureMinLabel.LabelSettings = badValueFont;
            pressureMaxLabel.LabelSettings = badValueFont;
        }
        else
        {
            pressureMinLabel.LabelSettings = originalPressureFont;
            pressureMaxLabel.LabelSettings = originalPressureFont;
        }

        oxygenResistanceLabel.Text = percentageFormat.FormatSafe(currentOxygenResistance);

        if (currentOxygenResistance < requiredOxygenResistance)
        {
            oxygenResistanceLabel.LabelSettings = badValueFont;
        }
        else
        {
            oxygenResistanceLabel.LabelSettings = originalTemperatureFont;
        }

        uvResistanceLabel.Text = percentageFormat.FormatSafe(currentUVResistance);

        if (currentUVResistance < requiredUVResistance)
        {
            uvResistanceLabel.LabelSettings = badValueFont;
        }
        else
        {
            uvResistanceLabel.LabelSettings = originalTemperatureFont;
        }
    }
}
