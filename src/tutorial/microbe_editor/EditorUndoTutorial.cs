namespace Tutorial;

using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Ensures the player knows about the undo button
/// </summary>
public class EditorUndoTutorial : TutorialPhase
{
    public override string ClosedByName => "CellEditorUndo";

    [JsonIgnore]
    public Control? EditorUndoButtonControl { get; set; }

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

            case TutorialEventType.MicrobeEditorUndo:
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
}
