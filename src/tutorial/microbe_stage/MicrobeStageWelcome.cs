namespace Tutorial
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    ///   A welcome popup to the stage
    /// </summary>
    public class MicrobeStageWelcome : TutorialPhase
    {
        private Action? patchNamePopup;

        [JsonProperty]
        private WorldGenerationSettings.LifeOrigin gameLifeOrigin;

        private WorldGenerationSettings.LifeOrigin appliedGUILifeOrigin = WorldGenerationSettings.LifeOrigin.Vent;

        public MicrobeStageWelcome()
        {
            Pauses = true;
        }

        public override string ClosedByName => "MicrobeStageWelcome";

        public override void ApplyGUIState(MicrobeTutorialGUI gui)
        {
            if (ShownCurrently && gameLifeOrigin != appliedGUILifeOrigin)
            {
                gui.SetWelcomeTextForLifeOrigin(gameLifeOrigin);
                appliedGUILifeOrigin = gameLifeOrigin;
            }

            gui.MicrobeWelcomeVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.EnteredMicrobeStage:
                {
                    foreach (var eventArg in ((AggregateEventArgs)args).Args)
                    {
                        if (eventArg is CallbackEventArgs callbackEventArgs)
                        {
                            patchNamePopup = callbackEventArgs.Data;
                        }
                        else if (eventArg is GameWorldEventArgs gameWorldEventArgs)
                        {
                            gameLifeOrigin = gameWorldEventArgs.World.WorldSettings.Origin;
                        }
                    }

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
