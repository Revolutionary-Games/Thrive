namespace Tutorial;

using System;
using SharedBase.Archive;

public class FoodChainTabTutorial : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly string reportTab = nameof(EditorTab.Report);
    private readonly string foodChainTab = nameof(MicrobeEditorReportComponent.ReportSubtab.FoodChain);

    public override string ClosedByName => "FoodChainTabTutorial";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialFoodChainTabTutorial;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.FoodChainTutorialVisible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorTabChanged:
            {
                var tab = ((StringEventArgs)args).Data;

                if (ShownCurrently && tab != reportTab)
                {
                    Hide();
                }

                break;
            }

            case TutorialEventType.ReportComponentSubtabChanged:
            {
                if (!HasBeenShown && CanTrigger && ((StringEventArgs)args).Data == foodChainTab &&
                    !overallState.TutorialActive())
                {
                    Show();
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
