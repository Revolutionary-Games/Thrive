namespace Tutorial;

using System;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Ensures the player knows about the undo button
/// </summary>
public class EditorUndoTutorial : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    public override string ClosedByName => "CellEditorUndo";

    public Control? EditorUndoButtonControl { get; set; }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialEditorUndoTutorial;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        if (gui.CellEditorUndoHighlight == null)
            throw new InvalidOperationException($"{nameof(gui.CellEditorUndoHighlight)} has not been set");

        gui.CellEditorUndoHighlight.TargetControl = ShownCurrently ? EditorUndoButtonControl : null;

        gui.CellEditorUndoVisible = ShownCurrently;
        gui.CellEditorUndoHighlight.Visible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.MicrobeEditorOrganellePlaced:
            {
                if (!HasBeenShown && CanTrigger && !overallState.TutorialActive())
                {
                    Show();
                    return true;
                }

                break;
            }

            case TutorialEventType.EditorUndo:
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
