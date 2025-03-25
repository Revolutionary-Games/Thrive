namespace Tutorial;

using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Tells the player about the environment panel and the day and night cycle
/// </summary>
public class DayNightTutorial : SwimmingAroundCountingTutorial
{
    [JsonIgnore]
    public HUDBottomBar? HUDBottomBar { get; set; }

    [JsonIgnore]
    public EnvironmentPanel? EnvironmentPanel { get; set; }

    public override string ClosedByName => "DayNightTutorial";

    protected override int TriggersOnNthSwimmingSession => 4;

    public override void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        gui.DayNightTutorialVisible = ShownCurrently;
    }

    public override void Show()
    {
        if (ShownCurrently)
            return;

        base.Show();

        if (HUDBottomBar != null && EnvironmentPanel != null)
        {
            GD.Print("Showing environment panel for tutorial");
            EnvironmentPanel.ShowPanel = true;
            HUDBottomBar.EnvironmentPressed = true;
        }
        else
        {
            GD.PrintErr("Missing GUI panels in day/night tutorial");
        }
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return true;

        switch (eventType)
        {
            case TutorialEventType.MicrobePlayerEnterSunlightPatch:
            {
                if (!HasBeenShown && CanTrigger && !overallState.TutorialActive())
                {
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
