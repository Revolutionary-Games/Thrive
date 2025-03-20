namespace Tutorial;

using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Tells the player about the day and night cycle
/// </summary>
public class DayNightTutorial : TutorialPhase
{
    [JsonIgnore]
    public HUDBottomBar? HUDBottomBar { get; set; }

    [JsonIgnore]
    public EnvironmentPanel? EnvironmentPanel { get; set; }

    public override string ClosedByName => "DayNightTutorial";

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.DayNightTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobePlayerEnterSunlightPatch:
            {
                if (!HasBeenShown && CanTrigger && !overallState.TutorialActive())
                {
                    if (HUDBottomBar != null && EnvironmentPanel != null)
                    {
                        EnvironmentPanel.ShowPanel = true;
                        HUDBottomBar.EnvironmentPressed = true;
                    }
                    else
                    {
                        GD.PrintErr("Missing GUI panels in day/night tutorial");
                    }

                    Show();
                    return true;
                }

                break;
            }
        }

        return false;
    }

    protected override void OnProcess(TutorialState overallState, float delta)
    {
        if (Time > Constants.HIDE_MICROBE_DAY_NIGHT_TUTORIAL_AFTER)
        {
            Hide();
        }
    }
}
