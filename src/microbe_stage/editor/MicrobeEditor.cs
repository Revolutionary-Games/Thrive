using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main class of the microbe editor
/// </summary>
[JsonObject(IsReference = true)]
[SceneLoadedClass("res://src/microbe_stage/editor/MicrobeEditor.tscn")]
[DeserializedCallbackTarget]
public class MicrobeEditor : HexEditorBase<MicrobeEditorGUI, MicrobeEditorAction, MicrobeStage, OrganelleTemplate>
{
    /// <summary>
    ///   The new to set on the species after exiting
    /// </summary>
    [JsonProperty]
    public string NewName = "error";

    /// <summary>
    ///   We're taking advantage of the available membrane and organelle system already present in
    ///   the microbe class for the cell preview.
    /// </summary>
    private Microbe? previewMicrobe;

    private MicrobeEditorTutorialGUI tutorialGUI = null!;

    private PackedScene microbeScene = null!;

    [JsonProperty]
    private Color colour;

    [JsonProperty]
    private float rigidity;

    [JsonProperty]
    private BehaviourDictionary? behaviour;

    /// <summary>
    ///   The species that is being edited, changes are applied to it on exit
    /// </summary>
    [JsonProperty]
    private MicrobeSpecies? editedSpecies;

    /// <summary>
    ///   To not have to recreate this object for each place / remove this is a cached clone of editedSpecies to which
    ///   current editor changes are applied for simulating what effect they would have on the population.
    /// </summary>
    private MicrobeSpecies? cachedAutoEvoPredictionSpecies;

    /// <summary>
    ///   This is the container that has the edited organelles in
    ///   it. This is populated when entering and used to update the
    ///   player's species template on exit.
    /// </summary>
    [JsonProperty]
    private OrganelleLayout<OrganelleTemplate> editedMicrobeOrganelles = null!;

    /// <summary>
    ///   When this is true, on next process this will handle added and removed organelles and update stats etc.
    ///   This is done to make adding a bunch of organelles at once more efficient.
    /// </summary>
    private bool organelleDataDirty = true;

    /// <summary>
    ///   Similar to organelleDataDirty but with the exception that this is only set false when the editor
    ///   membrane mesh has been redone. Used so the membrane doesn't have to be rebuild everytime when
    ///   switching back and forth between structure and membrane tab (without editing organelle placements).
    /// </summary>
    private bool membraneOrganellePositionsAreDirty = true;

    private bool microbePreviewMode;

    /// <summary>
    ///   The selected membrane rigidity
    /// </summary>
    [JsonIgnore]
    public float Rigidity
    {
        get => rigidity;
        set
        {
            rigidity = value;

            if (previewMicrobe?.Species != null)
            {
                previewMicrobe.Species.MembraneRigidity = value;
                previewMicrobe.ApplyMembraneWigglyness();
            }
        }
    }

    [JsonProperty]
    public BehaviourDictionary? Behaviour
    {
        get => behaviour ??= editedSpecies?.Behaviour;
        private set => behaviour = value;
    }

    /// <summary>
    ///   Selected membrane type for the species
    /// </summary>
    [JsonProperty]
    public MembraneType Membrane { get; private set; } = null!;

    /// <summary>
    ///   Current selected colour for the species.
    /// </summary>
    [JsonIgnore]
    public Color Colour
    {
        get => colour;
        set
        {
            colour = value;

            if (previewMicrobe?.Species != null)
            {
                previewMicrobe.Species.Colour = value;
                previewMicrobe.Membrane.Tint = value;
                previewMicrobe.ApplyPreviewOrganelleColours();
            }
        }
    }

    /// <summary>
    ///   The name of organelle type that is selected to be placed
    /// </summary>
    [JsonIgnore]
    public string? ActiveActionName
    {
        get => activeActionName;
        set
        {
            if (value != activeActionName)
            {
                TutorialState.SendEvent(TutorialEventType.MicrobeEditorOrganelleToPlaceChanged,
                    new StringEventArgs(value), this);
            }

            activeActionName = value;
        }
    }

    /// <summary>
    ///   If this is enabled the editor will show how the edited cell would look like in the environment with
    ///   parameters set in the editor. Editing hexes is disabled during this (with the exception of undo/redo).
    /// </summary>
    public bool MicrobePreviewMode
    {
        get => microbePreviewMode;
        set
        {
            microbePreviewMode = value;

            UpdateCellVisualization();

            if (previewMicrobe != null)
                previewMicrobe.Visible = value;

            placedHexes.ForEach(entry => entry.Visible = !MicrobePreviewMode);
            placedModels.ForEach(entry => entry.Visible = !MicrobePreviewMode);
        }
    }

    [JsonIgnore]
    public TutorialState TutorialState => CurrentGame?.TutorialState ??
        throw new InvalidOperationException("Editor doesn't have current game set yet");

    [JsonIgnore]
    public bool HasNucleus => PlacedUniqueOrganelles.Any(d => d.InternalName == "nucleus");

    [JsonIgnore]
    public override bool HasIslands => editedMicrobeOrganelles.GetIslandHexes().Count > 0;

    /// <summary>
    ///   Number of organelles in the microbe
    /// </summary>
    [JsonIgnore]
    public int MicrobeSize => editedMicrobeOrganelles.Organelles.Count;

    /// <summary>
    ///   Number of hexes in the microbe
    /// </summary>
    [JsonIgnore]
    public int MicrobeHexSize
    {
        get
        {
            int result = 0;

            foreach (var organelle in editedMicrobeOrganelles.Organelles)
            {
                result += organelle.Definition.HexCount;
            }

            return result;
        }
    }

    public IEnumerable<OrganelleDefinition> PlacedUniqueOrganelles => editedMicrobeOrganelles
        .Where(p => p.Definition.Unique)
        .Select(p => p.Definition);

    /// <summary>
    ///   If true an editor action is active and can be cancelled. Currently only checks for organelle move.
    /// </summary>
    [JsonIgnore]
    public override bool CanCancelAction => CanCancelMove;

    protected override Species EditedBaseSpecies =>
        editedSpecies ?? throw new InvalidOperationException("species not initialized");

    protected override string MusicCategory => "MicrobeEditor";

    protected override MainGameState ReturnToState => MainGameState.MicrobeStage;
    protected override string EditorLoadingMessage => TranslationServer.Translate("LOADING_MICROBE_EDITOR");

    protected override bool ForceHideHover => MicrobePreviewMode;

    public override void _Ready()
    {
        base._Ready();

        tutorialGUI.Visible = true;

    }

    protected override void InitConcreteGUI()
    {
        GUI.Init(this);
    }

