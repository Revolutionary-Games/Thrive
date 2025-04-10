﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Tutorial;

/// <summary>
///   State of the tutorials for a game of Thrive
/// </summary>
[JsonObject(IsReference = true)]
public class TutorialState : ITutorialInput, ISaveLoadable
{
    /// <summary>
    ///   True when the tutorial has paused the game
    /// </summary>
    [JsonProperty]
    private bool hasPaused;

    private bool needsToApplyEvenIfDisabled;

    private List<TutorialPhase>? cachedTutorials;

    private bool previousTutorialActiveState;
    private double timeSinceActiveCheck;

    public bool Enabled { get; set; } = Settings.Instance.TutorialsEnabled;

    /// <summary>
    ///   When this is true, tutorials that have already been seen by the player in any playthrough are automatically
    ///   marked as already complete.
    /// </summary>
    public bool DisableShowingAlreadySeenTutorials { get; private set; }

    // Tutorial states

    [JsonProperty]
    public MicrobeStageWelcome MicrobeStageWelcome { get; private set; } = new();

    [JsonProperty]
    public MicrobeMovement MicrobeMovement { get; private set; } = new();

    [JsonProperty]
    public MicrobeMovementExplanation MicrobeMovementExplanation { get; private set; } = new();

    [JsonProperty]
    public GlucoseCollecting GlucoseCollecting { get; private set; } = new();

    [JsonProperty]
    public MicrobeStayingAlive MicrobeStayingAlive { get; private set; } = new();

    [JsonProperty]
    public MicrobeReproduction MicrobeReproduction { get; private set; } = new();

    [JsonProperty]
    public MicrobePressEditorButton MicrobePressEditorButton { get; private set; } = new();

    [JsonProperty]
    public MicrobeUnbind MicrobeUnbind { get; private set; } = new();

    [JsonProperty]
    public MicrobeEngulfmentExplanation MicrobeEngulfmentExplanation { get; private set; } = new();

    [JsonProperty]
    public MicrobeEngulfedExplanation MicrobeEngulfedExplanation { get; private set; } = new();

    [JsonProperty]
    public CheckTheHelpMenu CheckTheHelpMenu { get; private set; } = new();

    [JsonProperty]
    public MicrobeEngulfmentStorageFull EngulfmentStorageFull { get; private set; } = new();

    [JsonProperty]
    public OpenProcessPanelTutorial OpenProcessPanelTutorial { get; private set; } = new();

    [JsonProperty]
    public ProcessPanelTutorial ProcessPanelTutorial { get; private set; } = new();

    [JsonProperty]
    public ResourcesAfterSplitTutorial ResourcesAfterSplitTutorial { get; private set; } = new();

    [JsonProperty]
    public MigrationTutorial MigrationTutorial { get; private set; } = new();

    [JsonProperty]
    public EditorReportWelcome EditorReportWelcome { get; private set; } = new();

    [JsonProperty]
    public Tutorial.PatchMap PatchMap { get; private set; } = new();

    [JsonProperty]
    public CellEditorIntroduction CellEditorIntroduction { get; private set; } = new();

    [JsonProperty]
    public EditorUndoTutorial EditorUndoTutorial { get; private set; } = new();

    [JsonProperty]
    public EditorRedoTutorial EditorRedoTutorial { get; private set; } = new();

    [JsonProperty]
    public EditorTutorialEnd EditorTutorialEnd { get; private set; } = new();

    [JsonProperty]
    public AutoEvoPrediction AutoEvoPrediction { get; private set; } = new();

    [JsonProperty]
    public StaySmallTutorial StaySmallTutorial { get; private set; } = new();

    [JsonProperty]
    public ChemoreceptorPlacementTutorial ChemoreceptorPlacementTutorial { get; private set; } = new();

    [JsonProperty]
    public NegativeAtpBalanceTutorial NegativeAtpBalanceTutorial { get; private set; } = new();

    [JsonProperty]
    public AtpBalanceIntroduction AtpBalanceIntroduction { get; private set; } = new();

    [JsonProperty]
    public CompoundBalancesTutorial CompoundBalancesTutorial { get; private set; } = new();

    [JsonProperty]
    public FoodChainTabTutorial FoodChainTabTutorial { get; private set; } = new();

    [JsonProperty]
    public LeaveColonyTutorial LeaveColonyTutorial { get; private set; } = new();

    [JsonProperty]
    public PausingTutorial PausingTutorial { get; private set; } = new();

    [JsonProperty]
    public SpeciesMemberDiedTutorial SpeciesMemberDiedTutorial { get; private set; } = new();

