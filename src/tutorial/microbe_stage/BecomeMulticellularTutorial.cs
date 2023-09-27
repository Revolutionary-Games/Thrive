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
