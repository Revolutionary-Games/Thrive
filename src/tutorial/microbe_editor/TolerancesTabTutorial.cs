namespace Tutorial;

using System;
using SharedBase.Archive;

public class TolerancesTabTutorial : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly string tolerancesTab = nameof(CellEditorComponent.SelectionMenuTab.Tolerance);

    public override string ClosedByName => "TolerancesTabTutorial";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialTolerancesTabTutorial;

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.CellEditorTabChanged:
            {
                if (!HasBeenShown && CanTrigger && ((StringEventArgs)args).Data == tolerancesTab &&
                    !overallState.TutorialActive())
                {
                    Show();
                }

                break;
            }

            case TutorialEventType.MicrobeEditorTolerancesModified:
            {
                if (ShownCurrently)
                {
                    Hide();
                }

                break;
            }
        }

        return false;
    }

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.TolerancesTabTutorialVisible = ShownCurrently;
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base version is not our version, so we pass 1 here
        base.ReadPropertiesFromArchive(reader, 1);
    }
}
