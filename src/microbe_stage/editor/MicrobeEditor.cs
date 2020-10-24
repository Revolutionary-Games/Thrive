using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main class of the microbe editor
/// </summary>
public class MicrobeEditor : Node, ILoadableGameState
{
    [Export]
    public NodePath PauseMenuPath;

    /// <summary>
    ///   The new to set on the species after exiting
    /// </summary>
    public string NewName;

    /// <summary>
    ///   Cost of the organelle that is about to be placed
    /// </summary>
    public float CurrentOrganelleCost;

    private MicrobeSymmetry symmetry = MicrobeSymmetry.None;

    private MicrobeCamera camera;
    private Node world;
    private MicrobeEditorGUI gui;
    private MicrobeEditorTutorialGUI tutorialGUI;
    private PauseMenu pauseMenu;

    /// <summary>
    ///   Where all user actions will  be registered
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is ignored until saving these actions to json is done
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    private ActionHistory<EditorAction> history;

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

    public MicrobeEditor()
    {
        RunOnInputAttribute.InputClasses.Add(this);
    }

    [Signal]
    public delegate void InvalidPlacementOfHex();

    [Signal]
    public delegate void InsufficientMPToPlaceHex();

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
    ///   When true nothing costs MP
    /// </summary>
    [JsonProperty]
    public bool FreeBuilding { get; private set; }

    /// <summary>
    ///   Hover hexes and models are only shown if this is true
    /// </summary>
    [JsonIgnore]
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

    /// <summary>
    ///   If true ReturnToStage has been loaded from a save and needs to be recreated on return
    /// </summary>
    public bool NeedToRestoreStageFromSave { get; set; }

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

    public Node GameStateRoot => this;

    public bool IsLoadedFromSave { get; set; } = false;

    public override void _Ready()
    {
        camera = GetNode<MicrobeCamera>("PrimaryCamera");
        world = GetNode("World");
        gui = GetNode<MicrobeEditorGUI>("MicrobeEditorGUI");
        tutorialGUI = GetNode<MicrobeEditorTutorialGUI>("TutorialGUI");
        pauseMenu = GetNode<PauseMenu>(PauseMenuPath);

        invalidMaterial = GD.Load<Material>(
            "res://src/microbe_stage/editor/InvalidHex.material");
        validMaterial = GD.Load<Material>("res://src/microbe_stage/editor/ValidHex.material");
        oldMaterial = GD.Load<Material>("res://src/microbe_stage/editor/OldHex.material");
        islandMaterial = GD.Load<Material>("res://src/microbe_stage/editor/IslandHex.material");

        hexScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/EditorHex.tscn");
        modelScene = GD.Load<PackedScene>("res://src/general/SceneDisplayer.tscn");

        camera.ObjectToFollow = GetNode<Spatial>("CameraLookAt");

        tutorialGUI.Visible = true;
        gui.Init(this);

        transitionFinished = false;

        OnEnterEditor();
    }

    public override void _ExitTree()
    {
        // As we will no longer return to the microbe stage we need to free it, if we have it
        // This might be disposed if this was loaded from a save and we loaded another save
        try
        {
            ReturnToStage?.QueueFree();
        }
        catch (ObjectDisposedException)
        {
            GD.Print("Editor's return to stage is already disposed");
        }
    }

    [RunOnMultiAxis("[[{\"e_pan_up\": -1}, {\"e_pan_down\": 1}],[{\"e_pan_left\": -1}, {\"e_pan_right\": 1}]]")]
    public void PanCamWithKeys(float delta, int[] inputs)
    {
        if (mousePanningStart != null)
            return;
        var movement = new Vector3(inputs[1], 0, inputs[0]);
        if (movement != Vector3.Zero)
            MoveObjectToFollow(movement.Normalized() * delta * Camera.CameraHeight);
    }

    [RunOnKey("e_pan_mouse", RunOnKeyAttribute.InputType.Hold)]
    public void PanCamWithMouse(float delta)
    {
        if (mousePanningStart == null)
            mousePanningStart = Camera.CursorWorldPos;
        else
        {
            var mousePanDirection = mousePanningStart.Value - Camera.CursorWorldPos;
            MoveObjectToFollow(mousePanDirection * delta * 10);
        }
    }