    public override void OnFinishEditing()
    {
        base.OnFinishEditing();

        // Apply changes to the species organelles
        // It is easiest to just replace all
        editedSpecies!.Organelles.Clear();

        foreach (var organelle in editedMicrobeOrganelles.Organelles)
        {
            var organelleToAdd = (OrganelleTemplate)organelle.Clone();
            organelleToAdd.PlacedThisSession = false;
            organelleToAdd.NumberOfTimesMoved = 0;
            editedSpecies.Organelles.Add(organelleToAdd);
        }

        editedSpecies.RepositionToOrigin();

        // Update bacteria status
        editedSpecies.IsBacteria = !HasNucleus;

        editedSpecies.UpdateInitialCompounds();

        GD.Print("MicrobeEditor: updated organelles for species: ", editedSpecies.FormattedName);

        // Update name, if valid
        var match = Regex.Match(NewName, Constants.SPECIES_NAME_REGEX);
        if (match.Success)
        {
            editedSpecies.Genus = match.Groups["genus"].Value;
            editedSpecies.Epithet = match.Groups["epithet"].Value;

            GD.Print("MicrobeEditor: edited species name is now ", editedSpecies.FormattedName);
        }
        else
        {
            GD.PrintErr("MicrobeEditor: Invalid newName: ", NewName);
        }

        // Update membrane
        editedSpecies.MembraneType = Membrane;
        editedSpecies.Colour = Colour;
        editedSpecies.MembraneRigidity = Rigidity;

        editedSpecies.Behaviour = Behaviour ?? throw new Exception("Editor has not created behaviour object");

        var stage = ReturnToStage!;

        // This needs to be reset here to not free this when we exit the tree
        ReturnToStage = null;

        SceneManager.Instance.SwitchToScene(stage);

        stage.OnReturnFromEditor();
    }

    public override void _Notification(int what)
    {
        // Rebuilds and recalculates all value dependent UI elements on language change
        if (what == NotificationTranslationChanged)
        {
            CalculateOrganelleEffectivenessInPatch(CurrentPatch);
            UpdatePatchDependentBalanceData();
            GUI.UpdateAutoEvoResults(autoEvoSummary?.ToString() ?? "error", autoEvoExternal?.ToString() ?? "error");
            GUI.UpdateTimeIndicator(CurrentGame!.GameWorld.TotalPassedTime);
            GUI.UpdateGlucoseReduction(Constants.GLUCOSE_REDUCTION_RATE);
            GUI.UpdatePatchDetails(CurrentPatch);
            GUI.UpdateMicrobePartSelections();
            GUI.UpdateMutationPointsBar();
            GUI.UpdateTimeline();
            GUI.UpdateReportTabPatchSelector();
        }
    }

    /// <summary>
    ///   Wipes clean the current cell.
    /// </summary>
    public void CreateNewMicrobe()
    {
        if (!FreeBuilding)
            throw new InvalidOperationException("can't reset cell when not freebuilding");

        var previousMP = MutationPoints;
        var oldEditedMicrobeOrganelles = new OrganelleLayout<OrganelleTemplate>();
        var oldMembrane = Membrane;

        foreach (var organelle in editedMicrobeOrganelles)
        {
            oldEditedMicrobeOrganelles.Add(organelle);
        }

        var data = new NewMicrobeActionData(oldEditedMicrobeOrganelles, previousMP, oldMembrane);

        var action = new MicrobeEditorAction(this, 0, DoNewMicrobeAction, UndoNewMicrobeAction, data);

        EnqueueAction(action);
    }

    public void SetMembrane(string membraneName)
    {
        var membrane = SimulationParameters.Instance.GetMembrane(membraneName);

        if (Membrane.Equals(membrane))
            return;

        var action = new MicrobeEditorAction(this, membrane.EditorCost, DoMembraneChangeAction,
            UndoMembraneChangeAction, new MembraneActionData(Membrane, membrane));

        EnqueueAction(action);

        // In case the action failed, we need to make sure the membrane buttons are updated properly
        GUI.UpdateMembraneButtons(Membrane.InternalName);
    }

    public void SetBehaviouralValue(BehaviouralValueType type, float value)
    {
        GUI.UpdateBehaviourSlider(type, value);

        if (Behaviour == null)
            throw new Exception($"{nameof(Behaviour)} is not set for editor");

        var oldValue = Behaviour[type];

        if (Math.Abs(value - oldValue) < MathUtils.EPSILON)
            return;

        var action = new MicrobeEditorAction(this, 0, DoBehaviourChangeAction, UndoBehaviourChangeAction,
            new BehaviourChangeActionData(value, oldValue, type));

        EnqueueAction(action);
    }

    public void SetRigidity(int rigidity)
    {
        int intRigidity = (int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO);

        if (MovingPlacedHex != null)
        {
            GUI.OnActionBlockedWhileMoving();
            GUI.UpdateRigiditySlider(intRigidity);
            return;
        }

        if (intRigidity == rigidity)
            return;

        int cost = Math.Abs(rigidity - intRigidity) * Constants.MEMBRANE_RIGIDITY_COST_PER_STEP;

        if (cost > MutationPoints)
        {
            int stepsLeft = MutationPoints / Constants.MEMBRANE_RIGIDITY_COST_PER_STEP;
            if (stepsLeft < 1)
            {
                GUI.UpdateRigiditySlider(intRigidity);
                return;
            }

            rigidity = intRigidity > rigidity ? intRigidity - stepsLeft : intRigidity + stepsLeft;
            cost = stepsLeft * Constants.MEMBRANE_RIGIDITY_COST_PER_STEP;
        }

        var newRigidity = rigidity / Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO;
        var prevRigidity = Rigidity;

        var action = new MicrobeEditorAction(this, cost, DoRigidityChangeAction, UndoRigidityChangeAction,
            new RigidityChangeActionData(newRigidity, prevRigidity));

        EnqueueAction(action);
    }

    /// <summary>
    ///   Show options for the organelle under the cursor
    /// </summary>
    [RunOnKeyDown("e_secondary")]
    public void ShowOrganelleOptions()
    {
        if (MicrobePreviewMode)
            return;

        // Can't open organelle popup menu while moving something
        if (MovingPlacedHex != null)
        {
            GUI.OnActionBlockedWhileMoving();
            return;
        }

        GetMouseHex(out int q, out int r);

        var organelle = editedMicrobeOrganelles.GetOrganelleAt(new Hex(q, r));

        if (organelle == null)
            return;

        GUI.ShowOrganelleMenu(organelle);
    }

    public float CalculateSpeed()
    {
        return MicrobeInternalCalculations.CalculateSpeed(editedMicrobeOrganelles, Membrane, Rigidity);
    }

    public float CalculateHitpoints()
    {
        var maxHitpoints = Membrane.Hitpoints +
            (Rigidity * Constants.MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER);

        return maxHitpoints;
    }

    /// <summary>
    ///   Returns the cost of the organelle that is about to be placed
    /// </summary>
    public override float CalculateCurrentActionCost()
    {
        if (string.IsNullOrEmpty(ActiveActionName) || !ShowHover)
            return 0;

        var cost = SimulationParameters.Instance.GetOrganelleType(ActiveActionName!).MPCost;

        switch (Symmetry)
        {
            case HexEditorSymmetry.XAxisSymmetry:
                cost *= 2;
                break;
            case HexEditorSymmetry.FourWaySymmetry:
                cost *= 4;
                break;
            case HexEditorSymmetry.SixWaySymmetry:
                cost *= 6;
                break;
        }

        return cost;
    }

    public override void SetPlayerPatch(Patch? patch)
    {
        base.SetPlayerPatch(patch);

        GUI.UpdatePlayerPatch(targetPatch);
        CalculateOrganelleEffectivenessInPatch(targetPatch);
        UpdatePatchDependentBalanceData();
    }

