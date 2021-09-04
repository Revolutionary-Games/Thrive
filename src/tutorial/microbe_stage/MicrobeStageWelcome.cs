namespace Tutorial
{
    using System;

    /// <summary>
    ///   A welcome popup to the stage
    /// </summary>
    public class MicrobeStageWelcome : TutorialPhase
    {
        private Action patchNamePopup;

        public MicrobeStageWelcome()
        {
            Exclusive = true;
            Pauses = true;
        }

        public override string ClosedByName { get; } = "MicrobeStageWelcome";

        public override void ApplyGUIState(MicrobeTutorialGUI gui)
        {
            gui.MicrobeWelcomeVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.EnteredMicrobeStage:
                {
                    patchNamePopup = ((CallbackEventArgs)args)?.Data;

                    if (!HasBeenShown && CanTrigger)
                    {
                        Show();
                        return true;
                    }

                    break;
                }
            }

            return false;
        }

        public override void Hide()
        {
            patchNamePopup?.Invoke();
            base.Hide();
        }
    }
}