    [RunOnKey("e_pan_mouse", RunOnKeyAttribute.InputType.Released)]
    public void ReleasePanCamWithMouse()
    {
        mousePanningStart = null;
    }

    [RunOnKey("e_reset_cam", RunOnKeyAttribute.InputType.Hold)]
    public void ResetCamera()
    {
        camera.ObjectToFollow.Translation = new Vector3(0, 0, 0);
        camera.ResetHeight();
    }

    [RunOnKey("e_rotate_right", RunOnKeyAttribute.InputType.Press)]
    public void RotateRight()
    {
        organelleRot = (organelleRot + 1) % 6;
    }

    [RunOnKey("e_rotate_left", RunOnKeyAttribute.InputType.Press)]
    public void RotateLeft()
    {
        --organelleRot;

        if (organelleRot < 0)
            organelleRot = 5;
    }

    [RunOnKey("e_redo", RunOnKeyAttribute.InputType.Press)]
    public void Redo()
    {
        if (history.Redo())
        {
            TutorialState.SendEvent(TutorialEventType.MicrobeEditorRedo, EventArgs.Empty, this);
        }

        UpdateUndoRedoButtons();
    }

    [RunOnKey("e_undo", RunOnKeyAttribute.InputType.Press)]
    public void Undo()
    {
        if (history.Undo())
        {
            TutorialState.SendEvent(TutorialEventType.MicrobeEditorUndo, EventArgs.Empty, this);
        }

        UpdateUndoRedoButtons();
    }

    [RunOnKey("e_primary", RunOnKeyAttribute.InputType.Press)]
    public void PlaceOrganelle()
    {
        if (ActiveActionName == null)
            return;

        if (AddOrganelle(ActiveActionName))
        {
            // Only trigger tutorial if something was really placed
            TutorialState.SendEvent(TutorialEventType.MicrobeEditorOrganellePlaced, EventArgs.Empty, this);
        }
    }

