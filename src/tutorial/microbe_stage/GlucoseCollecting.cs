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
        private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");

        [JsonProperty]
        private Vector3? glucosePosition;

        public GlucoseCollecting()
        {
            UsesPlayerPositionGuidance = true;
            CanTrigger = false;
        }

        public override string ClosedByName { get; } = "GlucoseCollecting";

        public override void ApplyGUIState(MicrobeTutorialGUI gui)
        {
            gui.GlucoseTutorialVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.MicrobePlayerCompounds:
                {
                    var compounds = ((CompoundBagEventArgs)args).Compounds;

                    if (!HasBeenShown && !CanTrigger &&
                        compounds.GetCompoundAmount(glucose) < compounds.BagCapacity -
                        Constants.GLUCOSE_TUTORIAL_TRIGGER_ENABLE_FREE_STORAGE_SPACE)
                    {
                        CanTrigger = true;
                        return true;
                    }

                    break;
                }

                case TutorialEventType.MicrobeCompoundsNearPlayer:
                {
                    var data = (CompoundPositionEventArgs)args;

                    if (!HasBeenShown && data.GlucosePosition.HasValue && CanTrigger && !overallState.TutorialActive())
                    {
                        Show();
                    }

                    if (data.GlucosePosition.HasValue && ShownCurrently)
                    {
                        glucosePosition = data.GlucosePosition.Value;
                        return true;
                    }

                    break;
                }

                case TutorialEventType.MicrobePlayerTotalCollected:
                {
                    if (!ShownCurrently)
                        break;

                    var compounds = ((CompoundEventArgs)args).Compounds;

                    if (compounds.ContainsKey(glucose) &&
                        compounds[glucose] >= Constants.GLUCOSE_TUTORIAL_COLLECT_BEFORE_COMPLETE)
                    {
                        // Tutorial is now complete
                        Hide();
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
