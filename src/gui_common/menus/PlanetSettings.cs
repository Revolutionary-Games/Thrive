﻿using System;
using System.Globalization;
using Godot;
using Xoshiro.PRNG64;

/// <summary>
///   Planet settings in the planet customizer.
/// </summary>
public partial class PlanetSettings : VBoxContainer
{
#pragma warning disable CA2213
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
    private OptionButton sulfideLevelButton = null!;

    [Export]
    private OptionButton glucoseLevelButton = null!;

    [Export]
    private OptionButton ironLevelButton = null!;

    [Export]
    private OptionButton ammoniaLevelButton = null!;

    [Export]
    private OptionButton phosphatesLevelButton = null!;

    [Export]
    private OptionButton radiationLevelButton = null!;

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

    [Export]
    private LineEdit gameSeed = null!;
#pragma warning restore CA2213

    private long latestValidSeed;

    private bool isUpdatingCurrentTabsSeed;

    [Signal]
    public delegate void LawkSettingsChangedEventHandler(bool lawkOnly);

    [Signal]
    public delegate void SeedSettingsChangedEventHandler(string seed);

    [Signal]
    public delegate void LifeOriginSettingsChangedEventHandler(WorldGenerationSettings.LifeOrigin lifeOrigin);

    [Signal]
    public delegate void PlanetSettingChangedEventHandler();

    public WorldGenerationSettings GetPlanetSettings()
    {
        return new WorldGenerationSettings
        {
            WorldSize = (WorldGenerationSettings.WorldSizeEnum)worldSizeButton.Selected,
            WorldTemperature = (WorldGenerationSettings.WorldTemperatureEnum)worldTemperatureButton.Selected,
            WorldOceanicCoverage = (WorldGenerationSettings.WorldOceanicCoverageEnum)worldSeaLevelButton.Selected,
            GeologicalActivity = (WorldGenerationSettings.GeologicalActivityEnum)worldGeologicalActivityButton.Selected,
            ClimateInstability = (WorldGenerationSettings.ClimateInstabilityEnum)worldClimateInstabilityButton.Selected,
            HydrogenSulfideLevel = (WorldGenerationSettings.CompoundLevel)sulfideLevelButton.Selected,
            GlucoseLevel = (WorldGenerationSettings.CompoundLevel)glucoseLevelButton.Selected,
            IronLevel = (WorldGenerationSettings.CompoundLevel)ironLevelButton.Selected,
            AmmoniaLevel = (WorldGenerationSettings.CompoundLevel)ammoniaLevelButton.Selected,
            PhosphatesLevel = (WorldGenerationSettings.CompoundLevel)phosphatesLevelButton.Selected,
            RadiationLevel = (WorldGenerationSettings.CompoundLevel)radiationLevelButton.Selected,
            Origin = (WorldGenerationSettings.LifeOrigin)lifeOriginButton.Selected,
            DayNightCycleEnabled = dayNightCycleButton.ButtonPressed,
            DayLength = (int)dayLength.Value,
            LAWK = lawkOnlyButton.ButtonPressed,
            Seed = latestValidSeed,
        };
    }

    public void SetLifeOrigin(WorldGenerationSettings.LifeOrigin value)
    {
        lifeOriginButton.Selected = (int)value;
    }

    public void SetWorldSize(WorldGenerationSettings.WorldSizeEnum value)
    {
        worldSizeButton.Selected = (int)value;
    }

    public void SetWorldTemperature(WorldGenerationSettings.WorldTemperatureEnum value)
    {
        worldTemperatureButton.Selected = (int)value;
    }

    public void SetOceanicCoverage(WorldGenerationSettings.WorldOceanicCoverageEnum value)
    {
        worldSeaLevelButton.Selected = (int)value;
    }

    public void SetWorldGeologicalActivity(WorldGenerationSettings.GeologicalActivityEnum value)
    {
        worldGeologicalActivityButton.Selected = (int)value;
    }

    public void SetWorldClimateInstability(WorldGenerationSettings.ClimateInstabilityEnum value)
    {
        worldClimateInstabilityButton.Selected = (int)value;
    }

    public void SetHydrogenSulfideLevel(WorldGenerationSettings.CompoundLevel value)
    {
        sulfideLevelButton.Selected = (int)value;
    }

    public void SetGlucoseLevel(WorldGenerationSettings.CompoundLevel value)
    {
        glucoseLevelButton.Selected = (int)value;
    }

    public void SetIronLevel(WorldGenerationSettings.CompoundLevel value)
    {
        ironLevelButton.Selected = (int)value;
    }

    public void SetAmmoniaLevel(WorldGenerationSettings.CompoundLevel value)
    {
        ammoniaLevelButton.Selected = (int)value;
    }

    public void SetPhosphatesLevel(WorldGenerationSettings.CompoundLevel value)
    {
        phosphatesLevelButton.Selected = (int)value;
    }

    public void SetRadiationLevel(WorldGenerationSettings.CompoundLevel value)
    {
        radiationLevelButton.Selected = (int)value;
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

    public void ReportValidityOfGameSeed(bool valid)
    {
        if (valid)
        {
            GUICommon.MarkInputAsValid(gameSeed);
        }
        else
        {
            GUICommon.MarkInputAsInvalid(gameSeed);
        }
    }

    public void GenerateAndSetRandomSeed()
    {
        var seed = GenerateNewRandomSeed();
        SetSeed(seed);
    }

    public void SetSeed(string text)
    {
        EmitSignal(SignalName.SeedSettingsChanged, text);

        var valid = long.TryParse(text, out var seed) && seed > 0;

        // Don't update the text when editing, otherwise the caret will go to the beginning
        if (!isUpdatingCurrentTabsSeed)
            gameSeed.Text = text;
        isUpdatingCurrentTabsSeed = false;

        ReportValidityOfGameSeed(valid);
        if (valid)
        {
            latestValidSeed = seed;
            EmitSignal(SignalName.PlanetSettingChanged);
        }
    }

    private void OnDayNightCycleToggled(bool pressed)
    {
        dayLengthContainer.Modulate = pressed ? Colors.White : new Color(1.0f, 1.0f, 1.0f, 0.5f);
        dayLength.Editable = pressed;
        EmitSignal(SignalName.PlanetSettingChanged);
    }

    private void OnDayLengthChanged(double length)
    {
        length = Math.Round(length, 1);
        dayLengthReadout.Text = length.ToString(CultureInfo.CurrentCulture);
        EmitSignal(SignalName.PlanetSettingChanged);
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

    private void OnLifeOriginSelected(WorldGenerationSettings.LifeOrigin lifeOrigin)
    {
        EmitSignal(SignalName.LifeOriginSettingsChanged, Variant.From(lifeOrigin));
    }

    private void OnSeedChanged(string seed)
    {
        isUpdatingCurrentTabsSeed = true;
        SetSeed(seed);
    }

    private void OnGenerateSeedPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        GenerateAndSetRandomSeed();
    }

    private void OnLAWKToggled(bool pressed)
    {
        UpdateLifeOriginOptions(pressed);
        EmitSignal(SignalName.LawkSettingsChanged, pressed);
    }

    private void OnPlanetSettingsChanged(int index)
    {
        _ = index;
        EmitSignal(SignalName.PlanetSettingChanged);
    }

    private string GenerateNewRandomSeed()
    {
        var random = new XoShiRo256starstar();

        string result;

        // Generate seeds until valid (0 is not considered valid)
        do
        {
            result = random.Next64().ToString();
        }
        while (result == "0");

        return result;
    }
}
