namespace Tutorial;

using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Redo tutorial in the cell editor
/// </summary>
public class EditorRedoTutorial : TutorialPhase
{
    public override string ClosedByName => "CellEditorRedo";

    [JsonIgnore]
    public Control? EditorRedoButtonControl { get; set; }

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
            case TutorialEventType.MicrobeEditorUndo:
            {
                if (!HasBeenShown && CanTrigger)
                {
                    Show();
                    overallState.EditorTutorialEnd.CanTrigger = true;

                    return true;
                }

                break;
            }

            case TutorialEventType.MicrobeEditorRedo:
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