    /// <summary>
    ///   Tutorial for the become multicellular button. Needs to be before <see cref="MulticellularWelcome"/>
    ///   as this should see the become multicellular event before that other tutorial consumes it.
    /// </summary>
    [JsonProperty]
    public BecomeMulticellularTutorial BecomeMulticellularTutorial { get; private set; } = new();

    [JsonProperty]
    public MulticellularWelcome MulticellularWelcome { get; private set; } = new();

    [JsonProperty]
    public DayNightTutorial DayNightTutorial { get; private set; } = new();

    [JsonProperty]
    public OrganelleDivisionTutorial OrganelleDivisionTutorial { get; private set; } = new();

    [JsonProperty]
    public MadeNoChangesTutorial MadeNoChangesTutorial { get; private set; } = new();

    [JsonProperty]
    public FlagellumPlacementTutorial FlagellumPlacementTutorial { get; private set; } = new();

    [JsonProperty]
    public DigestionStatTutorial DigestionStatTutorial { get; private set; } = new();

    [JsonProperty]
    public ModifyOrganelleTutorial ModifyOrganelleTutorial { get; private set; } = new();

    [JsonProperty]
    public TolerancesTabTutorial TolerancesTabTutorial { get; private set; } = new();

    [JsonProperty]
    public OpenTolerancesTabTutorial OpenTolerancesTabTutorial { get; private set; } = new();

    [JsonProperty]
    public EarlyGameGoalTutorial EarlyGameGoalTutorial { get; private set; } = new();

    [JsonProperty]
    public NucleusTutorial NucleusTutorial { get; private set; } = new();

    // End of tutorial state variables

    [JsonProperty]
    public double TotalElapsed { get; private set; }

