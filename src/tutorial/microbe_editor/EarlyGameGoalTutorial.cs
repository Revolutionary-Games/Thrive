namespace Tutorial;

using System;
using SharedBase.Archive;

public class EarlyGameGoalTutorial : EditorEntryCountingTutorial
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly string reportTab = nameof(EditorTab.Report);
    private readonly string foodChainTab = nameof(MicrobeEditorReportComponent.ReportSubtab.FoodChain);

    public override string ClosedByName => "EarlyGameGoalTutorial";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialEarlyGameGoalTutorial;

    protected override int TriggersOnNthEditorSession => 3;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.EarlyGameGoalVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return false;

        switch (eventType)
        {
            case TutorialEventType.EnteredMicrobeEditor:
            {
                if (!HasBeenShown && CanTrigger)
                {
                    Show();
                }

                break;
            }

            case TutorialEventType.MicrobeEditorTabChanged:
            {
                var tab = ((StringEventArgs)args).Data;

                // Hide when switched to another tab
                if (tab != reportTab)
                {
                    if (ShownCurrently)
                    {
                        Hide();
                    }
                }

                break;
            }

            case TutorialEventType.ReportComponentSubtabChanged:
            {
                // Hide this when the food chain tutorial would trigger to let that proceed
                if (ShownCurrently && ((StringEventArgs)args).Data == foodChainTab)
                {
                    Hide();
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
}
