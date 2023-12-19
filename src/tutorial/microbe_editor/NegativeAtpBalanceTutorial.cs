namespace Tutorial
{
    using System;

    /// <summary>
    ///   Tells the player about negative ATP balance (but only after the ATP introduction tutorial has triggered)
    /// </summary>
    public class NegativeAtpBalanceTutorial : TutorialPhase
    {
        public NegativeAtpBalanceTutorial()
        {
            CanTrigger = false;
        }

        public override string ClosedByName => "NegativeAtpBalanceTutorial";

        public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
        {
            gui.NegativeAtpBalanceTutorialVisible = ShownCurrently;
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
                            return true;
                        }
                    }

                    break;
                }
            }

            return false;
        }
    }
}