    /// <summary>
    ///   True if any of the tutorials are active that want to pause the game
    /// </summary>
    [JsonIgnore]
    public bool WantsGamePaused => Tutorials.Any(t => t.WantsPaused);

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
        {
            if (tutorial.ShownCurrently)
                tutorial.Hide();
        }
    }

    /// <summary>
    ///   Checks if any tutorial is visible
    /// </summary>
    /// <returns>True if any tutorial is visible</returns>
    public bool TutorialActive()
    {
        return Tutorials.Any(t => t.ShownCurrently);
    }

    /// <summary>
    ///   Returns true when the tutorial system is in a state where nearby compound info is wanted
    /// </summary>
    /// <returns>True when the tutorial system wants compound information</returns>
    public bool WantsNearbyCompoundInfo()
    {
        return MicrobeMovement.Complete && !GlucoseCollecting.Complete && GlucoseCollecting.CanTrigger;
    }

    /// <summary>
    ///   Returns true when the tutorial system is in a state where nearby engulfable entity info is wanted
    /// </summary>
    /// <returns>True when the tutorial system wants engulfable entity information</returns>
    public bool WantsNearbyEngulfableInfo()
    {
        return GlucoseCollecting.Complete && !MicrobeEngulfmentExplanation.Complete;
    }

    /// <summary>
    ///   Position in the world to guide the player to
    /// </summary>
    /// <returns>The target position or null</returns>
    public Vector3? GetPlayerGuidancePosition()
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

    public void Process(ITutorialGUI gui, float delta)
    {
        AlreadySeenTutorials.Process(delta);

        if (!Enabled)
        {
            if (previousTutorialActiveState)
            {
                previousTutorialActiveState = false;
                ReportClosedTutorialsToSeenTutorials();
            }

            if (hasPaused)
            {
                UnPause();
            }

            if (needsToApplyEvenIfDisabled)
            {
                HideAll();
                ApplyGUIState(gui);
                needsToApplyEvenIfDisabled = false;
            }

            return;
        }

        HandlePausing();

        timeSinceActiveCheck += delta;
        if (timeSinceActiveCheck > 0.1f)
        {
            timeSinceActiveCheck = 0;

            bool active = TutorialActive();

            if (active != previousTutorialActiveState)
            {
                previousTutorialActiveState = active;

                if (!previousTutorialActiveState)
                {
                    // When tutorials are deactivated, check what has been closed to make sure we haven't missed
                    // any tutorial closings and mark those in the seen system
                    ReportClosedTutorialsToSeenTutorials();
                }
            }
        }

        // Pause if the game is paused, but we didn't want to pause things
        if (PauseManager.Instance.Paused && !WantsGamePaused)
        {
            // Apply GUI states anyway to not have a chance of locking a tutorial on screen
            ApplyGUIState(gui);
            return;
        }

        TotalElapsed += delta;

        foreach (var tutorial in Tutorials)
        {
            if (!tutorial.ShownCurrently && !tutorial.ProcessWhileHidden)
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

    public void OnCompleteTutorialsAlreadySeen()
    {
        // Remember this setting for saving / loading
        DisableShowingAlreadySeenTutorials = true;

        var seen = AlreadySeenTutorials.SeenTutorials;

        foreach (var tutorial in Tutorials)
        {
            if (tutorial.HasBeenShown)
                continue;

            if (seen.Contains(tutorial.ClosedByName))
            {
                // Tutorial seen in another game, suppress it here as well
                tutorial.Inhibit();
            }
        }
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
            {
                tutorial.Hide();

                AlreadySeenTutorials.MarkSeen(name);
            }
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

        ReportClosedTutorialsToSeenTutorials();
    }

    public void OnNextPressed()
    {
        throw new NotImplementedException();
    }

    public void FinishLoading(ISaveContext? context)
    {
        if (DisableShowingAlreadySeenTutorials)
        {
            OnCompleteTutorialsAlreadySeen();
        }
    }

    /// <summary>
    ///   Applies all the GUI states related to the tutorial, this makes saving and loading the tutorial state easier
    /// </summary>
    /// <param name="gui">The target GUI instance</param>
    private void ApplyGUIState(ITutorialGUI gui)
    {
        gui.IsClosingAutomatically = true;

        switch (gui)
        {
            case MicrobeTutorialGUI casted:
                ApplySpecificGUI(casted);
                break;
            case MicrobeEditorTutorialGUI casted:
                ApplySpecificGUI(casted);
                break;
            default:
                throw new ArgumentException("Unhandled GUI class in ApplyGUIState");
        }

        gui.IsClosingAutomatically = false;
        needsToApplyEvenIfDisabled = true;
    }

    private void ApplySpecificGUI(MicrobeTutorialGUI gui)
    {
        foreach (var tutorial in Tutorials)
            tutorial.ApplyGUIState(gui);
    }

    private void ApplySpecificGUI(MicrobeEditorTutorialGUI gui)
    {
        foreach (var tutorial in Tutorials)
            tutorial.ApplyGUIState(gui);
    }

    private void HandlePausing()
    {
        if (WantsGamePaused != hasPaused)
        {
            if (hasPaused)
            {
                // Unpause
                UnPause();
            }
            else
            {
                // Due to initialization stuff, the tutorial is not allowed to immediately pause the game
                if (TotalElapsed < Constants.TIME_BEFORE_TUTORIAL_CAN_PAUSE)
                    return;

                // Pause
                PauseManager.Instance.AddPause(nameof(TutorialState));
                hasPaused = true;
            }
        }
    }

    private void UnPause()
    {
        if (hasPaused)
            PauseManager.Instance.Resume(nameof(TutorialState));
        hasPaused = false;
    }

    private void ReportClosedTutorialsToSeenTutorials()
    {
        foreach (var tutorial in Tutorials)
        {
            if (tutorial.HasBeenShown)
            {
                AlreadySeenTutorials.MarkSeen(tutorial.ClosedByName);
            }
        }
    }

    private List<TutorialPhase> BuildListOfAllTutorials()
    {
        return new List<TutorialPhase>
        {
            MicrobeStageWelcome,
            MicrobeMovement,
            MicrobeMovementExplanation,
            GlucoseCollecting,
            MicrobePressEditorButton,
            MicrobeStayingAlive,
            MicrobeReproduction,
            MicrobeUnbind,
            MicrobeEngulfmentExplanation,
            MicrobeEngulfedExplanation,
            EngulfmentStorageFull,
            OpenProcessPanelTutorial,
            ProcessPanelTutorial,
            ResourcesAfterSplitTutorial,
            CheckTheHelpMenu,
            EditorReportWelcome,
            PatchMap,
            MigrationTutorial,
            CellEditorIntroduction,
            NucleusTutorial,
            EditorUndoTutorial,
            EditorRedoTutorial,
            EditorTutorialEnd,
            StaySmallTutorial,
            ChemoreceptorPlacementTutorial,
            CompoundBalancesTutorial,
            LeaveColonyTutorial,
            BecomeMulticellularTutorial,
            MulticellularWelcome,
            DayNightTutorial,
            MadeNoChangesTutorial,
            OrganelleDivisionTutorial,
            FlagellumPlacementTutorial,
            DigestionStatTutorial,
            ModifyOrganelleTutorial,
            AtpBalanceIntroduction,
            OpenTolerancesTabTutorial,
            TolerancesTabTutorial,
            AutoEvoPrediction,
            EarlyGameGoalTutorial,
            FoodChainTabTutorial,
            NegativeAtpBalanceTutorial,
            PausingTutorial,
            SpeciesMemberDiedTutorial,
        };
    }
}
