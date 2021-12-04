namespace Tutorial
{
    using System;

    /// <summary>
    ///   Tells the player how to reproduce if they have taken a long while
    /// </summary>
    public class MicrobeReproduction : TutorialPhase
    {
        public override string ClosedByName { get; } = "MicrobeReproduction";

        public override void ApplyGUIState(MicrobeTutorialGUI gui)
        {
            gui.ReproductionTutorialVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.MicrobePlayerSpawned:
                {
                    if (!Complete && !ProcessWhileHidden)
                    {
                        ProcessWhileHidden = true;
                        Time = 0;
                    }

                    break;
                }

                case TutorialEventType.MicrobePlayerReadyToEdit:
                    Inhibit();
                    break;
                case TutorialEventType.EnteredMicrobeEditor:
                    Inhibit();
                    break;
            }

            return false;
        }

        public override void Hide()
        {
            base.Hide();
            ProcessWhileHidden = false;
        }

        protected override void OnProcess(TutorialState overallState, float delta)
        {
            if (Time > Constants.MICROBE_REPRODUCTION_TUTORIAL_DELAY && !HasBeenShown && !overallState.TutorialActive())
            {
                Show();
            }
        }
    }
}
