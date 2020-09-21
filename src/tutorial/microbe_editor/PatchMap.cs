namespace Tutorial
{
    using System;

    /// <summary>
    ///   Tutorial for the patch map tab
    /// </summary>
    public class PatchMap : TutorialPhase
    {
        public override string ClosedByName { get; } = "PatchMap";

        public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
        {
            gui.PatchMapVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.MicrobeEditorTabChanged:
                {
                    string tab = ((StringEventArgs)args).Data;

                    if (!HasBeenShown && CanTrigger && tab == "patch")
                    {
                        Show();
                    }

                    if (ShownCurrently && tab == "editor")
                    {
                        Hide();
                    }

                    break;
                }

                case TutorialEventType.MicrobeEditorPatchSelected:
                {
                    if (ShownCurrently && ((PatchEventArgs)args).Patch != null)
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
