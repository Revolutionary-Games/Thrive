namespace Tutorial;

using System;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Redo tutorial in the cell editor
/// </summary>
public class EditorRedoTutorial : TutorialPhase
{
    public const ushort SERIALIZATION_VERSION = 1;

    public override string ClosedByName => "CellEditorRedo";

    public Control? EditorRedoButtonControl { get; set; }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialEditorRedoTutorial;

    public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        if (gui.CellEditorRedoHighlight == null)
            throw new InvalidOperationException($"{nameof(gui.CellEditorRedoHighlight)} has not been set");

        gui.CellEditorRedoHighlight.TargetControl = ShownCurrently ? EditorRedoButtonControl : null;

        gui.CellEditorRedoVisible = ShownCurrently;
        gui.CellEditorRedoHighlight.Visible = ShownCurrently;
    }

    public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender)
    {
        switch (eventType)
        {
            case TutorialEventType.EditorUndo:
            {
                if (!HasBeenShown && CanTrigger)
                {
                    Show();
                    overallState.EditorTutorialEnd.CanTrigger = true;

                    return true;
                }

                break;
            }

            case TutorialEventType.EditorRedo:
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