    protected override void ResolveDerivedTypeNodeReferences()
    {
        base.ResolveDerivedTypeNodeReferences();

        GUI = GetNode<MicrobeEditorGUI>("MicrobeEditorGUI");
        tutorialGUI = GetNode<MicrobeEditorTutorialGUI>("TutorialGUI");
    }

    protected override void LoadScenes()
    {
        base.LoadScenes();

        microbeScene = GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn");
    }

    protected override void OnEnterEditor()
    {
        base.OnEnterEditor();

        if (!IsLoadedFromSave)
            TutorialState.SendEvent(TutorialEventType.EnteredMicrobeEditor, EventArgs.Empty, this);
    }

    /// <summary>
    ///   Combined old editor init and activate method
    /// </summary>
    protected override void InitEditor()
    {
        base.InitEditor();

        if (!IsLoadedFromSave)
        {
            GUI.ResetSymmetryButton();
        }
        else
        {
            GUI.SetSymmetry(Symmetry);
            GUI.UpdatePlayerPatch(targetPatch);
        }

        if (editedSpecies == null)
            throw new Exception($"Editor setup which was just ran didn't setup {nameof(editedSpecies)}");

        // Setup the display cell
        previewMicrobe = (Microbe)microbeScene.Instance();
        previewMicrobe.IsForPreviewOnly = true;
        rootOfDynamicallySpawned.AddChild(previewMicrobe);
        previewMicrobe.ApplySpecies((MicrobeSpecies)editedSpecies.Clone());

        // Set its initial visibility
        previewMicrobe.Visible = MicrobePreviewMode;

        UpdateUndoRedoButtons();

        UpdateArrow(false);

        GUI.UpdateMutationPointsBar(false);

        // Send freebuild value to GUI
        GUI.NotifyFreebuild(FreeBuilding);

        // Send info to the GUI about the organelle effectiveness in the current patch
        CalculateOrganelleEffectivenessInPatch(CurrentPatch);

        GUI.SetMap(CurrentGame!.GameWorld.Map);

        GUI.UpdateGlucoseReduction(Constants.GLUCOSE_REDUCTION_RATE);

        GUI.UpdateReportTabPatchSelector();

        GUI.UpdateRigiditySliderState(MutationPoints);

        // Make tutorials run
        tutorialGUI.EventReceiver = TutorialState;
        pauseMenu.GameProperties = CurrentGame;

        // Send undo button to the tutorial system
        GUI.SendUndoToTutorial(TutorialState);

        GUI.UpdateCancelButtonVisibility();
    }

    protected override void InitEditorFresh()
    {
        editedMicrobeOrganelles = new OrganelleLayout<OrganelleTemplate>(
            OnOrganelleAdded, OnOrganelleRemoved);

        base.InitEditorFresh();

        CurrentGame!.SetBool("edited_microbe", true);

        var playerSpecies = CurrentGame.GameWorld.PlayerSpecies;

        SetupEditedSpecies((MicrobeSpecies)playerSpecies);
    }

    protected override void InitEditorSaved()
    {
        UpdateGUIAfterLoadingSpecies(editedSpecies ??
            throw new JsonException($"Saved editor was missing {nameof(editedSpecies)}"));

        base.InitEditorSaved();

        // The error conditions here probably shouldn't be able to trigger at all
        GUI.UpdateAutoEvoResults(autoEvoSummary?.ToString() ?? "error", autoEvoExternal?.ToString() ?? "error");

        GUI.UpdateTimeIndicator(CurrentGame!.GameWorld.TotalPassedTime);

        GUI.UpdateReportTabStatistics(CurrentPatch);
        GUI.UpdateTimeline();
    }

    protected override void OnEditorReady()
    {
        // The base method stores the data, so we just need to update the GUI here (in case of failure)
        var run = CurrentGame!.GameWorld.GetAutoEvoRun();

        if (run.Results == null)
        {
            GUI.UpdateAutoEvoResults(TranslationServer.Translate("AUTO_EVO_FAILED"),
                TranslationServer.Translate("AUTO_EVO_RUN_STATUS") + " " + run.Status);
        }

        base.OnEditorReady();

        GD.Print("Elapsing time on editor entry");

        // TODO: select which units will be used for the master elapsed time counter
        CurrentGame!.GameWorld.OnTimePassed(1);

        GUI.UpdateTimeIndicator(CurrentGame.GameWorld.TotalPassedTime);

        if (autoEvoSummary != null && autoEvoExternal != null)
        {
            GUI.UpdateAutoEvoResults(autoEvoSummary.ToString(), autoEvoExternal.ToString());
        }

        GUI.UpdateReportTabStatistics(CurrentPatch);
        GUI.UpdateTimeline();
    }

    protected override GameProperties StartNewGameForEditor()
    {
        return GameProperties.StartNewMicrobeGame();
    }

    protected override void UpdateEditor(float delta)
    {
        if (organelleDataDirty)
        {
            OnOrganellesChanged();
            organelleDataDirty = false;
        }

        base.UpdateEditor(delta);

        // Show the organelle that is about to be placed
        if (ActiveActionName != null && ShowHover && !MicrobePreviewMode)
        {
            GetMouseHex(out int q, out int r);

            OrganelleDefinition shownOrganelle;

            var effectiveSymmetry = Symmetry;

            if (MovingPlacedHex == null)
            {
                // Can place stuff at all?
                isPlacementProbablyValid = IsValidPlacement(new OrganelleTemplate(
                    GetOrganelleDefinition(ActiveActionName), new Hex(q, r), organelleRot));

                shownOrganelle = SimulationParameters.Instance.GetOrganelleType(ActiveActionName);
            }
            else
            {
                isPlacementProbablyValid = IsMoveTargetValid(new Hex(q, r), organelleRot, MovingPlacedHex);
                shownOrganelle = MovingPlacedHex.Definition;
                effectiveSymmetry = HexEditorSymmetry.None;
            }

            switch (effectiveSymmetry)
            {
                case HexEditorSymmetry.None:
                {
                    RenderHighlightedOrganelle(q, r, organelleRot, shownOrganelle);
                    break;
                }

                case HexEditorSymmetry.XAxisSymmetry:
                {
                    RenderHighlightedOrganelle(q, r, organelleRot, shownOrganelle);
                    RenderHighlightedOrganelle(-1 * q, r + q, 6 + (-1 * organelleRot), shownOrganelle);
                    break;
                }

                case HexEditorSymmetry.FourWaySymmetry:
                {
                    RenderHighlightedOrganelle(q, r, organelleRot, shownOrganelle);
                    RenderHighlightedOrganelle(-1 * q, r + q, 6 + (-1 * organelleRot), shownOrganelle);
                    RenderHighlightedOrganelle(-1 * q, -1 * r, (organelleRot + 3) % 6, shownOrganelle);
                    RenderHighlightedOrganelle(q, -1 * (r + q), 9 + (-1 * organelleRot) % 6, shownOrganelle);
                    break;
                }

                case HexEditorSymmetry.SixWaySymmetry:
                {
                    RenderHighlightedOrganelle(q, r, organelleRot, shownOrganelle);
                    RenderHighlightedOrganelle(-1 * r, r + q, (organelleRot + 1) % 6, shownOrganelle);
                    RenderHighlightedOrganelle(-1 * (r + q), q, (organelleRot + 2) % 6, shownOrganelle);
                    RenderHighlightedOrganelle(-1 * q, -1 * r, (organelleRot + 3) % 6, shownOrganelle);
                    RenderHighlightedOrganelle(r, -1 * (r + q), (organelleRot + 4) % 6, shownOrganelle);
                    RenderHighlightedOrganelle(r + q, -1 * q, (organelleRot + 5) % 6, shownOrganelle);
                    break;
                }
            }
        }
    }

