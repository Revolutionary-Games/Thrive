using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   The partial class containing GUI updating actions
/// </summary>
public partial class CellBodyPlanEditorComponent
{
    protected override void OnTranslationsChanged()
    {
    }

    private void UpdateStorage(Dictionary<Compound, float> storage, float nominalStorage)
    {
        // Storage values can be as low as 0.25 so 2 decimals are needed
        storageLabel.Value = MathF.Round(nominalStorage, 2);

        if (storage.Count == 0)
        {
            storageLabel.UnRegisterFirstToolTipForControl();
            return;
        }

        var tooltip = ToolTipManager.Instance.GetToolTip("storageDetails", "editor");
        if (tooltip == null)
        {
            GD.PrintErr("Can't update storage tooltip");
            return;
        }

        if (!storageLabel.IsToolTipRegistered(tooltip))
            storageLabel.RegisterToolTipForControl(tooltip, true);

        var description = new LocalizedStringBuilder(100);

        bool first = true;

        var simulationParameters = SimulationParameters.Instance;

        foreach (var entry in storage)
        {
            if (!first)
                description.Append("\n");

            first = false;

            description.Append(simulationParameters.GetCompoundDefinition(entry.Key).Name);
            description.Append(": ");
            description.Append(entry.Value);
        }

        tooltip.Description = description.ToString();
    }

    private void UpdateSpeed(float speed)
    {
        speedLabel.Value = (float)Math.Round(MicrobeInternalCalculations.SpeedToUserReadableNumber(speed), 1);
    }

    private void UpdateRotationSpeed(float speed)
    {
        rotationSpeedLabel.Value = (float)Math.Round(
            MicrobeInternalCalculations.RotationSpeedToUserReadableNumber(speed), 1);
    }
}
