namespace Tutorial
{
    using System;
    using Godot;
    using Newtonsoft.Json;

    /// <summary>
    ///   Tells the player about the ATP balance bar
    /// </summary>
    public class AtpBalanceIntroduction : TutorialPhase
    {
        private bool shouldEnableNegativeATPTutorial;

        public override string ClosedByName => nameof(AtpBalanceIntroduction);

        [JsonIgnore]
        public Control? ATPBalanceBarControl { get; set; }

        public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
        {
            if (gui.AtpBalanceBarHighlight == null)
                throw new InvalidOperationException($"{nameof(gui.AtpBalanceBarHighlight)} has not been set");

            gui.AtpBalanceBarHighlight.TargetControl = ATPBalanceBarControl;

            gui.AtpBalanceIntroductionVisible = ShownCurrently;
            gui.HandleShowingATPBarHighlight();
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.MicrobeEditorPlayerEnergyBalanceChanged:
                {
                    if (args is EnergyBalanceEventArgs energyBalanceEventArgs)
                    {
                        var energyBalanceInfo = energyBalanceEventArgs.EnergyBalanceInfo;
                        bool isNegativeAtpBalance =
                            energyBalanceInfo.TotalProduction < energyBalanceInfo.TotalConsumption;

                        if (!HasBeenShown && isNegativeAtpBalance && CanTrigger && !overallState.TutorialActive())
                        {
                            Show();
                            shouldEnableNegativeATPTutorial = true;

                            return true;
                        }
                    }

                    break;
                }

                case TutorialEventType.EnteredMicrobeEditor:
                {
                    if (shouldEnableNegativeATPTutorial)
                    {
                        overallState.NegativeAtpBalanceTutorial.CanTrigger = true;
                        HandlesEvents = false;
                        shouldEnableNegativeATPTutorial = false;
                    }

                    break;
                }
            }

            return false;
        }

        public override void Hide()
        {
            base.Hide();

            // This is done to ensure the next tutorial will be enabled
            HandlesEvents = true;
        }
    }
}
