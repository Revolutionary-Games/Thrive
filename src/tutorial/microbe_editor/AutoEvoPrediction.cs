namespace Tutorial;

using System;
using Godot;
using SharedBase.Archive;

public class AutoEvoPrediction : CellEditorEntryCountingTutorial
{
    public const ushort SERIALIZATION_VERSION = 1;

    public AutoEvoPrediction()
    {
        CanTrigger = false;
    }

    public override string ClosedByName => "AutoEvoPrediction";

    public Control? EditorAutoEvoPredictionPanel { get; set; }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialAutoEvoPrediction;

    protected override int TriggersOnNthEditorSession => 3;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        if (gui.AutoEvoPredictionHighlight == null)
            throw new InvalidOperationException($"{nameof(gui.AutoEvoPredictionHighlight)} has not been set");

        gui.AutoEvoPredictionVisible = ShownCurrently;

        gui.AutoEvoPredictionHighlight.TargetControl = ShownCurrently ? EditorAutoEvoPredictionPanel : null;
        gui.AutoEvoPredictionHighlight.Visible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        // Disallow showing if the control we highlight is not visible.
        // Or if the prerequisite tutorial is not complete.
        if (eventType == TutorialEventType.MicrobeEditorTabChanged && (EditorAutoEvoPredictionPanel == null ||
                !GodotObject.IsInstanceValid(EditorAutoEvoPredictionPanel) || !EditorAutoEvoPredictionPanel.Visible ||
                !overallState.EarlyGameGoalTutorial.Complete))
        {
            return false;
        }

        if (base.CheckEvent(overallState, eventType, args, sender))
            return true;

        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorAutoEvoPredictionOpened:
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

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base version is not our version, so we pass 1 here
        base.ReadPropertiesFromArchive(reader, 1);
    }
}
