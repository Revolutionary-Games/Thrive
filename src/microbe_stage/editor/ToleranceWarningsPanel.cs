using System;
using Godot;

/// <summary>
///   Panel that displays warnings related to the organism's tolerance levels.
/// </summary>
public partial class ToleranceWarningsPanel : PanelContainer
{
#pragma warning disable CA2213
    [Export]
    public Label OsmoregulationCostValue = null!;

    [Export]
    public Label BioprocessSpeedValue = null!;

    [Export]
    public Label HealthValue = null!;

    [Export]
    public HBoxContainer OsmoregulationCostContainer = null!;

    [Export]
    public HBoxContainer BioprocessSpeedContainer = null!;

    [Export]
    public HBoxContainer HealthContainer = null!;

    [Export]
    public Label TemperatureConditionLabel = null!;

    [Export]
    public Label PressureConditionLabel = null!;

    [Export]
    public Label OxygenConditionLabel = null!;

    [Export]
    public Label UVConditionLabel = null!;

    [Export]
    public HBoxContainer TemperatureConditionContainer = null!;

    [Export]
    public HBoxContainer PressureConditionContainer = null!;

    [Export]
    public HBoxContainer OxygenConditionContainer = null!;

    [Export]
    public HBoxContainer UVConditionContainer = null!;
#pragma warning restore CA2213

    public void GenerateToleranceProblemList(ToleranceResult data, in ResolvedMicrobeTolerances problemNumbers,
        out bool areToleranceWarnings)
    {
        areToleranceWarnings = false;

        if (problemNumbers.OsmoregulationModifier > 1)
        {
            OsmoregulationCostContainer.Visible = true;
            OsmoregulationCostValue.Text = "+" +
                Localization.Translate("PERCENTAGE_VALUE")
                    .FormatSafe($"{(problemNumbers.OsmoregulationModifier - 1) * 100:0.#}");
        }
        else
        {
            OsmoregulationCostContainer.Visible = false;
        }

        if (problemNumbers.ProcessSpeedModifier < 1)
        {
            BioprocessSpeedContainer.Visible = true;
            BioprocessSpeedValue.Text = "-" +
                Localization.Translate("PERCENTAGE_VALUE")
                    .FormatSafe($"{Math.Round((1 - problemNumbers.ProcessSpeedModifier) * 100, 1)}");
        }
        else
        {
            BioprocessSpeedContainer.Visible = false;
        }

        if (problemNumbers.HealthModifier < 1)
        {
            HealthContainer.Visible = true;
            HealthValue.Text = "-" +
                Localization.Translate("PERCENTAGE_VALUE")
                    .FormatSafe($"{Math.Round((1 - problemNumbers.HealthModifier) * 100, 1)}");
        }
        else
        {
            HealthContainer.Visible = false;
        }

        if (data.TemperatureScore < 1)
        {
            TemperatureConditionContainer.Visible = true;
            areToleranceWarnings = true;

            if (data.PerfectTemperatureAdjustment < 0)
            {
                var text = Localization.Translate("TOLERANCES_TOO_HIGH_TEMPERATURE")
                        .FormatSafe(Math.Round(-data.MinimumTemperatureAdjustment, 1))
                    + Localization.Translate("TEMPERATURE_UNIT_CELSIUS");
                TemperatureConditionLabel.Text = text;
            }
            else
            {
                var text = Localization.Translate("TOLERANCES_TOO_LOW_TEMPERATURE")
                        .FormatSafe(Math.Round(data.MinimumTemperatureAdjustment, 1))
                    + Localization.Translate("TEMPERATURE_UNIT_CELSIUS");
                TemperatureConditionLabel.Text = text;
            }
        }
        else
        {
            TemperatureConditionContainer.Visible = false;
        }

        if (data.PressureScore < 1)
        {
            PressureConditionContainer.Visible = true;
            areToleranceWarnings = true;

            if (data.PerfectPressureAdjustment < 0)
            {
                var text = Localization.Translate("TOLERANCES_TOO_HIGH_PRESSURE")
                        .FormatSafe(Math.Round(-data.MinimumPressureAdjustment / 1000, 1))
                    + Localization.Translate("PRESSURE_UNIT_KPA");
                PressureConditionLabel.Text = text;
            }
            else
            {
                var text = Localization.Translate("TOLERANCES_TOO_LOW_PRESSURE")
                        .FormatSafe(Math.Round(data.MinimumPressureAdjustment / 1000, 1))
                    + Localization.Translate("PRESSURE_UNIT_KPA");
                PressureConditionLabel.Text = text;
            }
        }
        else
        {
            PressureConditionContainer.Visible = false;
        }

        if (data.OxygenScore < 1)
        {
            OxygenConditionContainer.Visible = true;
            areToleranceWarnings = true;

            var text = Localization.Translate("TOLERANCES_TOO_LOW_OXYGEN_PROTECTION")
                .FormatSafe(Math.Round(data.PerfectOxygenAdjustment * 100, 1));
            OxygenConditionLabel.Text = text;
        }
        else
        {
            OxygenConditionContainer.Visible = false;
        }

        if (data.UVScore < 1)
        {
            UVConditionContainer.Visible = true;
            areToleranceWarnings = true;

            var text = Localization.Translate("TOLERANCES_TOO_LOW_UV_PROTECTION")
                .FormatSafe(Math.Round(data.PerfectUVAdjustment * 100, 1));
            UVConditionLabel.Text = text;
        }
        else
        {
            UVConditionContainer.Visible = false;
        }
    }
}
