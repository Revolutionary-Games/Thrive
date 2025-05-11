using System;
using System.Globalization;
using Godot;

/// <summary>
///   Planet settings in the planet customizer.
/// </summary>
public partial class PlanetSettings : VBoxContainer
{
    [Export]
    private OptionButton lifeOriginButton = null!;

    [Export]
    private OptionButton worldSizeButton = null!;

    [Export]
    private OptionButton worldTemperatureButton = null!;

    [Export]
    private OptionButton worldSeaLevelButton = null!;

    [Export]
    private OptionButton worldGeologicalActivityButton = null!;

    [Export]
    private OptionButton worldClimateInstabilityButton = null!;

    [Export]
    private CheckButton lawkOnlyButton = null!;

    [Export]
    private CheckButton dayNightCycleButton = null!;

    [Export]
    private HSlider dayLength = null!;

    [Export]
    private LineEdit dayLengthReadout = null!;

    [Export]
    private VBoxContainer dayLengthContainer = null!;

    [Signal]
    public delegate void LawkSettingsChangedEventHandler(bool lawkOnly);

    [Signal]
    public delegate void LifeOriginSettingsChangedEventHandler(int lifeOrigin);

    public WorldGenerationSettings GetPlanetSettings()
    {
        return new WorldGenerationSettings
        {
            WorldSize = (WorldGenerationSettings.WorldSizeEnum)worldSizeButton.Selected,
            WorldTemperature = (WorldGenerationSettings.WorldTemperatureEnum)worldTemperatureButton.Selected,
            WorldSeaLevel = (WorldGenerationSettings.WorldSeaLevelEnum)worldSeaLevelButton.Selected,
            GeologicalActivity = (WorldGenerationSettings.GeologicalActivityEnum)worldGeologicalActivityButton.Selected,
            ClimateInstability = (WorldGenerationSettings.ClimateInstabilityEnum)worldClimateInstabilityButton.Selected,
            Origin = (WorldGenerationSettings.LifeOrigin)lifeOriginButton.Selected,
            DayNightCycleEnabled = dayNightCycleButton.ButtonPressed,
            DayLength = (int)dayLength.Value,
            LAWK = lawkOnlyButton.ButtonPressed,
        };
    }
    
    public void SetLifeOrigin(int index)
    {
        lifeOriginButton.Selected = index;
    }

    public void SetWorldSize(int index)
    {
        worldSizeButton.Selected = index;
    }

    public void SetWorldTemperature(int index)
    {
        worldTemperatureButton.Selected = index;
    }

    public void SetWorldSeaLevel(int index)
    {
        worldSeaLevelButton.Selected = index;
    }

    public void SetWorldGeologicalActivity(int index)
    {
        worldGeologicalActivityButton.Selected = index;
    }

    public void SetWorldClimateInstability(int index)
    {
        worldClimateInstabilityButton.Selected = index;
    }

    public void SetLawkOnly(bool pressed)
    {
        lawkOnlyButton.ButtonPressed = pressed;
    }

    public void SetDayNightCycle(bool enabled)
    {
        dayNightCycleButton.ButtonPressed = enabled;
    }

    public void SetDayLength(float value)
    {
        dayLength.Value = value;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lifeOriginButton.Dispose();
            worldSizeButton.Dispose();
            worldTemperatureButton.Dispose();
            worldSeaLevelButton.Dispose();
            worldGeologicalActivityButton.Dispose();
            worldClimateInstabilityButton.Dispose();
            lawkOnlyButton.Dispose();
            dayNightCycleButton.Dispose();
            dayLength.Dispose();
            dayLengthReadout.Dispose();
            dayLengthContainer.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnDayNightCycleToggled(bool pressed)
    {
        dayLengthContainer.Modulate = pressed ? Colors.White : new Color(1.0f, 1.0f, 1.0f, 0.5f);
        dayLength.Editable = pressed;
    }

    private void OnDayLengthChanged(double length)
    {
        length = Math.Round(length, 1);
        dayLengthReadout.Text = length.ToString(CultureInfo.CurrentCulture);
    }

    private void UpdateLifeOriginOptions(bool lawk)
    {
        // If we've switched to LAWK only, disable panspermia
        var panspermiaIndex = (int)WorldGenerationSettings.LifeOrigin.Panspermia;
        lifeOriginButton.SetItemDisabled(panspermiaIndex, lawk);

        // If we had selected panspermia, reset to vents
        if (lawk && lifeOriginButton.Selected == panspermiaIndex)
        {
            lifeOriginButton.Selected = (int)WorldGenerationSettings.LifeOrigin.Vent;
        }
    }

    private void OnLifeOriginSelected(int index)
    {
        EmitSignal(SignalName.LifeOriginSettingsChanged, index);
    }

    private void OnLAWKToggled(bool pressed)
    {
        EmitSignal(SignalName.LawkSettingsChanged, pressed);
        UpdateLifeOriginOptions(pressed);
    }
}
