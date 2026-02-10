namespace Tutorial;

using System;
using SharedBase.Archive;

/// <summary>
///   Welcome message and intro to the report tab
/// </summary>
public class EditorReportWelcome : EditorEntryCountingTutorial
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly string reportTab = nameof(EditorTab.Report);
    private readonly string foodChainTab = nameof(MicrobeEditorReportComponent.ReportSubtab.FoodChain);

    public override string ClosedByName => "MicrobeEditorReport";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialEditorReportWelcome;

    /// <summary>
    ///   On the first time in the editor we go directly to the cell editor tab, so this tutorial triggers on the
    ///   second go of the editor
    /// </summary>
    protected override int TriggersOnNthEditorSession => 2;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.EditorEntryReportVisible = ShownCurrently;
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
