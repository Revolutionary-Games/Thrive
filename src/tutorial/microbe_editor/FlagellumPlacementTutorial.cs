namespace Tutorial
{
    using System;

    public class FlagellumPlacementTutorial : TutorialPhase
    {
        public override string ClosedByName => "FlagellumPlacementTutorial";

        public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
        {
            gui.FlagellumPlacementTutorialVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState,
            TutorialEventType eventType, EventArgs args, object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.MicrobeFlagellumPlaced:
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
