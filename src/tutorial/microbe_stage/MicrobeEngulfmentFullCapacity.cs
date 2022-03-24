namespace Tutorial
{
    using System;
    using Godot;

    public class EngulfmentFullCapacity : TutorialPhase
    {
        public override string ClosedByName { get; } = "EngulfmentFullCapacity";

        public override void ApplyGUIState(MicrobeTutorialGUI gui)
        {
            gui.EngulfmentFullCapacityVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.MicrobePlayerEngulfmentFull:
                {
                    if (!HasBeenShown)
                        Show();

                    break;
                }
            }

            return false;
        }
    }
}
