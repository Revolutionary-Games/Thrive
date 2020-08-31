using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   State of the tutorials for a game of Thrive
/// </summary>
public class TutorialState : ITutorialInput
{
    [JsonProperty]
    private bool enabled = Settings.Instance.TutorialsEnabled;

    /// <summary>
    ///   Pause state to return the game to when a tutorial popup that paused the game is closed
    /// </summary>
    [JsonProperty]
    private bool hasPaused;

    /// <summary>
    ///   Pause state to return the game to when a tutorial popup that paused the game is closed
    /// </summary>
    [JsonProperty]
    private bool returnToPauseState;

    [JsonProperty]
    private bool showMicrobeWelcome;

    [JsonProperty]
    private bool microbeWelcomeShown;

    [JsonProperty]
    private bool showMovementTutorial;

    [JsonProperty]
    private float movementTutorialRotation;

    [JsonProperty]
    private bool movementTutorialShown;

    [JsonProperty]
    private bool showMovementExplainTutorial;

    [JsonProperty]
    private bool movementExplainTutorialShown;

    [JsonProperty]
    private float microbeMovementTutorialTime;

    [JsonProperty]
    private float microbeMoveForwardTime;

    [JsonProperty]
    private float microbeMoveLeftTime;

    [JsonProperty]
    private float microbeMoveRightTime;

    [JsonProperty]
    private float microbeMoveBackwardsTime;

    private bool needsToApplyEvenIfDisabled;

    [JsonIgnore]
    public bool Enabled
    {
        get => enabled;
        set => enabled = value;
    }

    [JsonProperty]
    public float TotalElapsed { get; private set; }

    /// <summary>
    ///   True if any of the tutorials are active that want to pause the game
    /// </summary>
    [JsonIgnore]
    public bool WantsGamePaused => showMicrobeWelcome;

    /// <summary>
    ///   Handles an event that potentially changes the tutorial state
    /// </summary>
    /// <param name="eventType">Type of the event that happened</param>
    /// <param name="args">Event arguments or EventArgs.Empty</param>
    /// <param name="sender">Who sent it, some events need access to the stage</param>
    public void SendEvent(TutorialEventType eventType, EventArgs args, object sender)
    {
        _ = sender;

        // TODO: some events might actually be better to always handle
        if (!Enabled)
            return;

        switch (eventType)
        {
            case TutorialEventType.EnteredMicrobeStage:
            {
                if (!microbeWelcomeShown)
                {
                    microbeWelcomeShown = true;
                    showMicrobeWelcome = true;
                }

                break;
            }

            case TutorialEventType.MicrobePlayerOrientation:
            {
                // Show if not shown currently or before, and microbe welcome has been shown but is no longer
                if (!showMovementTutorial && !movementTutorialShown && microbeWelcomeShown && !showMicrobeWelcome)
                {
                    movementTutorialShown = true;
                    showMovementTutorial = true;
                    microbeMovementTutorialTime = 0;
                    microbeMoveForwardTime = 0;
                    microbeMoveLeftTime = 0;
                    microbeMoveRightTime = 0;
                    microbeMoveBackwardsTime = 0;
                }

                if (showMovementTutorial)
                {
                    movementTutorialRotation = -((RotationEventArgs)args).RotationInDegrees.y;
                }

                break;
            }
        }
    }

    /// <summary>
    ///   Resets all the show flags to false
    /// </summary>
    public void HideAll()
    {
        showMicrobeWelcome = false;
        showMovementTutorial = false;
        showMovementExplainTutorial = false;
    }

    /// <summary>
    ///   Checks if any tutorial is visible
    /// </summary>
    /// <returns>True if any tutorial is visible</returns>
    public bool TutorialActive()
    {
        return showMicrobeWelcome || showMovementTutorial || showMovementExplainTutorial;
    }

    /// <summary>
    ///   Checks if any exclusive tutorial is visible. When one is active it prevents all other GUI buttons from working
    /// </summary>
    /// <returns>True if any exclusive tutorial is visible</returns>
    public bool ExclusiveTutorialActive()
    {
        return showMicrobeWelcome;
    }

    public void Process(TutorialGUI gui, float delta)
    {
        if (!Enabled)
        {
            if (hasPaused)
            {
                UnPause(gui);
            }

            if (needsToApplyEvenIfDisabled)
            {
                HideAll();
                ApplyGUIState(gui);
                needsToApplyEvenIfDisabled = false;
            }

            return;
        }

        HandlePausing(gui);

        // Pause if the game is paused, but we didn't want to pause things
        if (gui.GetTree().Paused && !WantsGamePaused)
            return;

        TotalElapsed += delta;

        if (showMovementTutorial)
            HandleMicrobeMovementTutorial(delta);

        ApplyGUIState(gui);
    }

