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
                            overallState.NegativeAtpBalanceTutorial.CanTrigger = true;

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
