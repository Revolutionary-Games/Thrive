using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Tutorial;

/// <summary>
///   State of the tutorials for a game of Thrive
/// </summary>
public class TutorialState : ITutorialInput
{
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

    private bool needsToApplyEvenIfDisabled;

    private List<TutorialPhase> cachedTutorials;

    public bool Enabled { get; set; } = Settings.Instance.TutorialsEnabled;

    // Tutorial states

    [JsonProperty]
    public MicrobeStageWelcome MicrobeStageWelcome { get; private set; } = new MicrobeStageWelcome();

    [JsonProperty]
    public MicrobeMovement MicrobeMovement { get; private set; } = new MicrobeMovement();

    [JsonProperty]
    public MicrobeMovementExplanation MicrobeMovementExplanation { get; private set; } =
        new MicrobeMovementExplanation();

    [JsonProperty]
    public GlucoseCollecting GlucoseCollecting { get; private set; } = new GlucoseCollecting();

    // End of tutorial state variables

    [JsonProperty]
    public float TotalElapsed { get; private set; }

    /// <summary>
    ///   True if any of the tutorials are active that want to pause the game
    /// </summary>
    [JsonIgnore]
    public bool WantsGamePaused => Tutorials.Any(tutorial => tutorial.WantsPaused);

    [JsonIgnore]
    public IEnumerable<TutorialPhase> Tutorials
    {
        get
        {
            if (cachedTutorials != null)
                return cachedTutorials;

            cachedTutorials = BuildListOfAllTutorials();
            return cachedTutorials;
        }
    }

    /// <summary>
    ///   Handles an event that potentially changes the tutorial state
    /// </summary>
    /// <param name="eventType">Type of the event that happened</param>
    /// <param name="args">Event arguments or EventArgs.Empty</param>
    /// <param name="sender">Who sent it, some events need access to the stage</param>
    public void SendEvent(TutorialEventType eventType, EventArgs args, object sender)
    {
        // TODO: some events might actually be better to always handle
        if (!Enabled)
            return;

        foreach (var tutorial in Tutorials)
        {
            if (!tutorial.HandlesEvents)
                continue;

            if (tutorial.CheckEvent(this, eventType, args, sender))
                break;
        }
    }

    /// <summary>
    ///   Resets all the show flags to false
    /// </summary>
    public void HideAll()
    {
        foreach (var tutorial in Tutorials)
            tutorial.Hide();
    }

    /// <summary>
    ///   Checks if any tutorial is visible
    /// </summary>
    /// <returns>True if any tutorial is visible</returns>
    public bool TutorialActive()
    {
        return Tutorials.Any(tutorial => tutorial.ShownCurrently);
    }

    /// <summary>
    ///   Checks if any exclusive tutorial is visible. When one is active it prevents all other GUI buttons from working
    /// </summary>
    /// <returns>True if any exclusive tutorial is visible</returns>
    public bool ExclusiveTutorialActive()
    {
        return Tutorials.Any(tutorial => tutorial.CurrentlyExclusivelyOpen);
    }

    /// <summary>
    ///   Returns true when the tutorial system is in a state where nearby compound info is wanted
    /// </summary>
    /// <returns>True when the tutorial system wants compound information</returns>
    public bool WantsNearbyCompoundInfo()
    {
        return MicrobeMovement.Complete && !GlucoseCollecting.Complete;
    }

    /// <summary>
    ///   Position in the world to guide the player to
    /// </summary>
    /// <returns>The target position or null</returns>
    public Vector3? GetPLayerGuidancePosition()
    {
        foreach (var tutorial in Tutorials)
        {
            if (tutorial.ShownCurrently && tutorial.UsesPlayerPositionGuidance)
            {
                return tutorial.GetPositionGuidance();
            }
        }

        return null;
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

        foreach (var tutorial in Tutorials)
        {
            if (!tutorial.ShownCurrently)
                continue;

            tutorial.Process(this, delta);
        }

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
        bool somethingMatched = false;

        foreach (var tutorial in Tutorials)
        {
            if (tutorial.ClosedByName != name)
                continue;

            somethingMatched = true;

            if (tutorial.ShownCurrently)
                tutorial.Hide();
        }

        if (!somethingMatched)
        {
            GD.PrintErr("Unknown tutorial closed: ", name);
            HideAll();
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

    private List<TutorialPhase> BuildListOfAllTutorials()
    {
        return new List<TutorialPhase>
        {
            MicrobeStageWelcome,
            MicrobeMovement,
            MicrobeMovementExplanation,
            GlucoseCollecting,
        };
    }

    /// <summary>
    ///   Applies all the GUI states related to the tutorial, this makes saving and loading the tutorial state easier
    /// </summary>
    /// <param name="gui">The target GUI instance</param>
    private void ApplyGUIState(TutorialGUI gui)
    {
        gui.IsClosingAutomatically = true;

        foreach (var tutorial in Tutorials)
            tutorial.ApplyGUIState(gui);

        gui.IsClosingAutomatically = false;
        needsToApplyEvenIfDisabled = true;
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
