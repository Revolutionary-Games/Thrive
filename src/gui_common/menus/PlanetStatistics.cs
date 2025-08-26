using System;
using System.Globalization;
using Godot;

/// <summary>
///   Displays various statistics about the currently generated planet.
/// </summary>
public partial class PlanetStatistics : VBoxContainer
{
#pragma warning disable CA2213
    [Export]
    private Label numberOfPatchesLabel = null!;

    [Export]
    private Label numberOfRegionsLabel = null!;

    // Environment
    [Export]
    private Label temperatureLevelLabel = null!;

    [Export]
    private Label lightLevelLabel = null!;

    [Export]
    private Label carbonOxideLevelLabel = null!;

    [Export]
    private Label oxygenLevelLabel = null!;

    [Export]
    private Label nitrogenLevelLabel = null!;

    [Export]
    private Label otherGasesLevelLabel = null!;

    // Compounds
    [Export]
    private Label sulfideLevelLabel = null!;

    [Export]
    private Label ammoniaLevelLabel = null!;

    [Export]
    private Label glucoseLevelLabel = null!;

    [Export]
    private Label phosphatesLevelLabel = null!;

    [Export]
    private Label ironLevelLabel = null!;

    [Export]
    private Label radiationLevelLabel = null!;
#pragma warning restore CA2213

    public void UpdateStatistics(PatchMap map)
    {
        var patchesCount = map.Patches.Count;
        var regionsCount = map.Regions.Count;
        var surfacePatches = 0;

        var temperatureLevel = 0.0;
        var lightLevel = 0.0;

        var carbonLevel = 0.0;
        var oxygenLevel = 0.0;
        var nitrogenLevel = 0.0;
        var otherGasesLevel = 0.0;

        var sulfideLevel = 0.0;
        var ammoniaLevel = 0.0;
        var glucoseLevel = 0.0;
        var phosphatesLevel = 0.0;
        var ironLevel = 0.0;
        var radiationLevel = 0.0;

        foreach (var patch in map.Patches.Values)
        {
            sulfideLevel += patch.GetCompoundAmountForDisplay(Compound.Hydrogensulfide);
            ammoniaLevel += patch.GetCompoundAmountForDisplay(Compound.Ammonia);
            glucoseLevel += patch.GetCompoundAmountForDisplay(Compound.Glucose);
            phosphatesLevel += patch.GetCompoundAmountForDisplay(Compound.Phosphates);
            ironLevel += patch.GetCompoundAmountForDisplay(Compound.Iron);
            radiationLevel += patch.GetCompoundAmountForDisplay(Compound.Radiation);

            if (patch.IsSurfacePatch())
            {
                carbonLevel += patch.GetCompoundAmountForDisplay(Compound.Carbondioxide);
                oxygenLevel += patch.GetCompoundAmountForDisplay(Compound.Oxygen);
                nitrogenLevel += patch.GetCompoundAmountForDisplay(Compound.Nitrogen);

                temperatureLevel += patch.GetCompoundAmountForDisplay(Compound.Temperature) / 100;
                lightLevel += patch.GetCompoundAmountForDisplay(Compound.Sunlight);
                surfacePatches += 1;
            }
        }

        temperatureLevel /= surfacePatches;
        lightLevel /= surfacePatches;

        carbonLevel /= surfacePatches;
        oxygenLevel /= surfacePatches;
        nitrogenLevel /= surfacePatches;
        otherGasesLevel += 100.0f - carbonLevel - oxygenLevel - nitrogenLevel;

        sulfideLevel /= patchesCount;
        ammoniaLevel /= patchesCount;
        glucoseLevel /= patchesCount;
        phosphatesLevel /= patchesCount;
        ironLevel /= patchesCount;
        radiationLevel /= patchesCount;

        var percentageFormat = Localization.Translate("PERCENTAGE_VALUE");
        var unitFormat = Localization.Translate("VALUE_WITH_UNIT");

        sulfideLevelLabel.Text =
            Math.Round(sulfideLevel, Constants.COMPOUND_DISPLAY_DECIMALS)
                .ToString(CultureInfo.CurrentCulture);
        ammoniaLevelLabel.Text =
            Math.Round(ammoniaLevel, Constants.COMPOUND_DISPLAY_DECIMALS)
                .ToString(CultureInfo.CurrentCulture);
        glucoseLevelLabel.Text =
            Math.Round(glucoseLevel, Constants.COMPOUND_DISPLAY_DECIMALS)
                .ToString(CultureInfo.CurrentCulture);
        phosphatesLevelLabel.Text =
            Math.Round(phosphatesLevel, Constants.COMPOUND_DISPLAY_DECIMALS)
                .ToString(CultureInfo.CurrentCulture);
        ironLevelLabel.Text =
            Math.Round(ironLevel, Constants.COMPOUND_DISPLAY_DECIMALS)
                .ToString(CultureInfo.CurrentCulture);
        radiationLevelLabel.Text =
            Math.Round(radiationLevel, Constants.DETAILED_COMPOUND_DISPLAY_DECIMALS)
                .ToString(CultureInfo.CurrentCulture);

        var temperature = SimulationParameters.Instance.GetCompoundDefinition(Compound.Temperature);
        temperatureLevelLabel.Text =
            unitFormat.FormatSafe(Math.Round(temperatureLevel)
                .ToString(CultureInfo.CurrentCulture), temperature.Unit);
        lightLevelLabel.Text =
            unitFormat.FormatSafe(Math.Round(lightLevel)
                .ToString(CultureInfo.CurrentCulture), "lx");

        carbonOxideLevelLabel.Text =
            percentageFormat.FormatSafe(Math.Round(carbonLevel, Constants.ATMOSPHERIC_COMPOUND_DISPLAY_DECIMALS));
        oxygenLevelLabel.Text =
            percentageFormat.FormatSafe(Math.Round(oxygenLevel, Constants.ATMOSPHERIC_COMPOUND_DISPLAY_DECIMALS));
        nitrogenLevelLabel.Text =
            percentageFormat.FormatSafe(Math.Round(nitrogenLevel, Constants.ATMOSPHERIC_COMPOUND_DISPLAY_DECIMALS));
        otherGasesLevelLabel.Text =
            percentageFormat.FormatSafe(Math.Round(otherGasesLevel, Constants.ATMOSPHERIC_COMPOUND_DISPLAY_DECIMALS));

        numberOfPatchesLabel.Text = patchesCount.ToString(CultureInfo.CurrentCulture);
        numberOfRegionsLabel.Text = regionsCount.ToString(CultureInfo.CurrentCulture);
    }
}
