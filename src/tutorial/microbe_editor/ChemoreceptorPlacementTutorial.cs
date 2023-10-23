namespace Tutorial
{
    /// <summary>
    ///   Notifies the player about the chemoreceptor
    /// </summary>
    public class ChemoreceptorPlacementTutorial : EditorEntryCountingTutorial
    {
        public ChemoreceptorPlacementTutorial()
        {
            CanTrigger = false;
        }

        public override string ClosedByName => "ChemoreceptorPlacementTutorial";

        protected override int TriggersOnNthEditorSession => 3;

        public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
        {
            gui.ChemoreceptorPlacementTutorialVisible = ShownCurrently;
        }
    }
}
