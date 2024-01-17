namespace Tutorial
{
    using System;
    using Godot;
    using Newtonsoft.Json;

    /// <summary>
    ///   Microbe movement tutorial with key prompts around the cell
    /// </summary>
    public class MicrobeMovement : TutorialPhase
    {
        [JsonProperty]
        private float keyPromptRotation;

        [JsonProperty]
        private float moveForwardTime;

        [JsonProperty]
        private float moveLeftTime;

        [JsonProperty]
        private float moveRightTime;

        [JsonProperty]
        private float moveBackwardsTime;

        private bool showFixedOrientation;

        public override string ClosedByName => "MicrobeMovementExplain";

        public override void ApplyGUIState(MicrobeTutorialGUI gui)
        {
            gui.MicrobeMovementRotation = keyPromptRotation;
            gui.MicrobeMovementPromptsVisible = ShownCurrently;

            if (ShownCurrently)
            {
                gui.MicrobeMovementPromptForwardVisible = moveForwardTime <
                    Constants.MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME;

                gui.MicrobeMovementPromptLeftVisible = moveLeftTime <
                    Constants.MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME;

                gui.MicrobeMovementPromptRightVisible = moveRightTime <
                    Constants.MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME;

                gui.MicrobeMovementPromptBackwardsVisible = moveBackwardsTime <
                    Constants.MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME;
            }
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.MicrobePlayerOrientation:
                {
                    bool consumed = false;

                    if (!HasBeenShown && overallState.MicrobeStageWelcome.Complete && CanTrigger)
                    {
                        Show();

                        moveForwardTime = 0;
                        moveLeftTime = 0;
                        moveRightTime = 0;
                        moveBackwardsTime = 0;

                        consumed = true;
                    }

                    if (ShownCurrently)
                    {
                        if (showFixedOrientation)
                        {
                            keyPromptRotation = 0;
                        }
                        else
                        {
                            var rotationDegrees = -((RotationEventArgs)args).RotationInDegrees.y;
                            var lerped = Mathf.LerpAngle(Mathf.Deg2Rad(keyPromptRotation),
                                Mathf.Deg2Rad(rotationDegrees), 0.1f);
                            keyPromptRotation = Mathf.Rad2Deg(lerped);
                        }

                        consumed = true;
                    }

                    if (consumed)
                        return true;

                    break;
                }

                case TutorialEventType.MicrobePlayerMovement:
                {
                    // We want this info when we haven't been shown yet (or are currently showing)
                    // As this event type can arrive before MicrobePlayerOrientation
                    if (!HasBeenShown || ShownCurrently)
                    {
                        var wantedState = ((MicrobeMovementEventArgs)args).UsesScreenRelativeMovement;

                        if (showFixedOrientation != wantedState)
                        {
                            showFixedOrientation = wantedState;

                            if (showFixedOrientation)
                                keyPromptRotation = 0;
                        }
                    }

                    break;
                }
            }

            return false;
        }

        protected override void OnProcess(TutorialState overallState, float delta)
        {
            // This does not use the input system because when OnProcess is called in a tutorial is complicated, so
            // when this code is triggered would be different using the input system
            if (Input.IsActionPressed("g_move_forward"))
            {
                moveForwardTime += delta;
            }

            if (Input.IsActionPressed("g_move_left"))
            {
                moveLeftTime += delta;
            }

            if (Input.IsActionPressed("g_move_right"))
            {
                moveRightTime += delta;
            }

            if (Input.IsActionPressed("g_move_backwards"))
            {
                moveBackwardsTime += delta;
            }

            // Check if all keys have been pressed, and if so close the tutorial
            if (moveForwardTime >= Constants.MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME &&
                moveLeftTime >= Constants.MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME &&
                moveRightTime >= Constants.MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME &&
                moveBackwardsTime >= Constants.MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME)
            {
                Hide();
                overallState.MicrobeMovementExplanation.Hide();
                return;
            }

            // Open explanation window if the player hasn't used all the movement keys within a certain time
            if (!overallState.MicrobeMovementExplanation.HasBeenShown &&
                overallState.MicrobeMovementExplanation.CanTrigger)
            {
                // When using controller input the text explanation is triggered much faster to show some extra info
                // about the controls
                if (Time > Constants.MICROBE_MOVEMENT_EXPLAIN_TUTORIAL_DELAY ||
                    (KeyPromptHelper.InputMethod != ActiveInputMethod.Keyboard &&
                        Time > Constants.MICROBE_MOVEMENT_EXPLAIN_TUTORIAL_DELAY_CONTROLLER))
                {
                    overallState.MicrobeMovementExplanation.Show();
                }
            }
        }
    }
}