    public void OnTutorialDisabled()
    {
        Enabled = false;
        HideAll();
        needsToApplyEvenIfDisabled = true;
    }

    public void OnTutorialEnabled()
    {
        Enabled = true;
    }

    public void OnCurrentTutorialClosed(string name)
    {
        switch (name)
        {
            case "MicrobeMovementExplain":
            {
                showMovementTutorial = false;
                showMovementExplainTutorial = false;
                break;
            }

            default:
                GD.PrintErr("Unknown tutorial closed: ", name);
                HideAll();
                break;
        }
    }

    public void OnTutorialClosed()
    {
        HideAll();
        needsToApplyEvenIfDisabled = true;
    }

    public void OnNextPressed()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///   Applies all the GUI states related to the tutorial, this makes saving and loading the tutorial state easier
    /// </summary>
    /// <param name="gui">The target GUI instance</param>
    private void ApplyGUIState(TutorialGUI gui)
    {
        gui.IsClosingAutomatically = true;

        gui.MicrobeWelcomeVisible = showMicrobeWelcome;
        gui.MicrobeMovementRotation = movementTutorialRotation;
        gui.MicrobeMovementPromptsVisible = showMovementTutorial;
        gui.MicrobeMovementPopupVisible = showMovementExplainTutorial;

        if (showMovementTutorial)
        {
            gui.MicrobeMovementPromptForwardVisible = microbeMoveForwardTime <
                Constants.MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME;

            gui.MicrobeMovementPromptLeftVisible = microbeMoveLeftTime <
                Constants.MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME;

            gui.MicrobeMovementPromptRightVisible = microbeMoveRightTime <
                Constants.MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME;

            gui.MicrobeMovementPromptBackwardsVisible = microbeMoveBackwardsTime <
                Constants.MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME;
        }

        gui.IsClosingAutomatically = false;
        needsToApplyEvenIfDisabled = true;
    }

    private void HandleMicrobeMovementTutorial(float delta)
    {
        microbeMovementTutorialTime += delta;

        if (Input.IsActionPressed("g_move_forward"))
        {
            microbeMoveForwardTime += delta;
        }

        if (Input.IsActionPressed("g_move_left"))
        {
            microbeMoveLeftTime += delta;
        }

        if (Input.IsActionPressed("g_move_right"))
        {
            microbeMoveRightTime += delta;
        }

        if (Input.IsActionPressed("g_move_backwards"))
        {
            microbeMoveBackwardsTime += delta;
        }

        // Check if all keys have been pressed, and if so close the tutorial
        if (microbeMoveForwardTime >= Constants.MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME &&
            microbeMoveLeftTime >= Constants.MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME &&
            microbeMoveRightTime >= Constants.MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME &&
            microbeMoveBackwardsTime >= Constants.MICROBE_MOVEMENT_TUTORIAL_REQUIRE_DIRECTION_PRESS_TIME)
        {
            showMovementTutorial = false;
            movementExplainTutorialShown = true;
            showMovementExplainTutorial = false;
            return;
        }

        // Open explanation window if the player hasn't used all the movement keys within a certain time
        if (microbeMovementTutorialTime > Constants.MICROBE_MOVEMENT_EXPLAIN_TUTORIAL_DELAY &&
            !movementExplainTutorialShown)
        {
            showMovementExplainTutorial = true;
            movementExplainTutorialShown = true;
        }
    }

    private void HandlePausing(Node gameNode)
    {
        if (WantsGamePaused != hasPaused)
        {
            if (hasPaused)
            {
                // Unpause
                UnPause(gameNode);
            }
            else
            {
                // Due to initialization stuff, the tutorial is not allowed to immediately pause the game
                if (TotalElapsed < Constants.TIME_BEFORE_TUTORIAL_CAN_PAUSE)
                    return;

                // Pause
                returnToPauseState = gameNode.GetTree().Paused;
                gameNode.GetTree().Paused = true;
                hasPaused = true;
            }
        }
    }

    private void UnPause(Node gameNode)
    {
        gameNode.GetTree().Paused = returnToPauseState;
        hasPaused = false;
    }
}
