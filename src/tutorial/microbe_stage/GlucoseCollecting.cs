namespace Tutorial
{
    using System;
    using Godot;
    using Newtonsoft.Json;

    /// <summary>
    ///   Tutorial pointing glucose collection out to the player
    /// </summary>
    public class GlucoseCollecting : TutorialPhase
    {
        [JsonProperty]
        private Vector3? glucosePosition;

        public GlucoseCollecting()
        {
            UsesPlayerPositionGuidance = true;
        }

        public override string ClosedByName { get; } = "GlucoseCollecting";

        public override void ApplyGUIState(TutorialGUI gui)
        {
            gui.GlucoseTutorialVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.MicrobeCompoundsNearPlayer:
                {
                    var data = (CompoundPositionEventArgs)args;

                    if (!HasBeenShown && data.GlucosePosition.HasValue)
                    {
                        glucosePosition = data.GlucosePosition.Value;
                        Show();
                        return true;
                    }

                    break;
                }
            }

            return false;
        }

        public override Vector3 GetPositionGuidance()
        {
            if (glucosePosition != null)
                return glucosePosition.Value;

            throw new InvalidOperationException("glucose tutorial doesn't have position set");
        }
    }
}
