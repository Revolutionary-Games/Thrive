namespace Tutorial
{
    using System;

    /// <summary>
    ///   When full on reproduction compounds the player needs to realize to leave a non-multicellular colony to
    ///   progress
    /// </summary>
    public class LeaveColonyTutorial : TutorialPhase
    {
        private readonly Compound ammonia = SimulationParameters.Instance.GetCompound("ammonia");
        private readonly Compound phosphates = SimulationParameters.Instance.GetCompound("phosphates");

        private bool hasColony;
        private bool fullAmmonia;
        private bool fullPhosphates;

        public override string ClosedByName => "LeaveColonyTutorial";

        public override void ApplyGUIState(MicrobeTutorialGUI gui)
        {
            gui.LeaveColonyTutorialVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.MicrobePlayerColony:
                {
                    if (!HasBeenShown)
                    {
                        hasColony = ((MicrobeColonyEventArgs)args).HasColony;
                    }

                    break;
                }

                case TutorialEventType.MicrobePlayerCompounds:
                {
                    if (!HasBeenShown && hasColony && CanTrigger)
                    {
                        var compounds = ((CompoundBagEventArgs)args).Compounds;

                        fullAmmonia = compounds.GetCapacityForCompound(ammonia) - compounds.GetCompoundAmount(ammonia) <
                            MathUtils.EPSILON;

                        fullPhosphates = compounds.GetCapacityForCompound(phosphates) -
                            compounds.GetCompoundAmount(phosphates) <
                            MathUtils.EPSILON;

                        if (fullAmmonia && fullPhosphates && !overallState.TutorialActive())
                            Show();
                    }

                    break;
                }

                case TutorialEventType.EnteredMicrobeEditor:
                {
                    if (ShownCurrently)
                        Inhibit();
                    break;
                }
            }

            return false;
        }
    }
}
