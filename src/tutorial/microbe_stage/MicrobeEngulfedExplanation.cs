namespace Tutorial
{
    using System;

    public class MicrobeEngulfedExplanation : TutorialPhase
    {
        public override string ClosedByName { get; } = "MicrobeEngulfedExplanation";

        public override void ApplyGUIState(MicrobeTutorialGUI gui)
        {
            gui.EngulfedExplanationVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.MicrobePlayerIsEngulfed:
                {
                    if (!HasBeenShown && !overallState.TutorialActive())
                        Show();

                    break;
                }
            }

            return false;
        }
    }
}