    protected override void PerformActiveAction()
    {
        if (AddOrganelle(ActiveActionName!))
        {
            // Only trigger tutorial if an organelle was really placed
            TutorialState.SendEvent(TutorialEventType.MicrobeEditorOrganellePlaced, EventArgs.Empty, this);
        }
    }

    protected override void PerformMove(int q, int r)
    {
        if (MoveOrganelle(MovingPlacedHex!, MovingPlacedHex!.Position, new Hex(q, r), MovingPlacedHex.Orientation,
                organelleRot))
        {
            // Move succeeded; Update the cancel button visibility so it's hidden because the move has completed
            MovingPlacedHex = null;
            GUI.UpdateCancelButtonVisibility();

            // Update rigidity slider in case it was disabled
            // TODO: could come up with a bit nicer design here
            int intRigidity = (int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO);
            GUI.UpdateRigiditySlider(intRigidity);

            // Re-enable undo/redo button
            UpdateUndoRedoButtons();
        }
        else
        {
            GUI.PlayInvalidActionSound();
        }
    }

    protected override bool IsMoveTargetValid(Hex position, int rotation, OrganelleTemplate organelle)
    {
        return editedMicrobeOrganelles.CanPlace(organelle.Definition, position, rotation, false);
    }

    protected override bool DoesActionEndInProgressAction(MicrobeEditorAction action)
    {
        // Allow only move actions with an in-progress move
        return action.IsMoveAction;
    }

    protected override void OnCurrentActionCanceled()
    {
        editedMicrobeOrganelles.Add(MovingPlacedHex!);
        GUI.UpdateCancelButtonVisibility();
    }

    protected override void OnMoveActionStarted()
    {
        editedMicrobeOrganelles.Remove(MovingPlacedHex!);
    }

    protected override void OnUndoPerformed()
    {
        TutorialState.SendEvent(TutorialEventType.MicrobeEditorUndo, EventArgs.Empty, this);
    }

    protected override void OnRedoPerformed()
    {
        TutorialState.SendEvent(TutorialEventType.MicrobeEditorRedo, EventArgs.Empty, this);
    }

    protected override OrganelleTemplate? GetHexAt(Hex position)
    {
        return editedMicrobeOrganelles.GetOrganelleAt(position);
    }

    protected override void TryRemoveHexAt(Hex location)
    {
        var organelleHere = editedMicrobeOrganelles.GetOrganelleAt(location);
        if (organelleHere == null)
            return;

        // Dont allow deletion of nucleus or the last organelle
        if (organelleHere.Definition.InternalName == "nucleus" || MicrobeSize < 2)
            return;

        // If it was placed this session, just refund the cost of adding it.
        int cost = organelleHere.PlacedThisSession ?
            -organelleHere.Definition.MPCost :
            Constants.ORGANELLE_REMOVE_COST;

        var action = new MicrobeEditorAction(this, cost,
            DoOrganelleRemoveAction, UndoOrganelleRemoveAction, new RemoveActionData(organelleHere));

        EnqueueAction(action);
    }

    protected override void OnInsufficientMP()
    {
        GUI.OnInsufficientMp();
    }

    protected override void OnActionBlockedWhileMoving()
    {
        GUI.OnActionBlockedWhileMoving();
    }

    protected override void PerformAutoSave()
    {
        SaveHelper.AutoSave(this);
    }

    protected override void PerformQuickSave()
    {
        SaveHelper.QuickSave(this);
    }

    protected override void OnMutationPointsChanged()
    {
        GUI.UpdateMutationPointsBar();
        GUI.UpdateRigiditySliderState(MutationPoints);
    }

    protected override void UpdateUndoRedoButtons()
    {
        GUI.SetUndoButtonStatus(history.CanUndo() && MovingPlacedHex == null);
        GUI.SetRedoButtonStatus(history.CanRedo() && MovingPlacedHex == null);
    }

    protected override void UpdateCancelState()
    {
        GUI.UpdateCancelButtonVisibility();
    }

    private bool HasOrganelle(OrganelleDefinition organelleDefinition)
    {
        return editedMicrobeOrganelles.Organelles.Any(o => o.Definition == organelleDefinition);
    }

    /// <summary>
    ///   Updates the arrowPosition variable to the top most point of the middle 3 rows
    ///   Should be called on any layout change
    /// </summary>
    private void UpdateArrow(bool animateMovement = true)
    {
        // The calculation falls back to 0 if there are no hexes found in the middle 3 rows
        var highestPointInMiddleRows = 0.0f;

        // Iterate through all organelles
        foreach (var organelle in editedMicrobeOrganelles)
        {
            // Iterate through all hexes
            foreach (var relativeHex in organelle.Definition.Hexes)
            {
                var absoluteHex = relativeHex + organelle.Position;

                // Only consider the middle 3 rows
                if (absoluteHex.Q is < -1 or > 1)
                    continue;

                var cartesian = Hex.AxialToCartesian(absoluteHex);

                // Get the min z-axis (highest point in the editor)
                highestPointInMiddleRows = Mathf.Min(highestPointInMiddleRows, cartesian.z);
            }
        }

        if (animateMovement)
        {
            GUICommon.Instance.Tween.InterpolateProperty(editorArrow, "translation:z", editorArrow.Translation.z,
                highestPointInMiddleRows - Constants.EDITOR_ARROW_OFFSET, Constants.EDITOR_ARROW_INTERPOLATE_SPEED,
                Tween.TransitionType.Expo, Tween.EaseType.Out);
            GUICommon.Instance.Tween.Start();
        }
        else
        {
            editorArrow.Translation = new Vector3(0, 0, highestPointInMiddleRows - Constants.EDITOR_ARROW_OFFSET);
        }
    }

    /// <summary>
    ///   Calculates the effectiveness of organelles in the current or given patch
    /// </summary>
    private void CalculateOrganelleEffectivenessInPatch(Patch? patch = null)
    {
        patch ??= CurrentPatch;

        var organelles = SimulationParameters.Instance.GetAllOrganelles();

        var result = ProcessSystem.ComputeOrganelleProcessEfficiencies(organelles, patch.Biome);

        GUI.UpdateOrganelleEfficiencies(result);
    }

