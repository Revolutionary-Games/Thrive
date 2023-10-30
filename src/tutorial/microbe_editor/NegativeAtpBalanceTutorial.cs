﻿namespace Tutorial
{
    using System;

    /// <summary>
    ///   Tells the player about negative ATP balance
    /// </summary>
    public class NegativeAtpBalanceTutorial : TutorialPhase
    {
        public override string ClosedByName => "NegativeAtpBalanceTutorial";

        public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
        {
            gui.NegativeAtpBalanceTutorialVisible = ShownCurrently;
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
