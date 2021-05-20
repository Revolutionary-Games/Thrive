﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main class of the microbe editor
/// </summary>
[JsonObject(IsReference = true)]
[SceneLoadedClass("res://src/microbe_stage/editor/MicrobeEditor.tscn")]
[DeserializedCallbackTarget]
public class MicrobeEditor : NodeWithInput, ILoadableGameState, IGodotEarlyNodeResolve
{
    [Export]
    public NodePath PauseMenuPath;

    /// <summary>
    ///   The new to set on the species after exiting
    /// </summary>
    [JsonProperty]
    public string NewName;

    private MicrobeSymmetry symmetry = MicrobeSymmetry.None;

    /// <summary>
    ///   Object camera is over. Needs to be defined before camera for saving to work
    /// </summary>
    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private Spatial cameraFollow;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private MicrobeCamera camera;

    [JsonIgnore]
    private MeshInstance editorArrow;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private MicrobeEditorGUI gui;

    private Node world;
    private MicrobeEditorTutorialGUI tutorialGUI;
    private PauseMenu pauseMenu;

    /// <summary>
    ///   Where all user actions will  be registered
    /// </summary>
    [JsonProperty]
    private ActionHistory<MicrobeEditorAction> history;

    private Material invalidMaterial;
    private Material validMaterial;
    private Material oldMaterial;
    private Material islandMaterial;

    private PackedScene hexScene;
    private PackedScene modelScene;

    /// <summary>
    ///   Where the player wants to move after editing
    /// </summary>
    [JsonProperty]
    private Patch targetPatch;

    /// <summary>
    ///   When false the player is no longer allowed to move patches (other than going back to where they were at the
    ///   start)
    /// </summary>
    [JsonProperty]
    private bool canStillMove;

    [JsonProperty]
    private Patch playerPatchOnEntry;

    /// <summary>
    ///   The hexes that are positioned under the cursor to show where
    ///   the player is about to place something.
    /// </summary>
    private List<MeshInstance> hoverHexes;

    /// <summary>
    ///   The organelle models that are positioned to show what the
    ///   player is about to place.
    /// </summary>
    private List<SceneDisplayer> hoverOrganelles;

    /// <summary>
    ///   This is used to keep track of used hover organelles
    /// </summary>
    private int usedHoverHex;

    private int usedHoverOrganelle;

    /// <summary>
    ///   The species that is being edited, changes are applied to it on exit
    /// </summary>
    [JsonProperty]
    private MicrobeSpecies editedSpecies;

    /// <summary>
    ///   This is a global assessment if the currently being placed
    ///   organelle is valid (if not all hover hexes will be shown as
    ///   invalid)
    /// </summary>
    private bool isPlacementProbablyValid;

    /// <summary>
    ///   This is the container that has the edited organelles in
    ///   it. This is populated when entering and used to update the
    ///   player's species template on exit.
    /// </summary>
    [JsonProperty]
    private OrganelleLayout<OrganelleTemplate> editedMicrobeOrganelles;

    /// <summary>
    ///   When this is true, on next process this will handle added and removed organelles and update stats etc.
    ///   This is done to make adding a bunch of organelles at once more efficient.
    /// </summary>
    private bool organelleDataDirty = true;

    // This is the already placed hexes

    /// <summary>
    ///   This is the hexes for editedMicrobeOrganelles
    /// </summary>
    private List<MeshInstance> placedHexes;

    /// <summary>
    ///   The hexes that have been changed by a hovering organelle and need to be reset to old material.
    /// </summary>
    private Dictionary<MeshInstance, Material> hoverOverriddenMaterials;

    /// <summary>
    ///   This is the organelle models for editedMicrobeOrganelles
    /// </summary>
    private List<SceneDisplayer> placedModels;

    /// <summary>
    ///   True once fade transition is finished when entering editor
    /// </summary>
    private bool transitionFinished;

    /// <summary>
    ///   True once auto-evo (and possibly other stuff) we need to wait for is ready
    /// </summary>
    [JsonProperty]
    private bool ready;

    [JsonProperty]
    private int organelleRot;

    [JsonProperty]
    private string autoEvoSummary;

    [JsonProperty]
    private string autoEvoExternal;

    [JsonProperty]
    private string activeActionName;

    /// <summary>
    ///   Where the user started panning with the mouse
    ///   Null if the user is not panning with the mouse
    /// </summary>
    private Vector3? mousePanningStart;

    /// <summary>
    /// The Symmetry setting of the Microbe Editor.
    /// </summary>
    public enum MicrobeSymmetry
    {
        /// <summary>
        /// No symmetry in the editor.
        /// </summary>
        None,

        /// <summary>
        /// Symmetry across the X-Axis in the editor.
        /// </summary>
        XAxisSymmetry,

        /// <summary>
        /// Symmetry across both the X and the Y axis in the editor.
        /// </summary>
        FourWaySymmetry,

        /// <summary>
        /// Symmetry across the X and Y axis, as well as across center, in the editor.
        /// </summary>
        SixWaySymmetry,
    }

    public bool NodeReferencesResolved { get; private set; }

    [JsonIgnore]
    public MicrobeCamera Camera => camera;

    /// <summary>
    ///   The selected membrane rigidity
    /// </summary>
    [JsonProperty]
    public float Rigidity { get; private set; }

    /// <summary>
    ///   Selected membrane type for the species
    /// </summary>
    [JsonProperty]
    public MembraneType Membrane { get; private set; }

    /// <summary>
    ///   Current selected colour for the species.
    /// </summary>
    [JsonProperty]
    public Color Colour { get; set; }

    /// <summary>
    ///   The name of organelle type that is selected to be placed
    /// </summary>
    [JsonIgnore]
    public string ActiveActionName
    {
        get => activeActionName;
        set
        {
            if (value != activeActionName)
            {
                TutorialState?.SendEvent(TutorialEventType.MicrobeEditorOrganelleToPlaceChanged,
                    new StringEventArgs(value), this);
            }

            activeActionName = value;
        }
    }

    /// <summary>
    ///   The number of mutation points left
    /// </summary>
    [JsonProperty]
    public int MutationPoints { get; private set; }

    /// <summary>
    ///   The symmetry setting of the microbe editor.
    /// </summary>
    public MicrobeSymmetry Symmetry
    {
        get => symmetry;
        set => symmetry = value;
    }