    private void StartAutoEvoPrediction()
    {
        // First prediction can be made only after population numbers from previous run are applied
        // so this is just here to guard against that potential programming mistake that may happen when code is
        // changed
        if (!Ready)
        {
            GD.PrintErr("Can't start auto-evo prediction before editor is ready");
            return;
        }

        // Note that in rare cases the auto-evo run doesn't manage to stop before we edit the cached species object
        // which may cause occasional background task errors
        GUI.CancelPreviousAutoEvoPrediction();

        if (editedSpecies == null)
            throw new InvalidOperationException("Editor has not been setup correctly, missing edited species");

        cachedAutoEvoPredictionSpecies ??= (MicrobeSpecies)editedSpecies.Clone();

        CopyEditedPropertiesToSpecies(cachedAutoEvoPredictionSpecies);

        var run = new EditorAutoEvoRun(CurrentGame!.GameWorld, editedSpecies, cachedAutoEvoPredictionSpecies);
        run.Start();

        GUI.UpdateAutoEvoPrediction(run, editedSpecies, cachedAutoEvoPredictionSpecies);
    }

    /// <summary>
    ///   Calculates the energy balance for a cell with the given organelles
    /// </summary>
    private void CalculateEnergyBalanceWithOrganellesAndMembraneType(List<OrganelleTemplate> organelles,
        MembraneType membrane, Patch? patch = null)
    {
        patch ??= CurrentPatch;

        GUI.UpdateEnergyBalance(
            ProcessSystem.ComputeEnergyBalance(organelles, patch.Biome, membrane));
    }

    private void CalculateCompoundBalanceInPatch(List<OrganelleTemplate> organelles, Patch? patch = null)
    {
        patch ??= CurrentPatch;

        var result = ProcessSystem
            .ComputeCompoundBalance(organelles, patch.Biome);

        GUI.UpdateCompoundBalances(result);
    }

    private void SetupEditedSpecies(MicrobeSpecies species)
    {
        editedSpecies = species ?? throw new NullReferenceException("didn't find edited species");

        // We need to set the membrane type here so the ATP balance
        // bar can take it into account (the bar is updated when
        // organelles are added)
        Membrane = species.MembraneType;
        Rigidity = species.MembraneRigidity;
        Colour = species.Colour;

        Behaviour = species.Behaviour;

        // Get the species organelles to be edited. This also updates the placeholder hexes
        foreach (var organelle in species.Organelles.Organelles)
        {
            editedMicrobeOrganelles.Add((OrganelleTemplate)organelle.Clone());
        }

#pragma warning disable 162

        // Disabled warning as this is a tweak constant
        // ReSharper disable ConditionIsAlwaysTrueOrFalse HeuristicUnreachableCode
        if (Constants.CREATE_COPY_OF_EDITED_SPECIES)
        {
            // Create a mutated version of the current species code to compete against the player
            CreateMutatedSpeciesCopy(species);
        }

        // ReSharper restore ConditionIsAlwaysTrueOrFalse HeuristicUnreachableCode
#pragma warning restore 162

        NewName = species.FormattedName;

        species.Generation += 1;

        // Only when not loaded from save are these properties fetched
        GUI.SetInitialCellStats();

        UpdateGUIAfterLoadingSpecies(species);
    }

    private void UpdateGUIAfterLoadingSpecies(MicrobeSpecies species)
    {
        GD.Print("Starting microbe editor with: ", editedMicrobeOrganelles.Organelles.Count,
            " organelles in the microbe");

        // Update GUI buttons now that we have correct organelles
        GUI.UpdatePartsAvailability(PlacedUniqueOrganelles.ToList());

        // Reset to cytoplasm if nothing is selected
        GUI.OnOrganelleToPlaceSelected(ActiveActionName ?? "cytoplasm");

        GUI.SetSpeciesInfo(NewName, Membrane, Colour, Rigidity,
            Behaviour ?? throw new Exception($"Editor doesn't have {nameof(Behaviour)} setup"));
        GUI.UpdateGeneration(species.Generation);
        GUI.UpdateHitpoints(CalculateHitpoints());
    }

    private void CreateMutatedSpeciesCopy(Species species)
    {
        var newSpecies = CurrentGame!.GameWorld.CreateMutatedSpecies(species);

        var random = new Random();

        var population = random.Next(Constants.INITIAL_SPLIT_POPULATION_MIN,
            Constants.INITIAL_SPLIT_POPULATION_MAX + 1);

        if (!CurrentGame.GameWorld.Map.CurrentPatch!.AddSpecies(newSpecies, population))
        {
            GD.PrintErr("Failed to create a mutated version of the edited species");
        }
    }

    /// <summary>
    ///   If not hovering over an organelle, render the to-be-placed organelle
    /// </summary>
    private void RenderHighlightedOrganelle(int q, int r, int rotation, OrganelleDefinition shownOrganelle)
    {
        if (MovingPlacedHex == null && ActiveActionName == null)
            return;

        bool showModel = true;

        foreach (var hex in shownOrganelle.GetRotatedHexes(rotation))
        {
            int posQ = hex.Q + q;
            int posR = hex.R + r;

            var pos = Hex.AxialToCartesian(new Hex(posQ, posR));

            // Detect can it be placed there
            bool canPlace = isPlacementProbablyValid;

            bool duplicate = false;

            // Skip if there is a placed organelle here already
            foreach (var placed in placedHexes)
            {
                if ((pos - placed.Translation).LengthSquared() < 0.001f)
                {
                    duplicate = true;

                    if (!canPlace)
                    {
                        // This check is here so that if there are multiple hover hexes overlapping this hex, then
                        // we do actually remember the original material
                        if (!hoverOverriddenMaterials.ContainsKey(placed))
                        {
                            // Store the material to put it back later
                            hoverOverriddenMaterials[placed] = placed.MaterialOverride;
                        }

                        // Mark as invalid
                        placed.MaterialOverride = invalidMaterial;

                        showModel = false;
                    }

                    break;
                }
            }

            // Or if there is already a hover hex at this position
            for (int i = 0; i < usedHoverHex; ++i)
            {
                if ((pos - hoverHexes[i].Translation).LengthSquared() < 0.001f)
                {
                    duplicate = true;
                    break;
                }
            }

            if (duplicate)
                continue;

            var hoverHex = hoverHexes[usedHoverHex++];

            hoverHex.Translation = pos;
            hoverHex.Visible = true;

            hoverHex.MaterialOverride = canPlace ? validMaterial : invalidMaterial;
        }

        // Model
        if (!string.IsNullOrEmpty(shownOrganelle.DisplayScene) && showModel)
        {
            var cartesianPosition = Hex.AxialToCartesian(new Hex(q, r));

            var organelleModel = hoverModels[usedHoverModel++];

            organelleModel.Transform = new Transform(
                MathUtils.CreateRotationForOrganelle(rotation),
                cartesianPosition + shownOrganelle.CalculateModelOffset());

            organelleModel.Scale = new Vector3(Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
                Constants.DEFAULT_HEX_SIZE);

            organelleModel.Visible = true;

            UpdateOrganellePlaceHolderScene(organelleModel, shownOrganelle.DisplayScene!,
                shownOrganelle, Hex.GetRenderPriority(new Hex(q, r)));
        }
    }

