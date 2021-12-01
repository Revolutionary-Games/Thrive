namespace Tutorial
{
    using Godot;
    using Newtonsoft.Json;

    public class AutoEvoPrediction : EditorEntryCountingTutorial
    {
        public AutoEvoPrediction()
        {
            CanTrigger = false;
        }

        public override string ClosedByName => "AutoEvoPrediction";

        [JsonIgnore]
        public Control EditorAutoEvoPredictionPanel { get; set; }

        protected override int TriggersOnNthEditorSession => 2;

        public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
        {
            gui.AutoEvoPredictionVisible = ShownCurrently;

            gui.AutoEvoPredictionHighlight.TargetControl = ShownCurrently ? EditorAutoEvoPredictionPanel : null;
            gui.AutoEvoPredictionHighlight.Visible = ShownCurrently;
        }
    }
}
