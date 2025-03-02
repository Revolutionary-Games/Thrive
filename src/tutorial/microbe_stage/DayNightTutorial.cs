﻿namespace Tutorial;

using System;
using Newtonsoft.Json;

/// <summary>
///   Tells the player about the day and night cycle
/// </summary>
public class DayNightTutorial : TutorialPhase
{
    [JsonProperty]
    public HUDBottomBar HUDBottomBar { get; set; } = new();

    [JsonProperty]
    public EnvironmentPanel EnvironmentPanel { get; set; } = new();

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
                    EnvironmentPanel.ShowPanel = true;
                    HUDBottomBar.EnvironmentPressed = true;
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
