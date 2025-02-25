namespace Tutorial;

using System;

/// <summary>
///   Tells the player about the day and night cycle
/// </summary>
public class DayNightTutorial : TutorialPhase
{
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
                MicrobeHUD hud = ((HUDEventArgs)args).HUD;

                if (!HasBeenShown && CanTrigger && !overallState.TutorialActive())
                {
                    hud.ShowEnvironmentPanel();
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
