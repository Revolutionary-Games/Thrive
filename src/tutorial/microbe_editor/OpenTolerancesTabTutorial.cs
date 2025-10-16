namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Tutorial about opening the tolerances-tab. In case the player hasn't viewed it.
/// </summary>
public class OpenTolerancesTabTutorial : CellEditorEntryCountingTutorial
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly string tolerancesTab = nameof(CellEditorComponent.SelectionMenuTab.Tolerance);

    public override string ClosedByName => "OpenTolerancesTabTutorial";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialOpenTolerancesTabTutorial;

    protected override int TriggersOnNthEditorSession => 6;

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return true;

        switch (eventType)
        {
            case TutorialEventType.CellEditorTabChanged:
            {
                // Make this tutorial not trigger once the player has opened the tolerances-tab, as this is just about
                // reminding to open it
                if ((!HasBeenShown || ShownCurrently) && ((StringEventArgs)args).Data == tolerancesTab)
                {
                    CanTrigger = false;
                    HasBeenShown = true;
                    Hide();
                }

                break;
            }
        }

        return false;
    }

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.OpenTolerancesTabTutorialVisible = ShownCurrently;
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base version is not our version, so we pass 1 here
        base.ReadPropertiesFromArchive(reader, 1);
    }
}
