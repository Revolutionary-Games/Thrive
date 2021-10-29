namespace Tutorial
{
    using System;
    using Godot;
    using Newtonsoft.Json;

    public class AutoEvoPrediction : TutorialPhase
    {
        private const int TriggersOnNthEditorSession = 2;

        private readonly string cellEditorTab = MicrobeEditorGUI.EditorTab.CellEditor.ToString();

        public AutoEvoPrediction()
        {
            CanTrigger = false;
        }

        public override string ClosedByName { get; } = "AutoEvoPrediction";

        [JsonIgnore]
        public Control EditorAutoEvoPredictionPanel { get; set; }

        [JsonProperty]
        public int NumberOfEditorEntries { get; set; }

        public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
        {
            gui.AutoEvoPredictionVisible = ShownCurrently;

            gui.AutoEvoPredictionHighlight.TargetControl = ShownCurrently ? EditorAutoEvoPredictionPanel : null;
            gui.AutoEvoPredictionHighlight.Visible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.EnteredMicrobeEditor:
                {
                    if (!HasBeenShown)
                    {
                        ++NumberOfEditorEntries;
                        CanTrigger = NumberOfEditorEntries >= TriggersOnNthEditorSession;
                    }

                    break;
                }

                case TutorialEventType.MicrobeEditorTabChanged:
                {
                    if (!HasBeenShown && CanTrigger && ((StringEventArgs)args).Data == cellEditorTab &&
                        !overallState.TutorialActive())
                    {
                        Show();
                    }

                    break;
                }
            }

            return false;
        }
    }
}
