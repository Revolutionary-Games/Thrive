namespace Tutorial;

using System;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Explains the specialization mechanic of the microbe editor
/// </summary>
public class MicrobeSpecializationTutorial : EditorEntryCountingTutorial
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly string cellEditorTab = nameof(EditorTab.CellEditor);

    public ScrollContainer? SpecializationContainer { get; set; }

    public Control? SpecializationDisplayControl { get; set; }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMicrobeSpecializationTutorial;

    public override string ClosedByName => "MicrobeSpecializationTutorial";

    protected override int TriggersOnNthEditorSession => 8;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        gui.SpecializationTutorialVisible = ShownCurrently;

        if (gui.SpecializationTutorialVisible && SpecializationContainer != null &&
            SpecializationDisplayControl != null)
        {
            // Make sure it is visible at the bottom of the scroll
            var containerHeight = SpecializationContainer.GetRect().Size.Y;

            var controlTop = SpecializationDisplayControl.Position.Y;
            var controlHeight = SpecializationDisplayControl.GetRect().Size.Y;
            var controlBottom = controlTop + controlHeight;

            var desiredScroll = controlBottom - containerHeight;

            SpecializationContainer.ScrollVertical =
                (int)Math.Clamp(desiredScroll, 0, SpecializationContainer.GetVScrollBar().MaxValue);
        }
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        if (base.CheckEvent(overallState, eventType, args, sender))
            return false;

        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorOrganellePlaced:
            {
                if (args is not OrganellePlacedEventArgs)
                {
                    GD.PrintErr("Organelle placed event has wrong argument type, this will break many tutorials!");
                    break;
                }

                if (ShownCurrently)
                {
                    Hide();
                }

                break;
            }

            case TutorialEventType.MicrobeEditorTabChanged:
            {
                var tab = ((StringEventArgs)args).Data;

                if (tab == cellEditorTab && CanTrigger)
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