    /// <summary>
    ///   Removes organelles under the cursor
    /// </summary>
    [RunOnKey("e_secondary", RunOnKeyAttribute.InputType.Press)]
    public void RemoveOrganelle()
    {
        GetMouseHex(out int q, out int r);

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
                    RemoveOrganelleAt(new Hex(q,      -1 * (r + q)));
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

                RemoveOrganelleAt(new Hex(-1 * r,       r + q));
                RemoveOrganelleAt(new Hex(-1 * (r + q), q));
                RemoveOrganelleAt(new Hex(-1 * q,       -1 * r));
                RemoveOrganelleAt(new Hex(r,            -1 * (r + q)));
                RemoveOrganelleAt(new Hex(r,            -1 * (r + q)));
                RemoveOrganelleAt(new Hex(r + q,        -1 * q));

                break;
            }
        }
    }

    [RunOnKey("g_quick_save", RunOnKeyAttribute.InputType.Press)]
    public void Screenshot()
    {
        // Can only save once the editor is ready
        if (ready)
        {
            GD.Print("quick saving microbe editor");
            SaveHelper.QuickSave(this);
        }
    }

    /// <summary>
    ///   Sets up the editor when entering
    /// </summary>
    public void OnEnterEditor()
    {
        // Clear old stuff in the world
        foreach (Node node in world.GetChildren())
        {
            node.Free();
        }

        // Let go of old resources
        hoverHexes = new List<MeshInstance>();
        hoverOrganelles = new List<SceneDisplayer>();

        history = new ActionHistory<EditorAction>();

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

        // Rest of the setup is only ran when not loading a save, the save finish callback does the equivalent thing
        if (IsLoadedFromSave)
            return;

        // Start a new game if no game has been started
        if (CurrentGame == null)
        {
            if (ReturnToStage != null)
                throw new Exception("stage to return to should have set our current game");

            GD.Print("Starting a new game for the microbe editor");
            CurrentGame = GameProperties.StartNewMicrobeGame();
        }

        InitEditor();

        StartMusic();

        TutorialState.SendEvent(TutorialEventType.EnteredMicrobeEditor, EventArgs.Empty, this);
    }

    public void OnFinishLoading(Save save)
    {
        ApplyPropertiesFromSave(save.MicrobeEditor);

        // Handle the stage to return to specially, as it also needs to run the code
        // for fixing the stuff in order to return there
        if (ReturnToStage != null)
        {
            NeedToRestoreStageFromSave = true;

            // We need to not let the objects be deleted before we apply them
            TemporaryLoadedNodeDeleter.Instance.AddDeletionHold(Constants.DELETION_HOLD_MICROBE_EDITOR);
        }

        InitEditor();

        StartMusic();
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

        MicrobeStage savedStageToApply = null;

        if (ReturnToStage == null || NeedToRestoreStageFromSave)
        {
            var scene = SceneManager.Instance.LoadScene(MainGameState.MicrobeStage);

            if (ReturnToStage == null)
            {
                GD.Print("Creating new microbe stage as there isn't one yet");
            }
            else
            {
                savedStageToApply = ReturnToStage;
            }

            ReturnToStage = (MicrobeStage)scene.Instance();
            ReturnToStage.CurrentGame = CurrentGame;
        }

        // Apply changes to the species organelles

        // It is easiest to just replace all
        editedSpecies.Organelles.Clear();

        var centerOfMass = editedMicrobeOrganelles.CenterOfMass;

        foreach (var organelle in editedMicrobeOrganelles.Organelles)
        {
            var organelleToAdd = (OrganelleTemplate)organelle.Clone();

            // This calculation aligns the center of mass with the origin by moving every organelle of the microbe.
            organelleToAdd.Position -= centerOfMass;
            organelleToAdd.PlacedThisSession = false;
            editedSpecies.Organelles.Add(organelleToAdd);
        }

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
            GD.Print("MicrobeEditor: applying player move to patch: ", targetPatch.Name);
            CurrentGame.GameWorld.Map.CurrentPatch = targetPatch;

            // Add the edited species to that patch to allow the species to gain population there
            CurrentGame.GameWorld.Map.CurrentPatch.AddSpecies(editedSpecies, 0);
        }

        var stage = ReturnToStage;

        // This needs to be reset here to not free this when we exit the tree
        ReturnToStage = null;

        SceneManager.Instance.SwitchToScene(stage);

        // We need to finish loading the save after attaching the stage scene
        if (savedStageToApply != null)
        {
            stage.OnFinishLoading(savedStageToApply);
            NeedToRestoreStageFromSave = false;

            // Resume deletion of save loaded objects now that we have used them finally
            TemporaryLoadedNodeDeleter.Instance.RemoveDeletionHold(Constants.DELETION_HOLD_MICROBE_EDITOR);
        }

        stage.OnReturnFromEditor();
    }

    public override void _Process(float delta)
    {
        if (!ready)
        {
            if (!CurrentGame.GameWorld.IsAutoEvoFinished())
            {
                LoadingScreen.Instance.Show("Loading Microbe Editor", "Waiting for auto-evo: " +
                    CurrentGame.GameWorld.GetAutoEvoRun().Status);
                return;
            }

            if (!transitionFinished)
                return;

            OnEditorReady();
        }

        UpdateEditor(delta);
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

        var action = new EditorAction(this, 0,
            redo =>
            {
                MutationPoints = Constants.BASE_MUTATION_POINTS;
                Membrane = SimulationParameters.Instance.GetMembrane("single");
                editedMicrobeOrganelles.Clear();
                editedMicrobeOrganelles.Add(new OrganelleTemplate(GetOrganelleDefinition("cytoplasm"),
                    new Hex(0, 0), 0));
                gui.UpdateMembraneButtons(Membrane.InternalName);
                gui.UpdateSpeed(CalculateSpeed());
                gui.UpdateHitpoints(CalculateHitpoints());
            },
            undo =>
            {
                editedMicrobeOrganelles.Clear();
                MutationPoints = previousMP;
                Membrane = oldMembrane;
                gui.UpdateMembraneButtons(Membrane.InternalName);
                gui.UpdateSpeed(CalculateSpeed());
                gui.UpdateHitpoints(CalculateHitpoints());

                foreach (var organelle in oldEditedMicrobeOrganelles)
                {
                    editedMicrobeOrganelles.Add(organelle);
                }
            });

        EnqueueAction(action);
    }

    public void SetMembrane(string membraneName)
    {
        var membrane = SimulationParameters.Instance.GetMembrane(membraneName);

        if (Membrane.Equals(membrane))
            return;

        var action = new EditorAction(this, membrane.EditorCost, DoMembraneChangeAction, UndoMembraneChangeAction,
            new MembraneActionData(Membrane, membrane));

        EnqueueAction(action);

        // In case the action failed, we need to make sure the membrane buttons are updated properly
        gui.UpdateMembraneButtons(Membrane.InternalName);
    }

    public void SetRigidity(int rigidity)
    {
        int intRigidity = (int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO);

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

        var action = new EditorAction(this, cost,
            redo =>
            {
                Rigidity = newRigidity;
                gui.UpdateRigiditySlider((int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO),
                    MutationPoints);
                gui.UpdateSpeed(CalculateSpeed());
                gui.UpdateHitpoints(CalculateHitpoints());
            },
            undo =>
            {
                Rigidity = prevRigidity;
                gui.UpdateRigiditySlider((int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO),
                    MutationPoints);
                gui.UpdateSpeed(CalculateSpeed());
                gui.UpdateHitpoints(CalculateHitpoints());
            });

        EnqueueAction(action);
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
    }

    /// <summary>
    ///   Changes the number of mutation points left. Should only be called by EditorAction
    /// </summary>
    internal void ChangeMutationPoints(int change)
    {
        if (FreeBuilding)
            return;

        MutationPoints = (MutationPoints + change).Clamp(0, Constants.BASE_MUTATION_POINTS);
    }

    /// <summary>
    ///   Recursively loops though all hexes and checks if there any without connection to the rest.
    /// </summary>
    /// <returns>
    ///   Returns a list of hexes that are not connected to the rest
    /// </returns>
    internal List<Hex> GetIslandHexes()
    {
        var organelles = editedMicrobeOrganelles.Organelles;

        if (organelles.Count == 0)
            return new List<Hex>();

        // The hex to start the recursion with
        var initHex = organelles[0].Position;

        // These are the hexes have neighbours and aren't islands
        var hexesWithNeighbours = new List<Hex> { initHex };

        // These are all of the existing hexes, that if there are no islands will all be visited
        var shouldBeVisited = organelles.Select(p => p.Position).ToList();

        CheckmarkNeighbors(hexesWithNeighbours, initHex);

        // Return the difference of the lists (hexes that were not visited)
        return shouldBeVisited.Except(hexesWithNeighbours).ToList();
    }

    /// <summary>
    ///   Called by PlayerMicrobeEditorInput
    /// </summary>
    /// <param name="vector">The direction to move the camera into</param>
    internal void MoveObjectToFollow(Vector3 vector)
    {
        camera.ObjectToFollow.Translation += vector;
    }

    private void StartMusic()
    {
        Jukebox.Instance.PlayingCategory = "MicrobeEditor";
        Jukebox.Instance.Resume();
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

        gui.UpdateEnergyBalance(ProcessSystem.ComputeEnergyBalance(organelles.Select(i => i.Definition), patch.Biome,
            membrane));
    }

    /// <summary>
    ///   Combined old editor init and activate method
    /// </summary>
    private void InitEditor()
    {
        // The world is reset each time so these are gone
        placedHexes = new List<MeshInstance>();
        placedModels = new List<SceneDisplayer>();

        hoverOverriddenMaterials = new Dictionary<MeshInstance, Material>();

        if (!IsLoadedFromSave)
        {
            InitEditorFresh();
        }
        else
        {
            InitEditorSaved();
        }

        // It's fine to reset these even when loading a save
        Symmetry = 0;
        gui.ResetSymmetryButton();

        UpdateUndoRedoButtons();

        // Send freebuild value to GUI
        gui.NotifyFreebuild(FreeBuilding);

        // Send info to the GUI about the organelle effectiveness in the current patch
        CalculateOrganelleEffectivenessInPatch();

        // Reset this, GUI will tell us to enable it again
        ShowHover = false;

        UpdatePatchBackgroundImage();

        gui.SetMap(CurrentGame.GameWorld.Map);

        gui.UpdateGlucoseReduction(Constants.GLUCOSE_REDUCTION_RATE);

        // Make tutorials run
        tutorialGUI.EventReceiver = TutorialState;
        pauseMenu.GameProperties = CurrentGame;

        // Send undo button to the tutorial system
        gui.SendUndoToTutorial(TutorialState);
    }

    private void InitEditorFresh()
    {
        // For now we only show a loading screen if auto-evo is not ready yet
        if (!CurrentGame.GameWorld.IsAutoEvoFinished())
        {
            ready = false;
            LoadingScreen.Instance.Show("Loading Microbe Editor", CurrentGame.GameWorld.GetAutoEvoRun().Status);
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
        // Need to recreate our organelle layout to make the callbacks work again, but we need to copy the existing
        // organelles to it
        var tempOrganelles = editedMicrobeOrganelles;

        editedMicrobeOrganelles = new OrganelleLayout<OrganelleTemplate>(
            OnOrganelleAdded, OnOrganelleRemoved);

        foreach (var organelle in tempOrganelles)
        {
            editedMicrobeOrganelles.Add(organelle);
        }

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

        UpdateGUIAfterLoadingSpecies(species);
    }

    private void UpdateGUIAfterLoadingSpecies(MicrobeSpecies species)
    {
        var genes = species.StringCode;

        GD.Print("Starting microbe editor with: ", editedMicrobeOrganelles.Organelles.Count,
            " organelles in the microbe, genes: ", genes);

        // Update GUI buttons now that we have correct organelles
        gui.UpdateGuiButtonStatus(HasNucleus);

        // Reset to cytoplasm if nothing is selected
        if (ActiveActionName == null)
        {
            gui.OnOrganelleToPlaceSelected("cytoplasm");
        }

        gui.SetInitialCellStats();

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
            CurrentOrganelleCost = SimulationParameters.Instance.GetOrganelleType(
                ActiveActionName).MPCost;

            GetMouseHex(out int q, out int r);

            // Can place stuff at all?
            isPlacementProbablyValid = IsValidPlacement(new OrganelleTemplate(
                GetOrganelleDefinition(ActiveActionName), new Hex(q, r), organelleRot));

            switch (Symmetry)
            {
                case MicrobeSymmetry.None:
                {
                    RenderHighlightedOrganelle(q, r, organelleRot);
                    break;
                }

                case MicrobeSymmetry.XAxisSymmetry:
                {
                    CurrentOrganelleCost *= 2;
                    RenderHighlightedOrganelle(q, r, organelleRot);
                    RenderHighlightedOrganelle(-1 * q, r + q, 6 + (-1 * organelleRot));
                    break;
                }

                case MicrobeSymmetry.FourWaySymmetry:
                {
                    CurrentOrganelleCost *= 4;
                    RenderHighlightedOrganelle(q, r, organelleRot);
                    RenderHighlightedOrganelle(-1 * q, r + q, 6 + (-1 * organelleRot));
                    RenderHighlightedOrganelle(-1 * q, -1 * r, (organelleRot + 180) % 6);
                    RenderHighlightedOrganelle(q, -1 * (r + q),
                        8 + (-1 * organelleRot) % 6);
                    break;
                }

                case MicrobeSymmetry.SixWaySymmetry:
                {
                    CurrentOrganelleCost *= 6;
                    RenderHighlightedOrganelle(q, r, organelleRot);
                    RenderHighlightedOrganelle(-1 * r, r + q, (organelleRot + 1) % 6);
                    RenderHighlightedOrganelle(-1 * (r + q), q, (organelleRot + 2) % 6);
                    RenderHighlightedOrganelle(-1 * q, -1 * r, (organelleRot + 3) % 6);
                    RenderHighlightedOrganelle(r, -1 * (r + q), (organelleRot + 4) % 6);
                    RenderHighlightedOrganelle(r + q, -1 * q, (organelleRot + 5) % 6);
                    break;
                }
            }
        }
        else
        {
            CurrentOrganelleCost = 0;
        }
    }

    /// <summary>
    ///   If not hovering over an organelle, render the to-be-placed organelle
    /// </summary>
    private void RenderHighlightedOrganelle(int q, int r, int rotation)
    {
        if (ActiveActionName == null)
            return;

        // TODO: this should be changed into a function parameter
        var toBePlacedOrganelle = SimulationParameters.Instance.GetOrganelleType(
            ActiveActionName);

        bool showModel = true;

        foreach (var hex in toBePlacedOrganelle.GetRotatedHexes(rotation))
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
        if (!string.IsNullOrEmpty(toBePlacedOrganelle.DisplayScene) && showModel)
        {
            var cartesianPosition = Hex.AxialToCartesian(new Hex(q, r));

            var organelleModel = hoverOrganelles[usedHoverOrganelle++];

            organelleModel.Transform = new Transform(
                MathUtils.CreateRotationForOrganelle(rotation),
                cartesianPosition + toBePlacedOrganelle.CalculateModelOffset());

            organelleModel.Scale = new Vector3(Constants.DEFAULT_HEX_SIZE, Constants.DEFAULT_HEX_SIZE,
                Constants.DEFAULT_HEX_SIZE);

            organelleModel.Visible = true;

            UpdateOrganellePlaceHolderScene(organelleModel, toBePlacedOrganelle.DisplayScene);
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
                    PlaceIfPossible(organelleType, q, -1 * (r + q), (8 + (-1 * organelleRot)) % 6, ref placedSomething);
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
            EmitSignal(nameof(InvalidPlacementOfHex));
            return;
        }

        // Skip placing if the player can't afford the organelle
        if (organelle.Definition.MPCost > MutationPoints && !FreeBuilding)
        {
            // Flash the MP bar and play sound
            EmitSignal(nameof(InsufficientMPToPlaceHex));
            return;
        }

        if (AddOrganelle(organelle))
        {
            placed = true;
        }
    }

    /// <summary>
    ///   A recursive function that adds the neighbours of current hex that contain organelles to the checked list and
    ///   recurses to them to find more connected organelles
    /// </summary>
    /// <param name="checked">The list of already visited hexes. Will be filled up with found hexes.</param>
    /// <param name="currentHex">The hex to visit the neighbours of.</param>
    private void CheckmarkNeighbors(List<Hex> @checked, Hex currentHex)
    {
        // Get all neighbors not already visited
        var myNeighbors = GetNeighborHexes(currentHex).Where(p => !@checked.Contains(p)).ToArray();

        // Add the new neighbors to the list to not visit them again
        @checked.AddRange(myNeighbors);

        // Recurse to all neighbours to find more connected hexes
        foreach (var neighbor in myNeighbors)
        {
            CheckmarkNeighbors(@checked, neighbor);
        }
    }

    /// <summary>Gets all neighboring hexes where there is an organelle</summary>
    /// <param name="hex">The hex to get the neighbours for</param>
    /// <returns>Returns a list of neighbors that are part of an organelle</returns>
    private IEnumerable<Hex> GetNeighborHexes(Hex hex)
    {
        return Hex.HexNeighbourOffset
            .Select(p => hex + p.Value)
            .Where(p => editedMicrobeOrganelles.GetOrganelleAt(p) != null);
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

    private void DoOrganellePlaceAction(EditorAction action)
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

    private void UndoOrganellePlaceAction(EditorAction action)
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

        var action = new EditorAction(this, organelle.Definition.MPCost,
            DoOrganellePlaceAction, UndoOrganellePlaceAction,
            new PlacementActionData(organelle));

        EnqueueAction(action);
        return true;
    }

    private void DoOrganelleRemoveAction(EditorAction action)
    {
        var data = (RemoveActionData)action.Data;
        editedMicrobeOrganelles.Remove(data.Organelle);
    }

    private void UndoOrganelleRemoveAction(EditorAction action)
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

        var action = new EditorAction(this, cost,
            DoOrganelleRemoveAction, UndoOrganelleRemoveAction,
            new RemoveActionData(organelleHere));

        EnqueueAction(action);
    }

    private void OnOrganelleAdded(OrganelleTemplate organelle)
    {
        OnOrganellesChanged();
    }

    private void OnOrganelleRemoved(OrganelleTemplate organelle)
    {
        OnOrganellesChanged();
    }

    private void OnOrganellesChanged()
    {
        UpdateAlreadyPlacedVisuals();

        // send to gui current status of cell
        gui.UpdateSize(MicrobeHexSize);
        gui.UpdateGuiButtonStatus(HasNucleus);

        // TODO: if this turns out to be expensive this should only be
        // called once the cell is fully loaded in and not for each
        // organelle
        // Calculate and send energy balance to the GUI
        CalculateEnergyBalanceWithOrganellesAndMembraneType(
            editedMicrobeOrganelles.Organelles, Membrane, targetPatch);

        // TODO: this might also be expensive
        gui.UpdateSpeed(CalculateSpeed());
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

        var islands = GetIslandHexes();

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
            placedHexes[placedHexes.Count - 1].QueueFree();
            placedHexes.RemoveAt(placedHexes.Count - 1);
        }

        while (nextFreeOrganelle < placedModels.Count)
        {
            placedModels[placedModels.Count - 1].QueueFree();
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

    private void DoMembraneChangeAction(EditorAction action)
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
    }

    private void UndoMembraneChangeAction(EditorAction action)
    {
        var data = (MembraneActionData)action.Data;
        Membrane = data.OldMembrane;
        GD.Print("Changing membrane back to '", Membrane.InternalName, "'");
        gui.UpdateMembraneButtons(Membrane.InternalName);
        gui.UpdateSpeed(CalculateSpeed());
        gui.UpdateHitpoints(CalculateHitpoints());
        CalculateEnergyBalanceWithOrganellesAndMembraneType(
            editedMicrobeOrganelles.Organelles, Membrane, targetPatch);
    }

    /// <summary>
    ///   Perform all actions through this to make undo and redo work
    /// </summary>
    private void EnqueueAction(EditorAction action)
    {
        // A sanity check to not let an action proceed if we don't have enough mutation points
        if (MutationPoints < action.Cost)
            return;

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
            gui.UpdateAutoEvoResults("Auto-evo failed to run", "run status: " +
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
    }

    private void ApplyAutoEvoResults()
    {
        GD.Print("Applying auto-evo results");
        CurrentGame.GameWorld.GetAutoEvoRun().ApplyExternalEffects();

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

    private void ApplyPropertiesFromSave(MicrobeEditor savedMicrobeEditor)
    {
        SaveApplyHelper.CopyJSONSavedPropertiesAndFields(this, savedMicrobeEditor);
    }

    /// <summary>
    ///   Done actions are stored here to provide undo/redo functionality
    /// </summary>
    /// <remarks>
    ///   TODO: this probably needs to be split into separate classes to make saving work for these
    /// </remarks>
    private class EditorAction : ReversableAction
    {
        [JsonProperty]
        public readonly int Cost;

        /// <summary>
        ///   Action specific data
        /// </summary>
        public object Data;

        private readonly Action<EditorAction> redo;
        private readonly Action<EditorAction> undo;

        private readonly MicrobeEditor editor;

        public EditorAction(MicrobeEditor editor, int cost,
            Action<EditorAction> redo,
            Action<EditorAction> undo, object data = null)
        {
            this.editor = editor;
            Cost = cost;
            this.redo = redo;
            this.undo = undo;
            Data = data;
        }

        public override void DoAction()
        {
            editor.ChangeMutationPoints(-Cost);
            redo(this);
        }

        public override void UndoAction()
        {
            editor.ChangeMutationPoints(Cost);
            undo(this);
        }
    }

    private class PlacementActionData
    {
        public List<OrganelleTemplate> ReplacedCytoplasm;
        public OrganelleTemplate Organelle;

        public PlacementActionData(OrganelleTemplate organelle)
        {
            Organelle = organelle;
        }
    }

    private class RemoveActionData
    {
        public OrganelleTemplate Organelle;

        public RemoveActionData(OrganelleTemplate organelle)
        {
            Organelle = organelle;
        }
    }

    private class MembraneActionData
    {
        public MembraneType OldMembrane;
        public MembraneType NewMembrane;

        public MembraneActionData(MembraneType oldMembrane, MembraneType newMembrane)
        {
            OldMembrane = oldMembrane;
            NewMembrane = newMembrane;
        }
    }
}
