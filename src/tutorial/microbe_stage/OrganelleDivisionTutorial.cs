namespace Tutorial
{
    using System;

    /// <summary>
    ///   Tells the player about the day and night cycle
    /// </summary>
    public class OrganelleDivisionTutorial : TutorialPhase
    {
        public override string ClosedByName => "OrganelleDivisionTutorial";

        public override void ApplyGUIState(MicrobeTutorialGUI gui)
        {
            gui.OrganelleDivisionTutorialVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.MicrobeNonCytoplasmOrganelleDivided:
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
