namespace Tutorial
{
    using System;

    /// <summary>
    ///   Tells the player about the "Become multicellular" button
    /// </summary>
    public class BecomeMulticellularTutorial : TutorialPhase
    {
        public override string ClosedByName => "BecomeMulticellularTutorial";

        public override void ApplyGUIState(MicrobeTutorialGUI gui)
        {
            gui.BecomeMulticellularTutorialVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.MicrobeBecomeMulticellularAvailable:
                {
                    if (!Complete && !ProcessWhileHidden && CanTrigger)
                    {
                        ProcessWhileHidden = true;
                        Time = 0;
                    }

                    break;
                }

                // Make sure the tutorial doesn't trigger after it is no longer relevant
                case TutorialEventType.EnteredEarlyMulticellularStage:
                case TutorialEventType.EnteredEarlyMulticellularEditor:
                {
                    Inhibit();
                    break;
                }
            }

            return false;
        }

        protected override void OnProcess(TutorialState overallState, float delta)
        {
            if (Time > Constants.OPEN_MICROBE_BECOME_MULTICELLULAR_TUTORIAL_AFTER && !HasBeenShown &&
                !overallState.TutorialActive())
            {
                Show();
            }
        }
    }
}