    /// <summary>
    ///   Organelle that is in the process of being moved but a new location hasn't been selected yet
    ///   If null, no organelle is in the process of moving
    /// </summary>
    [JsonProperty]
    public OrganelleTemplate MovingOrganelle { get; private set; }

    /// <summary>
    ///   When true nothing costs MP
    /// </summary>
    [JsonProperty]
    public bool FreeBuilding { get; private set; }

    /// <summary>
    ///   Hover hexes and models are only shown if this is true. This is saved to make this work better when the player
    ///   was in the cell editor tab and saved.
    /// </summary>
    public bool ShowHover { get; set; }

    /// <summary>
    ///   The main current game object holding various details
    /// </summary>
    [JsonProperty]
    public GameProperties CurrentGame { get; set; }

    [JsonIgnore]
    public TutorialState TutorialState => CurrentGame.TutorialState;

    /// <summary>
    ///   If set the editor returns to this stage. The CurrentGame
    ///   should be shared with this stage. If not set returns to a new microbe stage
    /// </summary>
    [JsonProperty]
    public MicrobeStage ReturnToStage { get; set; }

    [JsonIgnore]
    public bool HasNucleus
    {
        get
        {
            foreach (var organelle in editedMicrobeOrganelles.Organelles)
            {
                if (organelle.Definition.InternalName == "nucleus")
                    return true;
            }

            return false;
        }
    }

    [JsonIgnore]
    public bool HasIslands => editedMicrobeOrganelles.GetIslandHexes().Count > 0;

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

    /// <summary>
    ///   Returns the current patch the player is in
    /// </summary>
    [JsonIgnore]
    public Patch CurrentPatch => targetPatch ?? playerPatchOnEntry;

    /// <summary>
    ///   If true an editor action is active and can be cancelled. Currently only checks for organelle move.
    /// </summary>
    [JsonIgnore]
    public bool CanCancelAction => MovingOrganelle != null;

    [JsonIgnore]
    public Node GameStateRoot => this;

    public bool IsLoadedFromSave { get; set; }

    public override void _Ready()
    {
        ResolveNodeReferences();

        invalidMaterial = GD.Load<Material>(
            "res://src/microbe_stage/editor/InvalidHex.material");
        validMaterial = GD.Load<Material>("res://src/microbe_stage/editor/ValidHex.material");
        oldMaterial = GD.Load<Material>("res://src/microbe_stage/editor/OldHex.material");
        islandMaterial = GD.Load<Material>("res://src/microbe_stage/editor/IslandHex.material");

        hexScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/EditorHex.tscn");
        modelScene = GD.Load<PackedScene>("res://src/general/SceneDisplayer.tscn");

        if (!IsLoadedFromSave)
            camera.ObjectToFollow = cameraFollow;

        tutorialGUI.Visible = true;
        gui.Init(this);

        transitionFinished = false;

        OnEnterEditor();
    }

    public void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        NodeReferencesResolved = true;

