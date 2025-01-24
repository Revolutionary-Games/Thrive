using System;
using Godot;

/// <summary>
///   The microbe editor's light configuration top panel
/// </summary>
public partial class LightConfigurationPanel : PanelContainer
{
#pragma warning disable CA2213

    [Export]
    private Button dayButton = null!;

    [Export]
    private Button nightButton = null!;

    [Export]
    private Button averageLightButton = null!;

    [Export]
    private Button currentLightButton = null!;

#pragma warning disable CA2213

    [Signal]
    public delegate void OnLightButtonClickEventHandler(string type);

    public enum LightLevelOption
    {
        Day,
        Night,
        Average,
        Current,
    }

    public void OnLightButtonClicked(string type)
    {
        EmitSignal(SignalName.OnLightButtonClick, type);
    }

    public void ApplyLightLevelSelection(LightLevelOption lightLevelOption)
    {
        switch (lightLevelOption)
        {
            case LightLevelOption.Day:
            {
                dayButton.ButtonPressed = true;
                break;
            }

            case LightLevelOption.Night:
            {
                nightButton.ButtonPressed = true;
                break;
            }

            case LightLevelOption.Average:
            {
                averageLightButton.ButtonPressed = true;
                break;
            }

            case LightLevelOption.Current:
            {
                currentLightButton.ButtonPressed = true;
                break;
            }

            default:
                throw new Exception("Invalid light level option");
        }
    }
}
