namespace Tutorial;

using System;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Microbe movement tutorial with key prompts around the cell
/// </summary>
#pragma warning disable CA1001 // just has some StringNames that aren't critical to Dispose
public class MicrobeMovement : TutorialPhase, IArchiveUpdatable
#pragma warning restore CA1001
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly StringName moveForward = new("g_move_forward");
    private readonly StringName moveLeft = new("g_move_left");
    private readonly StringName moveRight = new("g_move_right");
    private readonly StringName moveBackwards = new("g_move_backwards");

    private float keyPromptRotation;
    private float moveForwardTime;
    private float moveLeftTime;
    private float moveRightTime;
    private float moveBackwardsTime;
    private bool showFixedOrientation;

    public override string ClosedByName => "MicrobeMovementExplain";

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TutorialMicrobeMovement;

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
                        var rotationDegrees = -((RotationEventArgs)args).RotationInRadians.Y;
                        var lerped = Mathf.LerpAngle(keyPromptRotation, rotationDegrees, 0.14f);
                        keyPromptRotation = lerped;
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

    public override void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        base.WritePropertiesToArchive(writer);

        writer.Write(keyPromptRotation);
        writer.Write(moveForwardTime);
        writer.Write(moveLeftTime);
        writer.Write(moveRightTime);
        writer.Write(moveBackwardsTime);
        writer.Write(showFixedOrientation);
    }

    public override void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        // Base version is not our version, so we pass 1 here
        base.ReadPropertiesFromArchive(reader, 1);

        keyPromptRotation = reader.ReadFloat();
        moveForwardTime = reader.ReadFloat();
        moveLeftTime = reader.ReadFloat();
        moveRightTime = reader.ReadFloat();
        moveBackwardsTime = reader.ReadFloat();
        showFixedOrientation = reader.ReadBool();
    }

    protected override void OnProcess(TutorialState overallState, float delta)
    {
        // This does not use the input system because when OnProcess is called in a tutorial is complicated, so
        // when this code is triggered would be different using the input system
        if (Input.IsActionPressed(moveForward))
        {
            moveForwardTime += delta;
        }

        if (Input.IsActionPressed(moveLeft))
        {
            moveLeftTime += delta;
        }

        if (Input.IsActionPressed(moveRight))
        {
            moveRightTime += delta;
        }

        if (Input.IsActionPressed(moveBackwards))
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
