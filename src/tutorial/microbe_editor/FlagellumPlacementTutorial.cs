namespace Tutorial
{
    using System;

    /// <summary>
    ///   Informs the player where to place flagella and how they work
    /// </summary>
    public class FlagellumPlacementTutorial : TutorialPhase
    {
        private readonly OrganelleDefinition flagellum = SimulationParameters.Instance.GetOrganelleType("flagellum");

        public override string ClosedByName => "FlagellumPlacementTutorial";

        public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
        {
            gui.FlagellumPlacementTutorialVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState,
            TutorialEventType eventType, EventArgs args, object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.MicrobeEditorOrganellePlaced:
                {
                    if (args is OrganellePlacedEventArgs organellePlacedArgs)
                    {
                        if (organellePlacedArgs.Definition.InternalName != flagellum.InternalName)
                            break;

                        if (!HasBeenShown && CanTrigger && !overallState.TutorialActive())
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
