namespace Tutorial
{
    using System;
    using Godot;
    using Newtonsoft.Json;

    /// <summary>
    ///   Redo tutorial in the cell editor
    /// </summary>
    public class MadeNoChangesTutorial : TutorialPhase
    {
        public override string ClosedByName => "MadeNoChangesTutorial";

        public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
        {
            gui.MadeNoChangesTutorialVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.MicrobeEditorNoChangesMade:
                {
                    if (!HasBeenShown && CanTrigger && !overallState.TutorialActive())
                    {
                        Show();
                        return true;
                    }

                    break;
                }
            }

            return false;
        }
    }
}