        camera = GetNode<MicrobeCamera>("PrimaryCamera");
        editorArrow = GetNode<MeshInstance>("EditorArrow");
        cameraFollow = GetNode<Spatial>("CameraLookAt");
        world = GetNode("World");
        gui = GetNode<MicrobeEditorGUI>("MicrobeEditorGUI");
        tutorialGUI = GetNode<MicrobeEditorTutorialGUI>("TutorialGUI");
        pauseMenu = GetNode<PauseMenu>(PauseMenuPath);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        // As we will no longer return to the microbe stage we need to free it, if we have it
        // This might be disposed if this was loaded from a save and we loaded another save
        try
        {
            if (IsLoadedFromSave)
            {
                // When loaded from save, the stage needs to be attached as a scene for the callbacks that reattach
                // children to run, otherwise some objects won't be correctly deleted
                if (ReturnToStage != null)
                    SceneManager.Instance.AttachAndDetachScene(ReturnToStage);
            }

            if (ReturnToStage != null)
            {
                if (ReturnToStage.GetParent() != null)
                    GD.PrintErr("ReturnToStage has a parent when editor is wanting to free it");

                ReturnToStage.QueueFree();
            }
        }
        catch (ObjectDisposedException)
        {
            GD.Print("Editor's return to stage is already disposed");
        }
    }

    public void OnFinishLoading(Save save)
    {
        // // Handle the stage to return to specially, as it also needs to run the code
        // // for fixing the stuff in order to return there
        // // TODO: this could be probably moved now to just happen when it enters the scene first time

        ReturnToStage?.OnFinishLoading();

        // Probably shouldn't be needed as the stage object is not orphaned automatically
        // // We need to not let the objects be deleted before we apply them
        // TemporaryLoadedNodeDeleter.Instance.AddDeletionHold(Constants.DELETION_HOLD_MICROBE_EDITOR);
    }

    public void OnFinishTransitioning()
    {
        transitionFinished = true;
    }

    /// <summary>
    ///   Applies the changes done and exits the editor
    /// </summary>
    public void OnFinishEditing()
    {
        GD.Print("MicrobeEditor: applying changes to edited Species");

        if (ReturnToStage == null)
        {
            GD.Print("Creating new microbe stage as there isn't one yet");

            var scene = SceneManager.Instance.LoadScene(MainGameState.MicrobeStage);

            ReturnToStage = (MicrobeStage)scene.Instance();
            ReturnToStage.CurrentGame = CurrentGame;
        }

        // Apply changes to the species organelles

        // It is easiest to just replace all
        editedSpecies.Organelles.Clear();

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

        GD.Print("MicrobeEditor: updated organelles for species: ",
            editedSpecies.FormattedName);

        // Update name
        var splits = NewName.Split(" ");
        if (splits.Length == 2)
        {
            editedSpecies.Genus = splits[0];
            editedSpecies.Epithet = splits[1];

            GD.Print("MicrobeEditor: edited species name is now ",
                editedSpecies.FormattedName);
        }
        else
        {
            GD.PrintErr("MicrobeEditor: Invalid newName: ", NewName);
        }

        // Update membrane
        editedSpecies.MembraneType = Membrane;
        editedSpecies.Colour = Colour;
        editedSpecies.MembraneRigidity = Rigidity;

        // Move patches
        if (targetPatch != null)
        {
            GD.Print("MicrobeEditor: applying player move to patch: ", TranslationServer.Translate(targetPatch.Name));
            CurrentGame.GameWorld.Map.CurrentPatch = targetPatch;

            // Add the edited species to that patch to allow the species to gain population there
            CurrentGame.GameWorld.Map.CurrentPatch.AddSpecies(editedSpecies, 0);
        }

        var stage = ReturnToStage;

        // This needs to be reset here to not free this when we exit the tree
        ReturnToStage = null;

        SceneManager.Instance.SwitchToScene(stage);

        stage.OnReturnFromEditor();
    }

    public override void _Process(float delta)
    {
        if (!ready)
        {
            if (!CurrentGame.GameWorld.IsAutoEvoFinished())
            {
                LoadingScreen.Instance.Show(TranslationServer.Translate("LOADING_MICROBE_EDITOR"),
                    MainGameState.MicrobeEditor,
                    TranslationServer.Translate("WAITING_FOR_AUTO_EVO") + " " +
                    CurrentGame.GameWorld.GetAutoEvoRun().Status);
                return;
            }

            if (!transitionFinished)
                return;

            OnEditorReady();
        }

        UpdateEditor(delta);
    }

    public override void _Notification(int what)
    {
        // Rebuilds and recalculates all value dependent UI elements on language change
        if (what == NotificationTranslationChanged)
        {
            CalculateOrganelleEffectivenessInPatch(CurrentPatch);
            UpdatePatchDependentBalanceData();
            gui.UpdateTimeIndicator(CurrentGame.GameWorld.TotalPassedTime);
            gui.UpdateGlucoseReduction(Constants.GLUCOSE_REDUCTION_RATE);
            gui.UpdatePatchDetails(CurrentPatch);
            gui.UpdateMicrobePartSelections();

            // TODO: AutoEvo run results summary
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

    [RunOnAxisGroup]
    [RunOnAxis(new[] { "e_pan_up", "e_pan_down" }, new[] { -1.0f, 1.0f })]
    [RunOnAxis(new[] { "e_pan_left", "e_pan_right" }, new[] { -1.0f, 1.0f })]
    public void PanCameraWithKeys(float delta, float upDown, float leftRight)
    {
        if (mousePanningStart != null)
            return;

        var movement = new Vector3(leftRight, 0, upDown);
        MoveObjectToFollow(movement.Normalized() * delta * Camera.CameraHeight);
    }

    [RunOnKey("e_pan_mouse", CallbackRequiresElapsedTime = false)]
    public bool PanCameraWithMouse(float delta)
    {
        if (mousePanningStart == null)
        {
            mousePanningStart = Camera.CursorWorldPos;
        }
        else
        {
            var mousePanDirection = mousePanningStart.Value - Camera.CursorWorldPos;
            MoveObjectToFollow(mousePanDirection * delta * 10);
        }

        return false;
    }

    [RunOnKeyUp("e_pan_mouse")]
    public void ReleasePanCameraWithMouse()
    {
        mousePanningStart = null;
    }

    [RunOnKeyDown("e_reset_camera")]
    public void ResetCamera()
    {
        camera.ObjectToFollow.Translation = new Vector3(0, 0, 0);
        camera.ResetHeight();
    }

    [RunOnKeyDown("g_quick_save")]
    public void QuickSave()
    {
        // Can only save once the editor is ready
        if (ready)
        {
            GD.Print("quick saving microbe editor");
            SaveHelper.QuickSave(this);
        }
    }

    [RunOnKeyDown("e_redo")]
    public void Redo()
    {
        if (history.Redo())
        {
            TutorialState.SendEvent(TutorialEventType.MicrobeEditorRedo, EventArgs.Empty, this);
        }

        UpdateUndoRedoButtons();
    }

    [RunOnKeyDown("e_undo")]
    public void Undo()
    {
        if (history.Undo())
        {
            TutorialState.SendEvent(TutorialEventType.MicrobeEditorUndo, EventArgs.Empty, this);
        }

        UpdateUndoRedoButtons();
    }

    [RunOnKeyDown("e_primary")]
    public void PlaceOrganelle()
    {
        if (MovingOrganelle != null)
        {
            GetMouseHex(out int q, out int r);
            if (MoveOrganelle(MovingOrganelle, MovingOrganelle.Position, new Hex(q, r), MovingOrganelle.Orientation,
                organelleRot))
            {
                // Move succeeded; Update the cancel button visibility so it's hidden because the move has completed
                MovingOrganelle = null;
                gui.UpdateCancelButtonVisibility();

                // Update rigidity slider in case it was disabled
                // TODO: could come up with a bit nicer design here
                int intRigidity = (int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO);
                gui.UpdateRigiditySlider(intRigidity, MutationPoints);
            }
            else
            {
                gui.PlayInvalidActionSound();
            }

            return;
        }

        if (ActiveActionName == null)
            return;

        if (AddOrganelle(ActiveActionName))
        {
            // Only trigger tutorial if an organelle was really placed
            TutorialState.SendEvent(TutorialEventType.MicrobeEditorOrganellePlaced, EventArgs.Empty, this);
        }
    }

    [RunOnKeyDown("e_rotate_right")]
    public void RotateRight()
    {
        organelleRot = (organelleRot + 1) % 6;
    }

    [RunOnKeyDown("e_rotate_left")]
    public void RotateLeft()
    {
        --organelleRot;

        if (organelleRot < 0)
            organelleRot = 5;
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
        gui.UpdateMembraneButtons(Membrane.InternalName);
    }

    public void SetRigidity(int rigidity)
    {
        int intRigidity = (int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO);

        if (MovingOrganelle != null)
        {
            gui.OnActionBlockedWhileMoving();
            gui.UpdateRigiditySlider(intRigidity, MutationPoints);
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
                gui.UpdateRigiditySlider(intRigidity, MutationPoints);
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
        // Can't open organelle popup menu while moving something
        if (MovingOrganelle != null)
        {
            gui.OnActionBlockedWhileMoving();
            return;
        }

        GetMouseHex(out int q, out int r);

        var organelle = editedMicrobeOrganelles.GetOrganelleAt(new Hex(q, r));

        if (organelle == null)
            return;

        gui.ShowOrganelleMenu(organelle);
    }

    public void StartOrganelleMove(OrganelleTemplate selectedOrganelle)
    {
        if (MovingOrganelle != null)
        {
            // Already moving something! some code went wrong
            throw new InvalidOperationException("Can't begin organelle move while another in progress");
        }

        MovingOrganelle = selectedOrganelle;
        editedMicrobeOrganelles.Remove(MovingOrganelle);
    }

    /// <summary>
    ///   Cancels the current editor action
    /// </summary>
    /// <returns>True when the input is consumed</returns>
    [RunOnKeyDown("e_cancel_current_action", Priority = 1)]
    public bool CancelCurrentAction()
    {
        if (MovingOrganelle != null)
        {
            editedMicrobeOrganelles.Add(MovingOrganelle);
            MovingOrganelle = null;
            gui.UpdateCancelButtonVisibility();
            return true;
        }

        return false;
    }

    public void RemoveOrganelle(Hex hex)
    {
        int q = hex.Q;
        int r = hex.R;

        switch (Symmetry)
        {
            case MicrobeSymmetry.None:
            {
                RemoveOrganelleAt(new Hex(q, r));
                break;
            }

            case MicrobeSymmetry.XAxisSymmetry:
            {
                RemoveOrganelleAt(new Hex(q, r));

                if (q != -1 * q || r != r + q)
                {
                    RemoveOrganelleAt(new Hex(-1 * q, r + q));
                }

                break;
            }

            case MicrobeSymmetry.FourWaySymmetry:
            {
                RemoveOrganelleAt(new Hex(q, r));

                if (q != -1 * q || r != r + q)
                {
                    RemoveOrganelleAt(new Hex(-1 * q, r + q));
                    RemoveOrganelleAt(new Hex(-1 * q, -1 * r));
                    RemoveOrganelleAt(new Hex(q, -1 * (r + q)));
                }
                else
                {
                    RemoveOrganelleAt(new Hex(-1 * q, -1 * r));
                }

                break;
            }

            case MicrobeSymmetry.SixWaySymmetry:
            {
                RemoveOrganelleAt(new Hex(q, r));

                RemoveOrganelleAt(new Hex(-1 * r, r + q));
                RemoveOrganelleAt(new Hex(-1 * (r + q), q));
                RemoveOrganelleAt(new Hex(-1 * q, -1 * r));
                RemoveOrganelleAt(new Hex(r, -1 * (r + q)));
                RemoveOrganelleAt(new Hex(r, -1 * (r + q)));
                RemoveOrganelleAt(new Hex(r + q, -1 * q));

                break;
            }
        }
    }

    public float CalculateSpeed()
    {
        float microbeMass = Constants.MICROBE_BASE_MASS;

        float organelleMovementForce = 0;

        Vector3 forwardsDirection = new Vector3(0, 0, -1);

        foreach (var organelle in editedMicrobeOrganelles.Organelles)
        {
            microbeMass += organelle.Definition.Mass;

            if (organelle.Definition.HasComponentFactory<MovementComponentFactory>())
            {
                Vector3 organelleDirection = (Hex.AxialToCartesian(new Hex(0, 0))
                    - Hex.AxialToCartesian(organelle.Position)).Normalized();

                float directionFactor = organelleDirection.Dot(forwardsDirection);

                // Flagella pointing backwards don't slow you down
                directionFactor = Math.Max(directionFactor, 0);

                organelleMovementForce += Constants.FLAGELLA_BASE_FORCE
                    * organelle.Definition.Components.Movement.Momentum / 100.0f
                    * directionFactor;
            }
        }

        float baseMovementForce = Constants.CELL_BASE_THRUST *
            (Membrane.MovementFactor - Rigidity * Constants.MEMBRANE_RIGIDITY_MOBILITY_MODIFIER);

        float finalSpeed = (baseMovementForce + organelleMovementForce) / microbeMass;

        return finalSpeed;
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
    public float CalculateCurrentOrganelleCost()
    {
        if (string.IsNullOrEmpty(ActiveActionName) || !ShowHover)
            return 0;

        var cost = SimulationParameters.Instance.GetOrganelleType(ActiveActionName).MPCost;

        switch (Symmetry)
        {
            case MicrobeSymmetry.XAxisSymmetry:
                cost *= 2;
                break;
            case MicrobeSymmetry.FourWaySymmetry:
                cost *= 4;
                break;
            case MicrobeSymmetry.SixWaySymmetry:
                cost *= 6;
                break;
        }

        return cost;
    }

    /// <summary>
    ///   Returns true when the player is allowed to move to the specified patch
    /// </summary>
    /// <returns>True if the patch move requested is valid. False otherwise</returns>
    public bool IsPatchMoveValid(Patch patch)
    {
        if (patch == null)
            return false;

        var from = CurrentPatch;

        // Can't go to the patch you are in
        if (from == patch)
            return false;

        // Can return to the patch the player started in, as a way to "undo" the change
        if (patch == playerPatchOnEntry)
            return true;

        // Can't move if out of moves
        if (!canStillMove)
            return false;

        // Need to have a connection to move
        foreach (var adjacent in from.Adjacent)
        {
            if (adjacent == patch)
                return true;
        }

        return false;
    }

    public void SetPlayerPatch(Patch patch)
    {
        if (!IsPatchMoveValid(patch))
            throw new ArgumentException("can't move to the specified patch");

        // One move per editor cycle allowed, unless freebuilding
        if (!FreeBuilding)
            canStillMove = false;

        if (patch == playerPatchOnEntry)
        {
            targetPatch = null;

            // Undoing the move, restores the move
            canStillMove = true;
        }
        else
        {
            targetPatch = patch;
        }

        gui.UpdatePlayerPatch(targetPatch);
        UpdatePatchBackgroundImage();
        CalculateOrganelleEffectivenessInPatch(targetPatch);
        UpdatePatchDependentBalanceData();
    }

    /// <summary>
    ///   Changes the number of mutation points left. Should only be called by MicrobeEditorAction
    /// </summary>
    internal void ChangeMutationPoints(int change)
    {
        if (FreeBuilding || CheatManager.InfiniteMP)
            return;

        MutationPoints = (MutationPoints + change).Clamp(0, Constants.BASE_MUTATION_POINTS);

        gui.UpdateMutationPointsBar();
    }

    /// <summary>
    ///   Moves the ObjectToFollow of the camera in a direction
    /// </summary>
    /// <param name="vector">The direction to move the camera</param>
    private void MoveObjectToFollow(Vector3 vector)
    {
        cameraFollow.Translation += vector;
    }

    /// <summary>
    ///   Sets up the editor when entering
    /// </summary>
    private void OnEnterEditor()
    {
        // Clear old stuff in the world
        world.FreeChildren();

        hoverHexes = new List<MeshInstance>();
        hoverOrganelles = new List<SceneDisplayer>();
        hoverOverriddenMaterials = new Dictionary<MeshInstance, Material>();

        // Create new hover hexes. See the TODO comment in _Process
        // This seems really cluttered, there must be a better way.
        for (int i = 0; i < Constants.MAX_HOVER_HEXES; ++i)
        {
            hoverHexes.Add(CreateEditorHex());
        }

        for (int i = 0; i < Constants.MAX_SYMMETRY; ++i)
        {
            hoverOrganelles.Add(CreateEditorOrganelle());
        }

        if (!IsLoadedFromSave)
        {
            history = new ActionHistory<MicrobeEditorAction>();

            // Start a new game if no game has been started
            if (CurrentGame == null)
            {
                if (ReturnToStage != null)
                    throw new Exception("stage to return to should have set our current game");

                GD.Print("Starting a new game for the microbe editor");
                CurrentGame = GameProperties.StartNewMicrobeGame();
            }
        }

        InitEditor();

        StartMusic();

        if (!IsLoadedFromSave)
            TutorialState.SendEvent(TutorialEventType.EnteredMicrobeEditor, EventArgs.Empty, this);
    }

    private void StartMusic()
    {
        Jukebox.Instance.PlayingCategory = "MicrobeEditor";
        Jukebox.Instance.Resume();
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
                if (absoluteHex.Q < -1 || absoluteHex.Q > 1)
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
    ///   Calculates the effectiveness of organelles in the current or
    ///   given patch
    /// </summary>
    private void CalculateOrganelleEffectivenessInPatch(Patch patch = null)
    {
        if (patch == null)
        {
            patch = CurrentPatch;
        }

        var organelles = SimulationParameters.Instance.GetAllOrganelles();

        var result = ProcessSystem.ComputeOrganelleProcessEfficiencies(organelles, patch.Biome);

        gui.UpdateOrganelleEfficiencies(result);
    }

    /// <summary>
    ///   Calculates the energy balance for a cell with the given organelles
    /// </summary>
    private void CalculateEnergyBalanceWithOrganellesAndMembraneType(List<OrganelleTemplate> organelles,
        MembraneType membrane, Patch patch = null)
    {
        if (patch == null)
        {
            patch = CurrentPatch;
        }

        gui.UpdateEnergyBalance(
            ProcessSystem.ComputeEnergyBalance(organelles.Select(i => i.Definition), patch.Biome, membrane));
    }

    private void CalculateCompoundBalanceInPatch(List<OrganelleTemplate> organelles, Patch patch = null)
    {
        patch ??= CurrentPatch;

        var result = ProcessSystem.ComputeCompoundBalance(organelles.Select(i => i.Definition), patch.Biome);

        gui.UpdateCompoundBalances(result);
    }

    /// <summary>
    ///   Combined old editor init and activate method
    /// </summary>
    private void InitEditor()
    {
        // The world is reset each time so these are gone
        placedHexes = new List<MeshInstance>();
        placedModels = new List<SceneDisplayer>();

        if (!IsLoadedFromSave)
        {
            InitEditorFresh();

            Symmetry = 0;
            gui.ResetSymmetryButton();
        }
        else
        {
            InitEditorSaved();

            gui.SetSymmetry(Symmetry);
            gui.UpdatePlayerPatch(targetPatch);
        }

        UpdateUndoRedoButtons();

        UpdateArrow(false);

        // Send freebuild value to GUI
        gui.NotifyFreebuild(FreeBuilding);

        // Send info to the GUI about the organelle effectiveness in the current patch
        CalculateOrganelleEffectivenessInPatch(CurrentPatch);

        UpdatePatchBackgroundImage();

        gui.SetMap(CurrentGame.GameWorld.Map);

        gui.UpdateGlucoseReduction(Constants.GLUCOSE_REDUCTION_RATE);

        gui.UpdateReportTabPatchName(TranslationServer.Translate(CurrentPatch.Name));

        // Make tutorials run
        tutorialGUI.EventReceiver = TutorialState;
        pauseMenu.GameProperties = CurrentGame;

        // Send undo button to the tutorial system
        gui.SendUndoToTutorial(TutorialState);

        gui.UpdateCancelButtonVisibility();
    }

    private void InitEditorFresh()
    {
        // For now we only show a loading screen if auto-evo is not ready yet
        if (!CurrentGame.GameWorld.IsAutoEvoFinished())
        {
            ready = false;
            LoadingScreen.Instance.Show(TranslationServer.Translate("LOADING_MICROBE_EDITOR"),
                MainGameState.MicrobeEditor,
                CurrentGame.GameWorld.GetAutoEvoRun().Status);
        }
        else if (!transitionFinished)
        {
            ready = false;
        }
        else
        {
            OnEditorReady();
        }

        CurrentGame.SetBool("edited_microbe", true);

        if (CurrentGame.FreeBuild)
        {
            GD.Print("Editor going to freebuild mode because player has activated freebuild");
            FreeBuilding = true;
        }
        else
        {
            // Make sure freebuilding doesn't get stuck on
            FreeBuilding = false;
        }

        MutationPoints = Constants.BASE_MUTATION_POINTS;
        editedMicrobeOrganelles = new OrganelleLayout<OrganelleTemplate>(
            OnOrganelleAdded, OnOrganelleRemoved);

        organelleRot = 0;

        targetPatch = null;

        playerPatchOnEntry = CurrentGame.GameWorld.Map.CurrentPatch;

        canStillMove = true;

        var playerSpecies = CurrentGame.GameWorld.PlayerSpecies;

        SetupEditedSpecies(playerSpecies as MicrobeSpecies);
    }

    private void InitEditorSaved()
    {
        UpdateGUIAfterLoadingSpecies(editedSpecies);
        OnLoadedEditorReady();
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

        // Get the species organelles to be edited. This also updates the placeholder hexes
        foreach (var organelle in species.Organelles.Organelles)
        {
            editedMicrobeOrganelles.Add((OrganelleTemplate)organelle.Clone());
        }

        // Create a mutated version of the current species code to compete against the player
        CreateMutatedSpeciesCopy(species);

        NewName = species.FormattedName;

        species.Generation += 1;

        // Only when not loaded from save are these properties fetched
        gui.SetInitialCellStats();

        UpdateGUIAfterLoadingSpecies(species);
    }

    private void UpdateGUIAfterLoadingSpecies(MicrobeSpecies species)
    {
        GD.Print("Starting microbe editor with: ", editedMicrobeOrganelles.Organelles.Count,
            " organelles in the microbe");

        // Update GUI buttons now that we have correct organelles
        gui.UpdatePartsAvailability(HasNucleus);

        // Reset to cytoplasm if nothing is selected
        if (ActiveActionName == null)
        {
            gui.OnOrganelleToPlaceSelected("cytoplasm");
        }
        else
        {
            gui.OnOrganelleToPlaceSelected(ActiveActionName);
        }

        gui.SetSpeciesInfo(NewName, Membrane, Colour, Rigidity);
        gui.UpdateGeneration(species.Generation);
        gui.UpdateHitpoints(CalculateHitpoints());
    }

    private void CreateMutatedSpeciesCopy(Species species)
    {
        var newSpecies = CurrentGame.GameWorld.CreateMutatedSpecies(species);

        var random = new Random();

        var population = random.Next(Constants.INITIAL_SPLIT_POPULATION_MIN,
            Constants.INITIAL_SPLIT_POPULATION_MAX + 1);

        if (!CurrentGame.GameWorld.Map.CurrentPatch.AddSpecies(newSpecies, population))
        {
            GD.PrintErr("Failed to create a mutated version of the edited species");
        }
    }

    private void UpdateEditor(float delta)
    {
        _ = delta;

        if (organelleDataDirty)
        {
            OnOrganellesChanged();
            organelleDataDirty = false;
        }

        // We move all the hexes and the hover hexes to 0,0,0 so that
        // the editor is free to replace them wherever
        // TODO: it would be way better if we didn't have to do this and instead only updated
        // the hover hexes and organelles when there is some change to them
        foreach (var hex in hoverHexes)
        {
            hex.Translation = new Vector3(0, 0, 0);
            hex.Visible = false;
        }

        foreach (var organelle in hoverOrganelles)
        {
            organelle.Translation = new Vector3(0, 0, 0);
            organelle.Visible = false;
        }

        // This is also highly non-optimal to update the hex locations
        // and materials all the time

        // Reset the material of hexes that have been hovered over
        foreach (var entry in hoverOverriddenMaterials)
        {
            entry.Key.MaterialOverride = entry.Value;
        }

        hoverOverriddenMaterials.Clear();

        usedHoverHex = 0;
        usedHoverOrganelle = 0;

        // Show the organelle that is about to be placed
        if (ActiveActionName != null && ShowHover)
        {
            GetMouseHex(out int q, out int r);

            OrganelleDefinition shownOrganelle;

            var effectiveSymmetry = Symmetry;

            if (MovingOrganelle == null)
            {
                // Can place stuff at all?
                isPlacementProbablyValid = IsValidPlacement(new OrganelleTemplate(
                    GetOrganelleDefinition(ActiveActionName), new Hex(q, r), organelleRot));

                shownOrganelle = SimulationParameters.Instance.GetOrganelleType(ActiveActionName);
            }
            else
            {
                isPlacementProbablyValid = IsMoveTargetValid(new Hex(q, r), organelleRot, MovingOrganelle);
                shownOrganelle = MovingOrganelle.Definition;
                effectiveSymmetry = MicrobeSymmetry.None;
            }

            switch (effectiveSymmetry)
            {
                case MicrobeSymmetry.None:
                {
                    RenderHighlightedOrganelle(q, r, organelleRot, shownOrganelle);
                    break;
                }

                case MicrobeSymmetry.XAxisSymmetry:
                {
                    RenderHighlightedOrganelle(q, r, organelleRot, shownOrganelle);
                    RenderHighlightedOrganelle(-1 * q, r + q, 6 + (-1 * organelleRot), shownOrganelle);
                    break;
                }

                case MicrobeSymmetry.FourWaySymmetry:
                {
                    RenderHighlightedOrganelle(q, r, organelleRot, shownOrganelle);
                    RenderHighlightedOrganelle(-1 * q, r + q, 6 + (-1 * organelleRot), shownOrganelle);
                    RenderHighlightedOrganelle(-1 * q, -1 * r, (organelleRot + 3) % 6, shownOrganelle);
                    RenderHighlightedOrganelle(q, -1 * (r + q), 9 + (-1 * organelleRot) % 6, shownOrganelle);
                    break;
                }

                case MicrobeSymmetry.SixWaySymmetry:
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

    /// <summary>
    ///   If not hovering over an organelle, render the to-be-placed organelle
    /// </summary>
    private void RenderHighlightedOrganelle(int q, int r, int rotation, OrganelleDefinition shownOrganelle)
    {
        if (MovingOrganelle == null && ActiveActionName == null)
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
                        // Store the material to put it back later
                        hoverOverriddenMaterials[placed] = placed.MaterialOverride;

                        // Mark as invalid
                        placed.MaterialOverride = invalidMaterial;

                        showModel = false;
                    }

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

            var organelleModel = hoverOrganelles[usedHoverOrganelle++];

            organelleModel.Transform = new Transform(
                MathUtils.CreateRotationForOrganelle(rotation),
                cartesianPosition + shownOrganelle.CalculateModelOffset());

            organelleModel.Scale = new Vector3(Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
                Constants.DEFAULT_HEX_SIZE);

            organelleModel.Visible = true;

            UpdateOrganellePlaceHolderScene(organelleModel, shownOrganelle.DisplayScene);
        }
    }

    /// <summary>
    ///   Returns the hex position the mouse is over
    /// </summary>
    private void GetMouseHex(out int q, out int r)
    {
        // Get the position of the cursor in the plane that the microbes is floating in
        var cursorPos = camera.CursorWorldPos;

        // Convert to the hex the cursor is currently located over.
        var hex = Hex.CartesianToAxial(cursorPos);

        q = hex.Q;
        r = hex.R;
    }

    private MeshInstance CreateEditorHex()
    {
        var hex = (MeshInstance)hexScene.Instance();
        world.AddChild(hex);
        return hex;
    }

    private SceneDisplayer CreateEditorOrganelle()
    {
        var node = (SceneDisplayer)modelScene.Instance();
        world.AddChild(node);
        return node;
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
            case MicrobeSymmetry.None:
            {
                PlaceIfPossible(organelleType, q, r, organelleRot, ref placedSomething);
                break;
            }

            case MicrobeSymmetry.XAxisSymmetry:
            {
                PlaceIfPossible(organelleType, q, r, organelleRot, ref placedSomething);

                if (q != -1 * q || r != r + q)
                {
                    PlaceIfPossible(organelleType, -1 * q, r + q, 6 + (-1 * organelleRot), ref placedSomething);
                }

                break;
            }

            case MicrobeSymmetry.FourWaySymmetry:
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

            case MicrobeSymmetry.SixWaySymmetry:
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
        var organelle = new OrganelleTemplate(GetOrganelleDefinition(organelleType),
            new Hex(q, r), rotation);

        if (!IsValidPlacement(organelle))
        {
            // Play Sound
            gui.OnInvalidHexLocationSelected();
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

    /// <summary>
    ///   Checks if the target position is valid to place organelle.
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="rotation">The rotation to check for the organelle</param>
    /// <param name="organelle">Organelle type to try at the position</param>
    /// <returns>True if valid</returns>
    private bool IsMoveTargetValid(Hex position, int rotation, OrganelleTemplate organelle)
    {
        return editedMicrobeOrganelles.CanPlace(organelle.Definition, position, rotation, false);
    }

    private OrganelleDefinition GetOrganelleDefinition(string name)
    {
        return SimulationParameters.Instance.GetOrganelleType(name);
    }

    [DeserializedCallbackAllowed]
    private void DoOrganellePlaceAction(MicrobeEditorAction action)
    {
        var data = (PlacementActionData)action.Data;

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
        var data = (PlacementActionData)action.Data;

        editedMicrobeOrganelles.Remove(data.Organelle);

        foreach (var cyto in data.ReplacedCytoplasm)
        {
            GD.Print("Replacing ", cyto.Definition.InternalName, " at: ",
                cyto.Position);

            editedMicrobeOrganelles.Add(cyto);
        }
    }

    private bool AddOrganelle(OrganelleTemplate organelle)
    {
        // 1 - you put nucleus but you already have it
        // 2 - you put organelle that need nucleus and you don't have it
        if ((organelle.Definition.InternalName == "nucleus" && HasNucleus) ||
            (organelle.Definition.ProkaryoteChance == 0 && !HasNucleus
                && organelle.Definition.ChanceToCreate != 0))
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
        var data = (RemoveActionData)action.Data;
        editedMicrobeOrganelles.Remove(data.Organelle);
    }

    [DeserializedCallbackAllowed]
    private void UndoOrganelleRemoveAction(MicrobeEditorAction action)
    {
        var data = (RemoveActionData)action.Data;
        editedMicrobeOrganelles.Add(data.Organelle);
    }

    private void RemoveOrganelleAt(Hex location)
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

    [DeserializedCallbackAllowed]
    private void DoOrganelleMoveAction(MicrobeEditorAction action)
    {
        var data = (MoveActionData)action.Data;
        data.Organelle.Position = data.NewLocation;
        data.Organelle.Orientation = data.NewRotation;

        if (editedMicrobeOrganelles.Contains(data.Organelle))
        {
            UpdateAlreadyPlacedVisuals();
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
        var data = (MoveActionData)action.Data;
        data.Organelle.Position = data.OldLocation;
        data.Organelle.Orientation = data.OldRotation;
        UpdateAlreadyPlacedVisuals();

        --data.Organelle.NumberOfTimesMoved;
    }

    /// <summary>
    ///   Finishes an organelle move
    /// </summary>
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

        var action = new MicrobeEditorAction(this, cost,
            DoOrganelleMoveAction, UndoOrganelleMoveAction,
            new MoveActionData(organelle, oldLocation, newLocation, oldRotation, newRotation));

        EnqueueAction(action);

        // It's assumed that the above enqueue can't fail, otherwise the reference to MovingOrganelle may be
        // permanently lost (as the code that calls this assumes it's safe to set MovingOrganelle to null
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
        var data = (NewMicrobeActionData)action.Data;

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
        gui.UpdateMembraneButtons(Membrane.InternalName);
        gui.UpdateSpeed(CalculateSpeed());
        gui.UpdateHitpoints(CalculateHitpoints());
    }

    [DeserializedCallbackAllowed]
    private void OnOrganelleAdded(OrganelleTemplate organelle)
    {
        organelleDataDirty = true;
    }

    [DeserializedCallbackAllowed]
    private void OnOrganelleRemoved(OrganelleTemplate organelle)
    {
        organelleDataDirty = true;
    }

    private void OnOrganellesChanged()
    {
        UpdateAlreadyPlacedVisuals();

        UpdateArrow();

        // Send to gui current status of cell
        gui.UpdateSize(MicrobeHexSize);
        gui.UpdatePartsAvailability(HasNucleus);

        UpdatePatchDependentBalanceData();

        gui.UpdateSpeed(CalculateSpeed());
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

                hexNode.Visible = true;
            }

            // Model of the organelle
            if (organelle.Definition.DisplayScene != null)
            {
                var pos = Hex.AxialToCartesian(organelle.Position) +
                    organelle.Definition.CalculateModelOffset();

                if (nextFreeOrganelle >= placedModels.Count)
                {
                    // New organelle model needed
                    placedModels.Add(CreateEditorOrganelle());
                }

                var organelleModel = placedModels[nextFreeOrganelle++];

                organelleModel.Transform = new Transform(
                    MathUtils.CreateRotationForOrganelle(1 * organelle.Orientation), pos);

                organelleModel.Scale = new Vector3(Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
                    Constants.DEFAULT_HEX_SIZE);

                organelleModel.Visible = true;

                UpdateOrganellePlaceHolderScene(organelleModel,
                    organelle.Definition.DisplayScene);
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
    private void UpdateOrganellePlaceHolderScene(SceneDisplayer organelleModel, string displayScene)
    {
        organelleModel.Scene = displayScene;
    }

    [DeserializedCallbackAllowed]
    private void DoMembraneChangeAction(MicrobeEditorAction action)
    {
        var data = (MembraneActionData)action.Data;
        var membrane = data.NewMembrane;
        GD.Print("Changing membrane to '", membrane.InternalName, "'");
        Membrane = membrane;
        gui.UpdateMembraneButtons(Membrane.InternalName);
        gui.UpdateSpeed(CalculateSpeed());
        gui.UpdateHitpoints(CalculateHitpoints());
        CalculateEnergyBalanceWithOrganellesAndMembraneType(
            editedMicrobeOrganelles.Organelles, Membrane, targetPatch);
        gui.SetMembraneTooltips(Membrane);
    }

    [DeserializedCallbackAllowed]
    private void UndoMembraneChangeAction(MicrobeEditorAction action)
    {
        var data = (MembraneActionData)action.Data;
        Membrane = data.OldMembrane;
        GD.Print("Changing membrane back to '", Membrane.InternalName, "'");
        gui.UpdateMembraneButtons(Membrane.InternalName);
        gui.UpdateSpeed(CalculateSpeed());
        gui.UpdateHitpoints(CalculateHitpoints());
        CalculateEnergyBalanceWithOrganellesAndMembraneType(
            editedMicrobeOrganelles.Organelles, Membrane, targetPatch);
        gui.SetMembraneTooltips(Membrane);
    }

    [DeserializedCallbackAllowed]
    private void DoRigidityChangeAction(MicrobeEditorAction action)
    {
        var data = (RigidityChangeActionData)action.Data;

        Rigidity = data.NewRigidity;

        OnRigidityChanged();
    }

    [DeserializedCallbackAllowed]
    private void UndoRigidityChangeAction(MicrobeEditorAction action)
    {
        var data = (RigidityChangeActionData)action.Data;

        Rigidity = data.PreviousRigidity;
        OnRigidityChanged();
    }

    private void OnRigidityChanged()
    {
        gui.UpdateRigiditySlider((int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO),
            MutationPoints);

        gui.UpdateSpeed(CalculateSpeed());
        gui.UpdateHitpoints(CalculateHitpoints());
    }

    /// <summary>
    ///   Perform all actions through this to make undo and redo work
    /// </summary>
    private void EnqueueAction(MicrobeEditorAction action)
    {
        // A sanity check to not let an action proceed if we don't have enough mutation points
        if (MutationPoints < action.Cost)
        {
            // Flash the MP bar and play sound
            gui.OnInsufficientMp();
            return;
        }

        // Or if they are in the process of moving an organelle
        if (MovingOrganelle != null && !action.IsMoveAction)
        {
            // Play sound
            gui.OnActionBlockedWhileMoving();
            return;
        }

        history.AddAction(action);

        UpdateUndoRedoButtons();
    }

    private void UpdateUndoRedoButtons()
    {
        gui.SetUndoButtonStatus(history.CanUndo());
        gui.SetRedoButtonStatus(history.CanRedo());
    }

    /// <summary>
    ///   Called once auto-evo results are ready
    /// </summary>
    private void OnEditorReady()
    {
        ready = true;
        LoadingScreen.Instance.Hide();

        GD.Print("Elapsing time on editor entry");

        // TODO: select which units will be used for the master elapsed time counter
        CurrentGame.GameWorld.OnTimePassed(1);

        gui.UpdateTimeIndicator(CurrentGame.GameWorld.TotalPassedTime);

        // Get summary before applying results in order to get comparisons to the previous populations
        var run = CurrentGame.GameWorld.GetAutoEvoRun();

        if (run?.Results == null)
        {
            gui.UpdateAutoEvoResults(TranslationServer.Translate("AUTO_EVO_FAILED"),
                TranslationServer.Translate("AUTO_EVO_RUN_STATUS") + " " +
                (run != null ? run.Status : string.Empty));
        }
        else
        {
            autoEvoSummary = run.Results.MakeSummary(CurrentGame.GameWorld.Map, true,
                run.ExternalEffects);
            autoEvoExternal = run.MakeSummaryOfExternalEffects();

            gui.UpdateAutoEvoResults(autoEvoSummary, autoEvoExternal);
        }

        ApplyAutoEvoResults();

        gui.UpdateReportTabStatistics(CurrentPatch);

        // Auto save after editor entry is complete
        if (!CurrentGame.FreeBuild)
            SaveHelper.AutoSave(this);
    }

    private void OnLoadedEditorReady()
    {
        if (ready != true)
            throw new InvalidOperationException("loaded editor isn't in the ready state");

        gui.UpdateAutoEvoResults(autoEvoSummary, autoEvoExternal);

        gui.UpdateTimeIndicator(CurrentGame.GameWorld.TotalPassedTime);

        // Make absolutely sure the current game doesn't have an auto-evo run
        CurrentGame.GameWorld.ResetAutoEvoRun();

        gui.UpdateReportTabStatistics(CurrentPatch);
    }

    private void ApplyAutoEvoResults()
    {
        GD.Print("Applying auto-evo results");
        CurrentGame.GameWorld.GetAutoEvoRun().ApplyExternalEffects();

        CurrentGame.GameWorld.Map.UpdateGlobalTimePeriod(CurrentGame.GameWorld.TotalPassedTime);

        // Needs to be before the remove extinct species call, so that extinct species could still be stored
        // for reference in patch history (e.g. displaying it as zero on the species population chart)
        foreach (var entry in CurrentGame.GameWorld.Map.Patches)
        {
            entry.Value.RecordConditions();
        }

        var extinct = CurrentGame.GameWorld.Map.RemoveExtinctSpecies(FreeBuilding);

        foreach (var species in extinct)
        {
            CurrentGame.GameWorld.RemoveSpecies(species);

            GD.Print("Species ", species.FormattedName, " has gone extinct from the world.");
        }

        CurrentGame.GameWorld.Map.UpdateGlobalPopulations();

        // Clear the run to make the cell stage start a new run when we go back there
        CurrentGame.GameWorld.ResetAutoEvoRun();
    }

    /// <summary>
    ///   Updates the background shown in the editor
    /// </summary>
    private void UpdatePatchBackgroundImage()
    {
        camera.SetBackground(SimulationParameters.Instance.GetBackground(CurrentPatch.BiomeTemplate.Background));
    }

    private void SaveGame(string name)
    {
        SaveHelper.Save(name, this);
    }
}