    /// <summary>
    ///   Updates the membrane and organelle placement of the preview cell.
    /// </summary>
    private void UpdateCellVisualization()
    {
        if (previewMicrobe == null)
            return;

        // Don't redo the preview cell when not in the preview mode to avoid unnecessary lags
        if (!MicrobePreviewMode || !membraneOrganellePositionsAreDirty)
            return;

        CopyEditedPropertiesToSpecies(previewMicrobe.Species);

        // Intentionally force it to not be bacteria to show it at full size
        previewMicrobe.Species.IsBacteria = false;

        // This is now just for applying changes in the species to the preview cell
        previewMicrobe.ApplySpecies(previewMicrobe.Species);

        membraneOrganellePositionsAreDirty = false;
    }

    /// <summary>
    ///   Copies current editor state to a species
    /// </summary>
    /// <param name="target">The species to copy to</param>
    /// <remarks>
    ///   <para>
    ///     TODO: it would be nice to unify this and the final apply properties to the edited species
    ///   </para>
    /// </remarks>
    private void CopyEditedPropertiesToSpecies(MicrobeSpecies target)
    {
        target.Colour = Colour;
        target.MembraneType = Membrane;
        target.MembraneRigidity = Rigidity;
        target.IsBacteria = true;

        target.Organelles.Clear();

        // TODO: if this is too slow to copy each organelle like this, we'll need to find a faster way to get the data
        // in, perhaps by sharing the entire Organelles object
        foreach (var entry in editedMicrobeOrganelles.Organelles)
        {
            if (entry.Definition.InternalName == "nucleus")
                target.IsBacteria = false;

            target.Organelles.Add(entry);
        }
    }

    /// <summary>
    ///   Places an organelle of the specified type under the cursor
    ///   and also applies symmetry to place multiple at once.
    /// </summary>
    /// <returns>True when at least one organelle got placed</returns>
    private bool AddOrganelle(string organelleType)
    {
        GetMouseHex(out int q, out int r);

        bool placedSomething = false;

        switch (Symmetry)
        {
            case HexEditorSymmetry.None:
            {
                PlaceIfPossible(organelleType, q, r, organelleRot, ref placedSomething);
                break;
            }

            case HexEditorSymmetry.XAxisSymmetry:
            {
                PlaceIfPossible(organelleType, q, r, organelleRot, ref placedSomething);

                if (q != -1 * q || r != r + q)
                {
                    PlaceIfPossible(organelleType, -1 * q, r + q, 6 + (-1 * organelleRot), ref placedSomething);
                }

                break;
            }

            case HexEditorSymmetry.FourWaySymmetry:
            {
                PlaceIfPossible(organelleType, q, r, organelleRot, ref placedSomething);

                if (q != -1 * q || r != r + q)
                {
                    PlaceIfPossible(organelleType, -1 * q, r + q, 6 + (-1 * organelleRot), ref placedSomething);
                    PlaceIfPossible(organelleType, -1 * q, -1 * r, (organelleRot + 3) % 6, ref placedSomething);
                    PlaceIfPossible(organelleType, q, -1 * (r + q), (9 + (-1 * organelleRot)) % 6, ref placedSomething);
                }
                else
                {
                    PlaceIfPossible(organelleType, -1 * q, -1 * r, (organelleRot + 3) % 6, ref placedSomething);
                }

                break;
            }

            case HexEditorSymmetry.SixWaySymmetry:
            {
                PlaceIfPossible(organelleType, q, r, organelleRot, ref placedSomething);

                PlaceIfPossible(organelleType, -1 * r, r + q, (organelleRot + 1) % 6, ref placedSomething);
                PlaceIfPossible(organelleType, -1 * (r + q), q,
                    (organelleRot + 2) % 6, ref placedSomething);
                PlaceIfPossible(organelleType, -1 * q, -1 * r, (organelleRot + 3) % 6, ref placedSomething);
                PlaceIfPossible(organelleType, r, -1 * (r + q),
                    (organelleRot + 4) % 6, ref placedSomething);
                PlaceIfPossible(organelleType, r + q, -1 * q, (organelleRot + 5) % 6, ref placedSomething);

                break;
            }

            default:
            {
                throw new Exception("unimplemented symmetry in AddOrganelle");
            }
        }

        return placedSomething;
    }

    /// <summary>
    ///   Helper for AddOrganelle
    /// </summary>
    private void PlaceIfPossible(string organelleType, int q, int r, int rotation, ref bool placed)
    {
        if (MicrobePreviewMode)
            return;

        var organelle = new OrganelleTemplate(GetOrganelleDefinition(organelleType),
            new Hex(q, r), rotation);

        if (!IsValidPlacement(organelle))
        {
            // Play Sound
            GUI.OnInvalidHexLocationSelected();
            return;
        }

        if (AddOrganelle(organelle))
        {
            placed = true;
        }
    }

    private bool IsValidPlacement(OrganelleTemplate organelle)
    {
        bool notPlacingCytoplasm = organelle.Definition.InternalName != "cytoplasm";

        return editedMicrobeOrganelles.CanPlaceAndIsTouching(
            organelle,
            notPlacingCytoplasm,
            notPlacingCytoplasm);
    }

    private OrganelleDefinition GetOrganelleDefinition(string name)
    {
        return SimulationParameters.Instance.GetOrganelleType(name);
    }

    [DeserializedCallbackAllowed]
    private void DoOrganellePlaceAction(MicrobeEditorAction action)
    {
        var data = (PlacementActionData?)action.Data ??
            throw new Exception($"{nameof(DoOrganellePlaceAction)} missing action data");

        data.ReplacedCytoplasm = new List<OrganelleTemplate>();
        var organelle = data.Organelle;

        // Check if there is cytoplasm under this organelle.
        foreach (var hex in organelle.RotatedHexes)
        {
            var organelleHere = editedMicrobeOrganelles.GetOrganelleAt(
                hex + organelle.Position);

            if (organelleHere == null)
                continue;

            if (organelleHere.Definition.InternalName != "cytoplasm")
            {
                throw new Exception("Can't place organelle on top of something " +
                    "else than cytoplasm");
            }

            // First we save the organelle data and then delete it
            data.ReplacedCytoplasm.Add(organelleHere);
            editedMicrobeOrganelles.Remove(organelleHere);
        }

        GD.Print("Placing organelle '", organelle.Definition.InternalName, "' at: ",
            organelle.Position);

        editedMicrobeOrganelles.Add(organelle);
    }

    [DeserializedCallbackAllowed]
    private void UndoOrganellePlaceAction(MicrobeEditorAction action)
    {
        var data = (PlacementActionData?)action.Data ??
            throw new Exception($"{nameof(UndoOrganellePlaceAction)} missing action data");

        editedMicrobeOrganelles.Remove(data.Organelle);

        if (data.ReplacedCytoplasm != null)
        {
            foreach (var cytoplasm in data.ReplacedCytoplasm)
            {
                GD.Print("Replacing ", cytoplasm.Definition.InternalName, " at: ",
                    cytoplasm.Position);

                editedMicrobeOrganelles.Add(cytoplasm);
            }
        }
    }

    private bool AddOrganelle(OrganelleTemplate organelle)
    {
        // 1 - you put a unique organelle (means only one instance allowed) but you already have it
        // 2 - you put an organelle that requires nucleus but you don't have one
        if ((organelle.Definition.Unique && HasOrganelle(organelle.Definition)) ||
            (organelle.Definition.RequiresNucleus && !HasNucleus))
            return false;

        organelle.PlacedThisSession = true;

        var action = new MicrobeEditorAction(this, organelle.Definition.MPCost,
            DoOrganellePlaceAction, UndoOrganellePlaceAction, new PlacementActionData(organelle));

        EnqueueAction(action);
        return true;
    }

