namespace Tutorial;

using System;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Tells the player about the environment panel and the day and night cycle
/// </summary>
public class DayNightTutorial : SwimmingAroundCountingTutorial
{
    public const ushort SERIALIZATION_VERSION = 1;

    public HUDBottomBar? HUDBottomBar { get; set; }

    public EnvironmentPanel? EnvironmentPanel { get; set; }

    public override string ClosedByName => "DayNightTutorial";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialDayNightTutorial;

    protected override int TriggersOnNthSwimmingSession => 3;

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

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base version is not our version, so we pass 1 here
        base.ReadPropertiesFromArchive(reader, 1);
    }

    protected override void OnProcess(TutorialState overallState, float delta)
    {
        if (Time > Constants.HIDE_MICROBE_DAY_NIGHT_TUTORIAL_AFTER)
        {
            Hide();
        }
    }
}
