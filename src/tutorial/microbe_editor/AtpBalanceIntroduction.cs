namespace Tutorial;

using System;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Tells the player about the ATP balance bar functionality (must trigger before the negative ATP balance
///   tutorial will work)
/// </summary>
public class AtpBalanceIntroduction : CellEditorEntryCountingTutorial
{
    public const ushort SERIALIZATION_VERSION = 1;

    private bool shouldEnableNegativeATPTutorial;

    public override string ClosedByName => nameof(AtpBalanceIntroduction);

    public Control? ATPBalanceBarControl { get; set; }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialAtpBalanceIntroduction;

    protected override int TriggersOnNthEditorSession => 2;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        if (gui.AtpBalanceBarHighlight == null)
            throw new InvalidOperationException($"{nameof(gui.AtpBalanceBarHighlight)} has not been set");

        gui.AtpBalanceBarHighlight.TargetControl = ATPBalanceBarControl;

        gui.AtpBalanceIntroductionVisible = ShownCurrently;
        gui.HandleShowingATPBarHighlight();
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return true;

        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorPlayerEnergyBalanceChanged:
            {
                // This event is fine enough for detecting when the player changes something to highlight the
                // ATP balance bar, could be changed in the future to use organelle placement

                if (!HasBeenShown && CanTrigger && !overallState.TutorialActive())
                {
                    Show();
                    shouldEnableNegativeATPTutorial = true;

                    return true;
                }

                break;
            }

            case TutorialEventType.EnteredMicrobeEditor:
            {
                if (shouldEnableNegativeATPTutorial)
                {
                    overallState.NegativeAtpBalanceTutorial.CanTrigger = true;
                    shouldEnableNegativeATPTutorial = false;
                    HandlesEvents = false;
                }

                break;
            }
        }

        return false;
    }

    public override void Hide()
    {
        base.Hide();

        // This needs to be done so that this keeps getting the microbe enter events and can make the next
        // tutorial trigger
        HandlesEvents = true;
    }

    public override void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        base.WritePropertiesToArchive(writer);

        writer.Write(shouldEnableNegativeATPTutorial);
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base version is not our version, so we pass 1 here
        base.ReadPropertiesFromArchive(reader, 1);

        shouldEnableNegativeATPTutorial = reader.ReadBool();
    }
}