    [DeserializedCallbackAllowed]
    private void DoOrganelleRemoveAction(MicrobeEditorAction action)
    {
        var data = (RemoveActionData?)action.Data ??
            throw new Exception($"{nameof(DoOrganelleRemoveAction)} missing action data");
        editedMicrobeOrganelles.Remove(data.Organelle);
    }

    [DeserializedCallbackAllowed]
    private void UndoOrganelleRemoveAction(MicrobeEditorAction action)
    {
        var data = (RemoveActionData?)action.Data ??
            throw new Exception($"{nameof(UndoOrganelleRemoveAction)} missing action data");
        editedMicrobeOrganelles.Add(data.Organelle);
    }

    [DeserializedCallbackAllowed]
    private void DoOrganelleMoveAction(MicrobeEditorAction action)
    {
        var data = (MoveActionData?)action.Data ??
            throw new Exception($"{nameof(DoOrganelleMoveAction)} missing action data");
        data.Organelle.Position = data.NewLocation;
        data.Organelle.Orientation = data.NewRotation;

        if (editedMicrobeOrganelles.Contains(data.Organelle))
        {
            UpdateAlreadyPlacedVisuals();

            // Organelle placement *might* affect auto-evo in the future so this is here for that reason
            StartAutoEvoPrediction();
        }
        else
        {
            editedMicrobeOrganelles.Add(data.Organelle);
        }

        ++data.Organelle.NumberOfTimesMoved;
    }

    [DeserializedCallbackAllowed]
    private void UndoOrganelleMoveAction(MicrobeEditorAction action)
    {
        var data = (MoveActionData?)action.Data ??
            throw new Exception($"{nameof(UndoOrganelleMoveAction)} missing action data");
        data.Organelle.Position = data.OldLocation;
        data.Organelle.Orientation = data.OldRotation;

        UpdateAlreadyPlacedVisuals();
        StartAutoEvoPrediction();

        --data.Organelle.NumberOfTimesMoved;
    }

    /// <summary>
    ///   Finishes an organelle move
    /// </summary>
    /// <returns>True if the organelle move succeeded.</returns>
    private bool MoveOrganelle(OrganelleTemplate organelle, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation)
    {
        // Make sure placement is valid
        if (!IsMoveTargetValid(newLocation, newRotation, organelle))
            return false;

        // If the organelle was already moved this session, added (placed) this session,
        // or not moved (but can be rotated), then moving it is free
        bool isFreeToMove = organelle.MovedThisSession || oldLocation == newLocation || organelle.PlacedThisSession;
        int cost = isFreeToMove ? 0 : Constants.ORGANELLE_MOVE_COST;

        // Too low mutation points, cancel move
        if (!isFreeToMove && MutationPoints < Constants.ORGANELLE_MOVE_COST)
        {
            CancelCurrentAction();
            GUI.OnInsufficientMp(false);
            return false;
        }

        // Don't register the action if the final location is the same as previous. This is so the player can't exploit
        // the MovedThisSession flag allowing them to freely move an organelle that was placed in another session
        // while on zero mutation points. Also it makes more sense to not count that organelle as moved either way.
        if (oldLocation == newLocation)
        {
            CancelCurrentAction();

            // Assume this is a successful move (some operation in the above call may be repeated)
            return true;
        }

        var action = new MicrobeEditorAction(this, cost,
            DoOrganelleMoveAction, UndoOrganelleMoveAction,
            new MoveActionData(organelle, oldLocation, newLocation, oldRotation, newRotation));

        EnqueueAction(action);

        // It's assumed that the above enqueue can't fail, otherwise the reference to MovingPlacedHex may be
        // permanently lost (as the code that calls this assumes it's safe to set MovingPlacedHex to null
        // when we return true)
        return true;
    }

    [DeserializedCallbackAllowed]
    private void DoNewMicrobeAction(MicrobeEditorAction action)
    {
        // TODO: could maybe grab the current organelles and put them in the action here? This could be more safe
        // against weird situations where it might be possible if the undo / redo system is changed to restore
        // the wrong organelles

        MutationPoints = Constants.BASE_MUTATION_POINTS;
        Membrane = SimulationParameters.Instance.GetMembrane("single");
        editedMicrobeOrganelles.Clear();
        editedMicrobeOrganelles.Add(new OrganelleTemplate(GetOrganelleDefinition("cytoplasm"),
            new Hex(0, 0), 0));

        OnPostNewMicrobeChange();
    }

    [DeserializedCallbackAllowed]
    private void UndoNewMicrobeAction(MicrobeEditorAction action)
    {
        var data = (NewMicrobeActionData?)action.Data ??
            throw new Exception($"{nameof(UndoNewMicrobeAction)} missing action data");

        editedMicrobeOrganelles.Clear();
        MutationPoints = data.PreviousMP;
        Membrane = data.OldMembrane;

        foreach (var organelle in data.OldEditedMicrobeOrganelles)
        {
            editedMicrobeOrganelles.Add(organelle);
        }

        OnPostNewMicrobeChange();
    }

    private void OnPostNewMicrobeChange()
    {
        GUI.UpdateMembraneButtons(Membrane.InternalName);
        GUI.UpdateSpeed(CalculateSpeed());
        GUI.UpdateHitpoints(CalculateHitpoints());

        StartAutoEvoPrediction();
    }

    [DeserializedCallbackAllowed]
    private void OnOrganelleAdded(OrganelleTemplate organelle)
    {
        organelleDataDirty = true;
        membraneOrganellePositionsAreDirty = true;
    }

    [DeserializedCallbackAllowed]
    private void OnOrganelleRemoved(OrganelleTemplate organelle)
    {
        organelleDataDirty = true;
        membraneOrganellePositionsAreDirty = true;
    }

    private void OnOrganellesChanged()
    {
        UpdateAlreadyPlacedVisuals();

        UpdateArrow();

        // Send to gui current status of cell
        GUI.UpdateSize(MicrobeHexSize);

        GUI.UpdatePartsAvailability(PlacedUniqueOrganelles.ToList());

        UpdatePatchDependentBalanceData();

        GUI.UpdateSpeed(CalculateSpeed());

        UpdateCellVisualization();

        StartAutoEvoPrediction();
    }

    private void UpdatePatchDependentBalanceData()
    {
        // Calculate and send energy balance to the GUI
        CalculateEnergyBalanceWithOrganellesAndMembraneType(
            editedMicrobeOrganelles.Organelles, Membrane, targetPatch);

        CalculateCompoundBalanceInPatch(editedMicrobeOrganelles.Organelles, targetPatch);
    }

