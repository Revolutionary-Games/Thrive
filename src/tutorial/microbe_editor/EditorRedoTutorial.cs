namespace Tutorial
{
    using System;

    /// <summary>
    ///   Redo tutorial in the cell editor
    /// </summary>
    public class EditorRedoTutorial : TutorialPhase
    {
        public override string ClosedByName { get; } = "CellEditorRedo";

        public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
        {
            gui.CellEditorRedoVisible = ShownCurrently;
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
}