    /// <summary>
    ///   This destroys and creates again entities to represent all
    ///   the currently placed organelles. Call this whenever
    ///   editedMicrobeOrganelles is changed.
    /// </summary>
    private void UpdateAlreadyPlacedVisuals()
    {
        int nextFreeHex = 0;
        int nextFreeOrganelle = 0;

        var islands = editedMicrobeOrganelles.GetIslandHexes();

        // Build the entities to show the current microbe
        foreach (var organelle in editedMicrobeOrganelles.Organelles)
        {
            foreach (var hex in organelle.RotatedHexes)
            {
                var pos = Hex.AxialToCartesian(hex + organelle.Position);

                if (nextFreeHex >= placedHexes.Count)
                {
                    // New hex needed
                    placedHexes.Add(CreateEditorHex());
                }

                var hexNode = placedHexes[nextFreeHex++];

                if (islands.Contains(organelle.Position))
                {
                    hexNode.MaterialOverride = islandMaterial;
                }
                else if (organelle.PlacedThisSession)
                {
                    hexNode.MaterialOverride = validMaterial;
                }
                else
                {
                    hexNode.MaterialOverride = oldMaterial;
                }

                // As we set the correct material, we don't need to remember to restore it anymore
                hoverOverriddenMaterials.Remove(hexNode);

                hexNode.Translation = pos;

                hexNode.Visible = !MicrobePreviewMode;
            }

            // Model of the organelle
            if (organelle.Definition.DisplayScene != null)
            {
                var pos = Hex.AxialToCartesian(organelle.Position) +
                    organelle.Definition.CalculateModelOffset();

                if (nextFreeOrganelle >= placedModels.Count)
                {
                    // New organelle model needed
                    placedModels.Add(CreatePreviewModelHolder());
                }

                var organelleModel = placedModels[nextFreeOrganelle++];

                organelleModel.Transform = new Transform(
                    MathUtils.CreateRotationForOrganelle(1 * organelle.Orientation), pos);

                organelleModel.Scale = new Vector3(Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
                    Constants.DEFAULT_HEX_SIZE);

                organelleModel.Visible = !MicrobePreviewMode;

                UpdateOrganellePlaceHolderScene(organelleModel,
                    organelle.Definition.DisplayScene, organelle.Definition, Hex.GetRenderPriority(organelle.Position));
            }
        }

        // Delete excess entities
        while (nextFreeHex < placedHexes.Count)
        {
            placedHexes[placedHexes.Count - 1].DetachAndQueueFree();
            placedHexes.RemoveAt(placedHexes.Count - 1);
        }

        while (nextFreeOrganelle < placedModels.Count)
        {
            placedModels[placedModels.Count - 1].DetachAndQueueFree();
            placedModels.RemoveAt(placedModels.Count - 1);
        }
    }

    /// <summary>
    ///   Updates the organelle model displayer to have the specified scene in it
    /// </summary>
    private void UpdateOrganellePlaceHolderScene(SceneDisplayer organelleModel,
        string displayScene, OrganelleDefinition definition, int renderPriority)
    {
        organelleModel.Scene = displayScene;
        var material = organelleModel.GetMaterial(definition.DisplaySceneModelPath);
        if (material != null)
        {
            material.RenderPriority = renderPriority;
        }
    }

    [DeserializedCallbackAllowed]
    private void DoMembraneChangeAction(MicrobeEditorAction action)
    {
        var data = (MembraneActionData?)action.Data ??
            throw new Exception($"{nameof(DoMembraneChangeAction)} missing action data");

        var membrane = data.NewMembrane;
        GD.Print("Changing membrane to '", membrane.InternalName, "'");
        Membrane = membrane;
        GUI.UpdateMembraneButtons(Membrane.InternalName);
        GUI.UpdateSpeed(CalculateSpeed());
        GUI.UpdateHitpoints(CalculateHitpoints());
        CalculateEnergyBalanceWithOrganellesAndMembraneType(
            editedMicrobeOrganelles.Organelles, Membrane, targetPatch);
        GUI.SetMembraneTooltips(Membrane);

        StartAutoEvoPrediction();

        if (previewMicrobe != null)
        {
            previewMicrobe.Membrane.Type = membrane;
            previewMicrobe.Membrane.Dirty = true;
            previewMicrobe.ApplyMembraneWigglyness();
        }
    }

    [DeserializedCallbackAllowed]
    private void UndoMembraneChangeAction(MicrobeEditorAction action)
    {
        var data = (MembraneActionData?)action.Data ??
            throw new Exception($"{nameof(UndoMembraneChangeAction)} missing action data");
        Membrane = data.OldMembrane;
        GD.Print("Changing membrane back to '", Membrane.InternalName, "'");
        GUI.UpdateMembraneButtons(Membrane.InternalName);
        GUI.UpdateSpeed(CalculateSpeed());
        GUI.UpdateHitpoints(CalculateHitpoints());
        CalculateEnergyBalanceWithOrganellesAndMembraneType(
            editedMicrobeOrganelles.Organelles, Membrane, targetPatch);
        GUI.SetMembraneTooltips(Membrane);

        StartAutoEvoPrediction();

        if (previewMicrobe != null)
        {
            previewMicrobe.Membrane.Type = Membrane;
            previewMicrobe.Membrane.Dirty = true;
            previewMicrobe.ApplyMembraneWigglyness();
        }
    }

    [DeserializedCallbackAllowed]
    private void DoBehaviourChangeAction(MicrobeEditorAction action)
    {
        var data = (BehaviourChangeActionData?)action.Data ??
            throw new Exception($"{nameof(DoBehaviourChangeAction)} missing action data");

        if (Behaviour == null)
            throw new InvalidOperationException($"Editor has no {nameof(Behaviour)} set for change action to use");

        Behaviour[data.Type] = data.NewValue;
        GUI.UpdateBehaviourSlider(data.Type, data.NewValue);
    }

    [DeserializedCallbackAllowed]
    private void UndoBehaviourChangeAction(MicrobeEditorAction action)
    {
        var data = (BehaviourChangeActionData?)action.Data ??
            throw new Exception($"{nameof(UndoBehaviourChangeAction)} missing action data");

        if (Behaviour == null)
            throw new InvalidOperationException($"Editor has no {nameof(Behaviour)} set for change action to use");

        Behaviour[data.Type] = data.OldValue;
        GUI.UpdateBehaviourSlider(data.Type, data.OldValue);
    }

    [DeserializedCallbackAllowed]
    private void DoRigidityChangeAction(MicrobeEditorAction action)
    {
        var data = (RigidityChangeActionData?)action.Data ??
            throw new Exception($"{nameof(DoRigidityChangeAction)} missing action data");

        Rigidity = data.NewRigidity;

        // TODO: when rigidity affects auto-evo this also needs to re-run the prediction, though there should probably
        // be some kind of throttling, this also applies to the behaviour values

        OnRigidityChanged();
    }

    [DeserializedCallbackAllowed]
    private void UndoRigidityChangeAction(MicrobeEditorAction action)
    {
        var data = (RigidityChangeActionData?)action.Data ??
            throw new Exception($"{nameof(UndoRigidityChangeAction)} missing action data");

        Rigidity = data.PreviousRigidity;
        OnRigidityChanged();
    }

    private void OnRigidityChanged()
    {
        GUI.UpdateRigiditySlider((int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO));

        GUI.UpdateSpeed(CalculateSpeed());
        GUI.UpdateHitpoints(CalculateHitpoints());
    }

    private void SaveGame(string name)
    {
        SaveHelper.Save(name, this);
    }
}
