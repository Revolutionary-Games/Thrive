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
public class MicrobeEditor : NodeWithInput, ILoadableGameState, IGodotEarlyNodeResolve
{
    [Export]
    public NodePath PauseMenuPath = null!;

    /// <summary>
    ///   The new to set on the species after exiting
    /// </summary>
    [JsonProperty]
    public string NewName = "error";

    /// <summary>
    ///   The hexes that are positioned under the cursor to show where the player is about to place something.
    /// </summary>
    private readonly List<MeshInstance> hoverHexes = new();

    /// <summary>
    ///   The organelle models that are positioned to show what the player is about to place.
    /// </summary>
    private readonly List<SceneDisplayer> hoverOrganelles = new();

    /// <summary>
    ///   This is the hexes for editedMicrobeOrganelles. This is the already placed hexes
    /// </summary>
    private readonly List<MeshInstance> placedHexes = new();

    /// <summary>
    ///   The hexes that have been changed by a hovering organelle and need to be reset to old material.
    /// </summary>
    private readonly Dictionary<MeshInstance, Material> hoverOverriddenMaterials = new();

    /// <summary>
    ///   This is the organelle models for editedMicrobeOrganelles
    /// </summary>
    private readonly List<SceneDisplayer> placedModels = new();

    private MicrobeSymmetry symmetry = MicrobeSymmetry.None;

    private int? mutationPointsCache;

    /// <summary>
    ///   Object camera is over. Needs to be defined before camera for saving to work
    /// </summary>
    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private Spatial cameraFollow = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private MicrobeCamera camera = null!;

    [JsonIgnore]
    private MeshInstance editorArrow = null!;

    /// <summary>
    ///   We're taking advantage of the available membrane and organelle system already present in
    ///   the microbe class for the cell preview.
    /// </summary>
    private Microbe? previewMicrobe;

    private MeshInstance editorGrid = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private MicrobeEditorGUI gui = null!;

    private Node world = null!;
    private Spatial rootOfDynamicallySpawned = null!;
    private MicrobeEditorTutorialGUI tutorialGUI = null!;
    private PauseMenu pauseMenu = null!;

    private Material invalidMaterial = null!;
    private Material validMaterial = null!;
    private Material oldMaterial = null!;
    private Material islandMaterial = null!;

    private PackedScene hexScene = null!;
    private PackedScene modelScene = null!;
    private PackedScene microbeScene = null!;

    [JsonProperty]
    private Color colour;

    [JsonProperty]
    private float rigidity;

    [JsonProperty]
    private BehaviourDictionary? behaviour;

    /// <summary>
    ///   Where the player wants to move after editing
    /// </summary>
    [JsonProperty]
    private Patch? targetPatch;

    /// <summary>
    ///   When false the player is no longer allowed to move patches (other than going back to where they were at the
    ///   start)
    /// </summary>
    [JsonProperty]
    private bool canStillMove;

    [JsonProperty]
    private Patch playerPatchOnEntry = null!;

    /// <summary>
    ///   This is used to keep track of used hover organelles
    /// </summary>
    private int usedHoverHex;

    private int usedHoverOrganelle;

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

    /// <summary>
    ///   True once auto-evo (and possibly other stuff) we need to wait for is ready
    /// </summary>
    [JsonProperty]
    private bool ready;

    [JsonProperty]
    private int organelleRot;

    [JsonProperty]
    private LocalizedStringBuilder? autoEvoSummary;

    [JsonProperty]
    private LocalizedStringBuilder? autoEvoExternal;

    [JsonProperty]
    private string? activeActionName;

    private bool microbePreviewMode;

    /// <summary>
    ///   True if auto save should trigger ASAP
    /// </summary>
    private bool wantsToSave;

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

    /// <summary>
    ///   True once fade transition is finished when entering editor
    /// </summary>
    [JsonIgnore]
    public bool TransitionFinished { get; private set; }

    public bool NodeReferencesResolved { get; private set; }

    [JsonIgnore]
    public MicrobeCamera Camera => camera;

    /// <summary>
    ///   Where all user actions will be registered
    /// </summary>
    public EditorActionHistory History { get; private set; } = new();

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
    ///   The number of mutation points left
    /// </summary>
    [JsonIgnore]
    public int MutationPoints => mutationPointsCache ?? CalculateMutationPointsLeft();

    /// <summary>
    ///   The symmetry setting of the microbe editor.
    /// </summary>
    public MicrobeSymmetry Symmetry
    {
        get => symmetry;
        set => symmetry = value;
    }

    /// <summary>
    ///   Organelles that is in the process of being moved but a new location hasn't been selected yet
    ///   If null, no organelle is in the process of moving.
    ///   May contain null when symmetry moving but there wasn't an organelle at a symmetry position
    /// </summary>
    [JsonProperty]
    public List<OrganelleTemplate?>? MovingOrganelles { get; private set; }

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

    /// <summary>
    ///   The main current game object holding various details
    /// </summary>
    [JsonProperty]
    public GameProperties? CurrentGame { get; set; }

    [JsonIgnore]
    public TutorialState TutorialState => CurrentGame?.TutorialState ??
        throw new InvalidOperationException("Editor doesn't have current game set yet");

    /// <summary>
    ///   If set the editor returns to this stage. The CurrentGame
    ///   should be shared with this stage. If not set returns to a new microbe stage
    /// </summary>
    [JsonProperty]
    public MicrobeStage? ReturnToStage { get; set; }

    [JsonIgnore]
    public bool HasNucleus => PlacedUniqueOrganelles.Any(d => d.InternalName == "nucleus");

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

    public IEnumerable<OrganelleDefinition> PlacedUniqueOrganelles => editedMicrobeOrganelles
        .Where(p => p.Definition.Unique)
        .Select(p => p.Definition);

    /// <summary>
    ///   Returns the current patch the player is in
    /// </summary>
    [JsonIgnore]
    public Patch CurrentPatch => targetPatch ?? playerPatchOnEntry;

    /// <summary>
    ///   If true an editor action is active and can be cancelled. Currently only checks for organelle move.
    /// </summary>
    [JsonIgnore]
    public bool CanCancelAction => MovingOrganelles != null;

    [JsonIgnore]
    public Node GameStateRoot => this;

    public bool IsLoadedFromSave { get; set; }

    [JsonIgnore]
    private bool Ready
    {
        get => ready;
        set
        {
            ready = value;
            pauseMenu.GameLoading = !value;
        }
    }

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
        microbeScene = GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn");

        if (!IsLoadedFromSave)
            camera.ObjectToFollow = cameraFollow;

        tutorialGUI.Visible = true;
        gui.Init(this);

        TransitionFinished = false;

        OnEnterEditor();
    }

    public void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        NodeReferencesResolved = true;

        world = GetNode("World");
        camera = world.GetNode<MicrobeCamera>("PrimaryCamera");
        editorArrow = world.GetNode<MeshInstance>("EditorArrow");
        editorGrid = world.GetNode<MeshInstance>("Grid");
        cameraFollow = world.GetNode<Spatial>("CameraLookAt");
        rootOfDynamicallySpawned = world.GetNode<Spatial>("DynamicallySpawned");
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

            if (ReturnToStage?.GetParent() != null)
                GD.PrintErr("ReturnToStage has a parent when editor is wanting to free it");

            ReturnToStage?.QueueFree();
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
        TransitionFinished = true;
    }

    /// <summary>
    ///   Applies the changes done and exits the editor
    /// </summary>
    public void OnFinishEditing()
    {
        GD.Print("MicrobeEditor: applying changes to edited Species");

        if (CurrentGame == null)
            throw new Exception("Editor must have active game when returning to the stage");

        if (ReturnToStage == null)
        {
            GD.Print("Creating new microbe stage as there isn't one yet");

            var scene = SceneManager.Instance.LoadScene(MainGameState.MicrobeStage);

            ReturnToStage = (MicrobeStage)scene.Instance();
            ReturnToStage.CurrentGame = CurrentGame;
        }

        // Apply changes to the species organelles
        if (editedSpecies == null)
            throw new Exception("Editor not initialized, missing edited species");

        // It is easiest to just replace all
        editedSpecies.Organelles.Clear();

        foreach (var organelle in editedMicrobeOrganelles.Organelles)
        {
            var organelleToAdd = (OrganelleTemplate)organelle.Clone();
            editedSpecies.Organelles.Add(organelleToAdd);
        }

        editedSpecies.RepositionToOrigin();

        // Update bacteria status
        editedSpecies.IsBacteria = !HasNucleus;

        editedSpecies.UpdateInitialCompounds();

        GD.Print("MicrobeEditor: updated organelles for species: ",
            editedSpecies.FormattedName);

        // Update name
        var match = Regex.Match(NewName, Constants.SPECIES_NAME_REGEX);
        if (match.Success)
        {
            editedSpecies.Genus = match.Groups["genus"].Value;
            editedSpecies.Epithet = match.Groups["epithet"].Value;

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

        editedSpecies.Behaviour = Behaviour ?? throw new Exception("Editor has not created behaviour object");

        // Move patches
        if (targetPatch != null)
        {
            GD.Print("MicrobeEditor: applying player move to patch: ", TranslationServer.Translate(targetPatch.Name));
            CurrentGame.GameWorld.Map.CurrentPatch = targetPatch;

            // Add the edited species to that patch to allow the species to gain population there
            // TODO: Log player species' migration
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
        if (!Ready)
        {
            if (!CurrentGame!.GameWorld.IsAutoEvoFinished())
            {
                LoadingScreen.Instance.Show(TranslationServer.Translate("LOADING_MICROBE_EDITOR"),
                    MainGameState.MicrobeEditor,
                    TranslationServer.Translate("WAITING_FOR_AUTO_EVO") + " " +
                    CurrentGame.GameWorld.GetAutoEvoRun().Status);
                return;
            }

            OnEditorReady();
        }

        // Auto save after editor entry is complete
        if (TransitionFinished && wantsToSave)
        {
            if (!CurrentGame!.FreeBuild)
                SaveHelper.AutoSave(this);

            wantsToSave = false;
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
            gui.UpdateAutoEvoResults(autoEvoSummary?.ToString() ?? "error", autoEvoExternal?.ToString() ?? "error");
            gui.UpdateTimeIndicator(CurrentGame!.GameWorld.TotalPassedTime);
            gui.UpdateGlucoseReduction(Constants.GLUCOSE_REDUCTION_RATE);
            gui.UpdatePatchDetails(CurrentPatch);
            gui.UpdateMicrobePartSelections();
            gui.UpdateMutationPointsBar();
            gui.UpdateTimeline();
            gui.UpdateReportTabPatchSelector();
        }
    }

    /// <summary>
    ///   Counts the number of placed organelles with this symmetry mode
    /// </summary>
    public int GetPositionsOfSymmetryMode(MicrobeSymmetry symmetry)
    {
        return symmetry switch
        {
            MicrobeSymmetry.None => 1,
            MicrobeSymmetry.FourWaySymmetry => 4,
            MicrobeSymmetry.XAxisSymmetry => 2,
            MicrobeSymmetry.SixWaySymmetry => 6,
            _ => throw new NotSupportedException("Symmetry mode not supported"),
        };
    }

    /// <summary>
    ///   Wipes clean the current cell.
    /// </summary>
    public void CreateNewMicrobe()
    {
        if (!FreeBuilding)
            throw new InvalidOperationException("can't reset cell when not freebuilding");

        var oldEditedMicrobeOrganelles = new OrganelleLayout<OrganelleTemplate>();
        var oldMembrane = Membrane;

        foreach (var organelle in editedMicrobeOrganelles)
        {
            oldEditedMicrobeOrganelles.Add(organelle);
        }

        var data = new NewMicrobeActionData(oldEditedMicrobeOrganelles, oldMembrane);

        var action =
            new SingleMicrobeEditorAction<NewMicrobeActionData>(DoNewMicrobeAction, UndoNewMicrobeAction, data);

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
        if (camera.ObjectToFollow == null)
        {
            GD.PrintErr("Editor camera doesn't have followed object set");
            return;
        }

        camera.ObjectToFollow.Translation = new Vector3(0, 0, 0);
        camera.ResetHeight();
    }

    [RunOnKeyDown("g_quick_save")]
    public void QuickSave()
    {
        // Can only save once the editor is ready
        if (Ready)
        {
            GD.Print("quick saving microbe editor");
            SaveHelper.QuickSave(this);
        }
    }

    [RunOnKeyDown("e_redo")]
    public void Redo()
    {
        if (MovingOrganelles != null)
            return;

        if (History.Redo())
        {
            DirtyMutationPointsCache();
            TutorialState.SendEvent(TutorialEventType.MicrobeEditorRedo, EventArgs.Empty, this);
        }

        UpdateUndoRedoButtons();
    }

    [RunOnKeyDown("e_undo")]
    public void Undo()
    {
        if (MovingOrganelles != null)
            return;

        if (History.Undo())
        {
            DirtyMutationPointsCache();
            TutorialState.SendEvent(TutorialEventType.MicrobeEditorUndo, EventArgs.Empty, this);
        }

        UpdateUndoRedoButtons();
    }

    [RunOnKeyDown("e_primary")]
    public void PlaceOrganelle()
    {
        if (MovingOrganelles != null)
        {
            GetMouseHex(out int q, out int r);
            if (MoveOrganelle(MovingOrganelles, new Hex(q, r), organelleRot))
            {
                // Move succeeded; Update the cancel button visibility so it's hidden because the move has completed
                MovingOrganelles = null;
                gui.UpdateCancelButtonVisibility();

                // Update rigidity slider in case it was disabled
                // TODO: could come up with a bit nicer design here
                int intRigidity = (int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO);
                gui.UpdateRigiditySlider(intRigidity);

                // Re-enable undo/redo/symmetry button
                UpdateUndoRedoButtons();
                UpdateSymmetryButton();
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

    [RunOnKeyDown("g_toggle_gui")]
    public void ToggleGUI()
    {
        gui.Visible = !gui.Visible;
    }

    public void SetMembrane(string membraneName)
    {
        var membrane = SimulationParameters.Instance.GetMembrane(membraneName);

        if (Membrane.Equals(membrane))
            return;

        var action = new SingleMicrobeEditorAction<MembraneActionData>(DoMembraneChangeAction, UndoMembraneChangeAction,
            new MembraneActionData(Membrane, membrane));

        EnqueueAction(action);

        // In case the action failed, we need to make sure the membrane buttons are updated properly
        gui.UpdateMembraneButtons(Membrane.InternalName);
    }

    public void SetBehaviouralValue(BehaviouralValueType type, float value)
    {
        gui.UpdateBehaviourSlider(type, value);

        if (Behaviour == null)
            throw new Exception($"{nameof(Behaviour)} is not set for editor");

        var oldValue = Behaviour[type];

        if (Math.Abs(value - oldValue) < MathUtils.EPSILON)
            return;

        var action = new SingleMicrobeEditorAction<BehaviourActionData>(DoBehaviourChangeAction,
            UndoBehaviourChangeAction, new BehaviourActionData(oldValue, value, type));

        EnqueueAction(action);
    }

    public void SetRigidity(int rigidity)
    {
        int intRigidity = (int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO);

        if (MovingOrganelles != null)
        {
            gui.OnActionBlockedWhileMoving();
            gui.UpdateRigiditySlider(intRigidity);
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
                gui.UpdateRigiditySlider(intRigidity);
                return;
            }

            rigidity = intRigidity > rigidity ? intRigidity - stepsLeft : intRigidity + stepsLeft;
        }

        var newRigidity = rigidity / Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO;
        var prevRigidity = Rigidity;

        var action = new SingleMicrobeEditorAction<RigidityActionData>(DoRigidityChangeAction,
            UndoRigidityChangeAction, new RigidityActionData(newRigidity, prevRigidity));

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
        if (MovingOrganelles != null)
        {
            gui.OnActionBlockedWhileMoving();
            return;
        }

        GetMouseHex(out int q, out int r);

        var organelle = editedMicrobeOrganelles.GetOrganelleAt(new Hex(q, r));

        if (organelle == null)
            return;

        var hexes = GetDistinctHexesWithSymmetryMode(q, r);
        var organelles = hexes.Select(p => editedMicrobeOrganelles.GetOrganelleAt(p.Hex))
            .DiscardNulls().ToList();

        // Put main organelle to the beginning
        organelles.Remove(organelle);
        organelles.Insert(0, organelle);

        gui.ShowOrganelleMenu(organelles);
    }

    public void StartOrganelleMoveAtHexWithSymmetryMode(IEnumerable<OrganelleTemplate?> selectedOrganelles)
    {
        if (MovingOrganelles != null)
        {
            // Already moving something! some code went wrong
            throw new InvalidOperationException("Can't begin organelle move while another in progress");
        }

        MovingOrganelles = new List<OrganelleTemplate?>();

        foreach (var organelle in selectedOrganelles)
        {
            MovingOrganelles.Add(organelle);
            if (organelle != null)
                editedMicrobeOrganelles.Remove(organelle);
        }

        if (MovingOrganelles.Count == 0)
        {
            MovingOrganelles = null;
            return;
        }

        // Disable undo/redo/symmetry button while moving (enabled after finishing move)
        UpdateUndoRedoButtons();
        UpdateSymmetryButton();
    }

    /// <summary>
    ///   Begin organelle movement for the organelle under the cursor
    /// </summary>
    [RunOnKeyDown("e_move")]
    public void StartOrganelleMoveAtCursor()
    {
        // Can't move an organelle while already moving one
        if (MovingOrganelles != null)
        {
            gui.OnActionBlockedWhileMoving();
            return;
        }

        GetMouseHex(out int q, out int r);
        var hexes = GetHexesWithSymmetryMode(q, r).Select(h => editedMicrobeOrganelles.GetOrganelleAt(h.Hex));

        StartOrganelleMoveAtHexWithSymmetryMode(hexes);

        // Once an organelle move has begun, the button visibility should be updated so it becomes visible
        gui.UpdateCancelButtonVisibility();
    }

    /// <summary>
    ///   Cancels the current editor action
    /// </summary>
    /// <returns>True when the input is consumed</returns>
    [RunOnKeyDown("e_cancel_current_action", Priority = 1)]
    public bool CancelCurrentAction()
    {
        if (MovingOrganelles != null)
        {
            foreach (var movingOrganelle in MovingOrganelles)
                editedMicrobeOrganelles.Add(movingOrganelle!);

            MovingOrganelles = null;
            gui.UpdateCancelButtonVisibility();

            // Re-enable undo/redo/symmetry button
            UpdateUndoRedoButtons();
            UpdateSymmetryButton();

            return true;
        }

        return false;
    }

    public void RemoveOrganelleAtHexWithSymmetryMode(IEnumerable<OrganelleTemplate> selectedOrganelles)
    {
        var action =
            new CombinedMicrobeEditorAction(selectedOrganelles.Select(RemoveOrganelleAt).DiscardNulls().ToArray());
        EnqueueAction(action);
    }

    /// <summary>
    ///   Remove the organelle under the cursor
    /// </summary>
    [RunOnKeyDown("e_delete")]
    public void RemoveOrganelleAtCursor()
    {
        GetMouseHex(out int q, out int r);

        var hexes =
            GetHexesWithSymmetryMode(q, r).Select(h => editedMicrobeOrganelles.GetOrganelleAt(h.Hex)).DiscardNulls();

        RemoveOrganelleAtHexWithSymmetryMode(hexes);
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
    public float CalculateCurrentOrganelleCost(List<(Hex Hex, int Orientation)>? mouseHoverHexes)
    {
        if (string.IsNullOrEmpty(ActiveActionName) || !ShowHover)
            return 0;

        var organelleDefinition = SimulationParameters.Instance.GetOrganelleType(ActiveActionName!);

        if (mouseHoverHexes == null)
            return organelleDefinition.MPCost * GetPositionsOfSymmetryMode(Symmetry);

        var organelleTemplate = mouseHoverHexes
            .Select(hex => new OrganelleTemplate(organelleDefinition, hex.Hex, hex.Orientation)).ToList();

        var moveOccupancies = GetMultiActionWithOccupancies(mouseHoverHexes, MovingOrganelles ?? organelleTemplate!,
            MovingOrganelles != null);

        return History.WhatWouldActionsCost(moveOccupancies.Data.ToList());
    }

    /// <summary>
    ///   Returns true when the player is allowed to move to the specified patch
    /// </summary>
    /// <returns>True if the patch move requested is valid. False otherwise</returns>
    public bool IsPatchMoveValid(Patch? patch)
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

        // If we are freebuilding, check if the target patch is connected by any means, then it is allowed
        if (FreeBuilding && CurrentPatch.GetAllConnectedPatches().Contains(patch))
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

    public void SetPlayerPatch(Patch? patch)
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
    ///   Sets the visibility of placed cell parts, editor forward arrow, etc.
    /// </summary>
    public void SetEditorCellVisibility(bool shown)
    {
        editorArrow.Visible = shown;
        editorGrid.Visible = shown;
        rootOfDynamicallySpawned.Visible = shown;
    }

    internal void DirtyMutationPointsCache()
    {
        mutationPointsCache = null;
    }

    private bool HasOrganelle(OrganelleDefinition organelleDefinition)
    {
        return editedMicrobeOrganelles.Organelles.Any(o => o.Definition == organelleDefinition);
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
        rootOfDynamicallySpawned.FreeChildren();

        // For now we never reuse editors so it isn't worth the trouble to have code to properly clear these
        if (hoverHexes.Count > 0 || hoverOrganelles.Count > 0 || hoverOverriddenMaterials.Count > 0)
            throw new InvalidOperationException("This editor has already been initialized (hexes not empty)");

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
        Jukebox.Instance.PlayCategory("MicrobeEditor");
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

        gui.UpdateOrganelleEfficiencies(result);
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
        gui.CancelPreviousAutoEvoPrediction();

        if (editedSpecies == null)
            throw new InvalidOperationException("Editor has not been setup correctly, missing edited species");

        cachedAutoEvoPredictionSpecies ??= (MicrobeSpecies)editedSpecies.Clone();

        CopyEditedPropertiesToSpecies(cachedAutoEvoPredictionSpecies);

        var run = new EditorAutoEvoRun(CurrentGame!.GameWorld, editedSpecies, cachedAutoEvoPredictionSpecies);
        run.Start();

        gui.UpdateAutoEvoPrediction(run, editedSpecies, cachedAutoEvoPredictionSpecies);
    }

    /// <summary>
    ///   Calculates the energy balance for a cell with the given organelles
    /// </summary>
    private void CalculateEnergyBalanceWithOrganellesAndMembraneType(List<OrganelleTemplate> organelles,
        MembraneType membrane, Patch? patch = null)
    {
        patch ??= CurrentPatch;

        gui.UpdateEnergyBalance(
            ProcessSystem.ComputeEnergyBalance(organelles, patch.Biome, membrane));
    }

    private void CalculateCompoundBalanceInPatch(List<OrganelleTemplate> organelles, Patch? patch = null)
    {
        patch ??= CurrentPatch;

        var result = ProcessSystem
            .ComputeCompoundBalance(organelles, patch.Biome);

        gui.UpdateCompoundBalances(result);
    }

    /// <summary>
    ///   Combined old editor init and activate method
    /// </summary>
    private void InitEditor()
    {
        // The world is reset each time so these are gone. We throw an exception if that's not the case as that
        // indicates a programming bug
        if (placedHexes.Count > 0 || placedModels.Count > 0)
            throw new InvalidOperationException("This editor has already been initialized (placed hexes not empty)");

        if (!IsLoadedFromSave)
        {
            // Auto save is wanted once possible
            wantsToSave = true;

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

        if (editedSpecies == null || CurrentGame == null)
            throw new Exception($"Editor setup which was just ran didn't setup {nameof(editedSpecies)} or world");

        // Setup the display cell
        previewMicrobe = (Microbe)microbeScene.Instance();
        previewMicrobe.IsForPreviewOnly = true;
        rootOfDynamicallySpawned.AddChild(previewMicrobe);
        previewMicrobe.ApplySpecies((MicrobeSpecies)editedSpecies.Clone());

        // Set its initial visibility
        previewMicrobe.Visible = MicrobePreviewMode;

        UpdateUndoRedoButtons();
        UpdateSymmetryButton();

        UpdateArrow(false);

        gui.UpdateMutationPointsBar(false);

        // Send freebuild value to GUI
        gui.NotifyFreebuild(FreeBuilding);

        // Send info to the GUI about the organelle effectiveness in the current patch
        CalculateOrganelleEffectivenessInPatch(CurrentPatch);

        UpdatePatchBackgroundImage();

        gui.SetMap(CurrentGame.GameWorld.Map);

        gui.UpdateGlucoseReduction(Constants.GLUCOSE_REDUCTION_RATE);

        gui.UpdateReportTabPatchSelector();

        gui.UpdateRigiditySliderState(MutationPoints);

        // Make tutorials run
        tutorialGUI.EventReceiver = TutorialState;
        pauseMenu.GameProperties = CurrentGame;

        // Send undo button to the tutorial system
        gui.SendUndoToTutorial(TutorialState);

        gui.UpdateCancelButtonVisibility();

        pauseMenu.SetNewSaveNameFromSpeciesName();
    }

    private void InitEditorFresh()
    {
        editedMicrobeOrganelles = new OrganelleLayout<OrganelleTemplate>(
            OnOrganelleAdded, OnOrganelleRemoved);

        organelleRot = 0;

        targetPatch = null;

        playerPatchOnEntry = CurrentGame!.GameWorld.Map.CurrentPatch ??
            throw new InvalidOperationException("Map current patch needs to be set before entering the editor");

        canStillMove = true;

        // For now we only show a loading screen if auto-evo is not ready yet
        if (!CurrentGame.GameWorld.IsAutoEvoFinished())
        {
            Ready = false;
            LoadingScreen.Instance.Show(TranslationServer.Translate("LOADING_MICROBE_EDITOR"),
                MainGameState.MicrobeEditor,
                CurrentGame.GameWorld.GetAutoEvoRun().Status);

            CurrentGame.GameWorld.FinishAutoEvoRunAtFullSpeed();
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

        var playerSpecies = CurrentGame.GameWorld.PlayerSpecies;

        SetupEditedSpecies((MicrobeSpecies)playerSpecies);
    }

    private void InitEditorSaved()
    {
        UpdateGUIAfterLoadingSpecies(editedSpecies ??
            throw new JsonException($"Saved editor was missing {nameof(editedSpecies)}"));
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
        gui.SetInitialCellStats();

        UpdateGUIAfterLoadingSpecies(species);
    }

    private void UpdateGUIAfterLoadingSpecies(MicrobeSpecies species)
    {
        GD.Print("Starting microbe editor with: ", editedMicrobeOrganelles.Organelles.Count,
            " organelles in the microbe");

        // Update GUI buttons now that we have correct organelles
        gui.UpdatePartsAvailability(PlacedUniqueOrganelles.ToList());

        // Reset to cytoplasm if nothing is selected
        gui.OnOrganelleToPlaceSelected(ActiveActionName ?? "cytoplasm");

        gui.SetSpeciesInfo(NewName, Membrane, Colour, Rigidity,
            Behaviour ?? throw new Exception($"Editor doesn't have {nameof(Behaviour)} setup"));
        gui.UpdateGeneration(species.Generation);
        gui.UpdateHitpoints(CalculateHitpoints());
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
    ///   Calculates the remaining MP from the action history
    /// </summary>
    /// <returns>The remaining MP</returns>
    private int CalculateMutationPointsLeft()
    {
        if (FreeBuilding || CheatManager.InfiniteMP)
            return Constants.BASE_MUTATION_POINTS;

        mutationPointsCache = History.CalculateMutationPointsLeft();

        if (mutationPointsCache.Value < 0 || mutationPointsCache > Constants.BASE_MUTATION_POINTS)
        {
            GD.PrintErr("Warning: Invalid MP amount: ", mutationPointsCache,
                " This should only happen if the user disabled the Infinite MP cheat while having mutated too much.");
        }

        gui.UpdateMutationPointsBar();

        return mutationPointsCache.Value;
    }

    /// <summary>
    ///   Returns a list with hex, orientation, the organelle and whether or not this hex is
    ///   already occupied by a higher-ranked organelle.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     An organelle is ranked higher if it costs more MP.
    ///   </para>
    /// </remarks>
    private List<(Hex Hex, OrganelleTemplate? Organelle, int Orientation, bool Occupied)> GetOccupancies(
        List<(Hex Hex, int Orientation)> hexes, List<OrganelleTemplate?> organelles)
    {
        var organellePositions = new List<(Hex Hex, OrganelleTemplate? Organelle, int Orientation, bool Occupied)>();

        for (var i = 0; i < hexes.Count; i++)
        {
            var (hex, orientation) = hexes[i];
            var organelle = organelles[i];
            var oldOrganelle = organellePositions.FirstOrDefault(p => p.Hex == hex);
            var occupied = false;
            if (oldOrganelle != default && organelle != null)
            {
                if (organelle.Definition.MPCost > oldOrganelle.Organelle?.Definition.MPCost)
                {
                    organellePositions.Remove(oldOrganelle);
                    oldOrganelle.Occupied = true;
                    organellePositions.Add(oldOrganelle);
                }
                else
                {
                    occupied = true;
                }
            }

            organellePositions.Add((hex, organelle, orientation, occupied));
        }

        return organellePositions;
    }

    private CombinedMicrobeEditorAction GetMultiActionWithOccupancies(List<(Hex Hex, int Orientation)> hexes,
        List<OrganelleTemplate?> organelles, bool moving)
    {
        var moveActionData = new List<MicrobeEditorAction>();
        foreach (var (hex, organelle, orientation, occupied) in GetOccupancies(hexes, organelles))
        {
            if (organelle == null)
                continue;

            MicrobeEditorAction action;
            if (occupied)
            {
                var data = new RemoveActionData(organelle, organelle.Position, organelle.Orientation)
                {
                    GotReplaced = organelle.Definition.InternalName == "cytoplasm",
                };
                action = new SingleMicrobeEditorAction<RemoveActionData>(DoOrganelleRemoveAction,
                    UndoOrganelleRemoveAction, data);
            }
            else
            {
                if (moving)
                {
                    var data = new MoveActionData(organelle, organelle.Position, hex, organelle.Orientation,
                        orientation);
                    action = new SingleMicrobeEditorAction<MoveActionData>(DoOrganelleMoveAction,
                        UndoOrganelleMoveAction, data);
                }
                else
                {
                    var replacedHex = editedMicrobeOrganelles.GetOrganelleAt(hex);
                    var data = new PlacementActionData(organelle, hex, orientation);
                    if (replacedHex != null)
                        data.ReplacedCytoplasm = new List<OrganelleTemplate> { replacedHex };

                    action = new SingleMicrobeEditorAction<PlacementActionData>(DoOrganellePlaceAction,
                        UndoOrganellePlaceAction, data);
                }
            }

            moveActionData.Add(action);
        }

        return new CombinedMicrobeEditorAction(moveActionData.ToArray());
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

        editorGrid.Translation = Camera.CursorWorldPos;
        editorGrid.Visible = ShowHover && !MicrobePreviewMode;

        // Show the organelle that is about to be placed
        if (ActiveActionName != null && ShowHover && !MicrobePreviewMode)
        {
            GetMouseHex(out int q, out int r);

            var hexes = GetHexesWithSymmetryMode(q, r);
            var distinctHexes = GetDistinctHexesWithSymmetryMode(hexes);
            if (MovingOrganelles == null)
            {
                // Can place stuff at all?
                isPlacementProbablyValid = IsValidPlacement(new OrganelleTemplate(
                    GetOrganelleDefinition(ActiveActionName), new Hex(q, r), organelleRot));

                var shownOrganelle = SimulationParameters.Instance.GetOrganelleType(ActiveActionName);

                foreach (var (hex, orientation) in distinctHexes)
                {
                    RenderHighlightedOrganelle(hex.Q, hex.R, orientation, shownOrganelle);
                }
            }
            else
            {
                foreach (var (hex, organelle, orientation, occupied) in GetOccupancies(hexes, MovingOrganelles))
                {
                    if (organelle != null && !occupied)
                        RenderHighlightedOrganelle(hex.Q, hex.R, orientation, organelle.Definition);
                }
            }

            gui.MouseHoverHexes = distinctHexes;
        }

        if (mutationPointsCache == null)
        {
            gui.UpdateMutationPointsBar();
            gui.UpdateRigiditySliderState(MutationPoints);
        }
    }

    /// <summary>
    ///   If not hovering over an organelle, render the to-be-placed organelle
    /// </summary>
    private void RenderHighlightedOrganelle(int q, int r, int rotation, OrganelleDefinition shownOrganelle)
    {
        if (MovingOrganelles == null && ActiveActionName == null)
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

            var organelleModel = hoverOrganelles[usedHoverOrganelle++];

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
        rootOfDynamicallySpawned.AddChild(hex);
        return hex;
    }

    private SceneDisplayer CreateEditorOrganelle()
    {
        var node = (SceneDisplayer)modelScene.Instance();
        rootOfDynamicallySpawned.AddChild(node);
        return node;
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

    private List<(Hex Hex, int Orientation)> GetDistinctHexesWithSymmetryMode(List<(Hex Hex, int Orientation)> hexes)
    {
        return hexes.GroupBy(p => p.Hex).Select(p => p.First()).ToList();
    }

    private List<(Hex Hex, int Orientation)> GetDistinctHexesWithSymmetryMode(int q, int r)
    {
        return GetDistinctHexesWithSymmetryMode(GetHexesWithSymmetryMode(q, r));
    }

    /// <summary>
    ///   Returns the hexes with the current symmetry mode. May contains duplicates.
    /// </summary>
    private List<(Hex Hex, int Orientation)> GetHexesWithSymmetryMode(int q, int r)
    {
        var hexes = new List<(Hex Hex, int Orientation)>
        {
            (new Hex(q, r), organelleRot),
        };

        switch (Symmetry)
        {
            case MicrobeSymmetry.None:
            {
                break;
            }

            case MicrobeSymmetry.XAxisSymmetry:
            {
                hexes.Add((new Hex(-1 * q, r + q), 6 + (-1 * organelleRot)));

                break;
            }

            case MicrobeSymmetry.FourWaySymmetry:
            {
                hexes.Add((new Hex(-1 * q, r + q), 6 + (-1 * organelleRot)));
                hexes.Add((new Hex(-1 * q, -1 * r), (organelleRot + 3) % 6));
                hexes.Add((new Hex(q, -1 * (r + q)), (9 + (-1 * organelleRot)) % 6));

                break;
            }

            case MicrobeSymmetry.SixWaySymmetry:
            {
                hexes.Add((new Hex(-1 * r, r + q), (organelleRot + 1) % 6));
                hexes.Add((new Hex(-1 * (r + q), q), (organelleRot + 2) % 6));
                hexes.Add((new Hex(-1 * q, -1 * r), (organelleRot + 3) % 6));
                hexes.Add((new Hex(r, -1 * (r + q)), (organelleRot + 4) % 6));
                hexes.Add((new Hex(r + q, -1 * q), (organelleRot + 5) % 6));
                break;
            }

            default:
                throw new Exception("unimplemented symmetry in AddOrganelle");
        }

        return hexes;
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
        GetMouseHex(out var q, out var r);

        var hexes = GetDistinctHexesWithSymmetryMode(q, r);

        var organelleDefinition = GetOrganelleDefinition(organelleType);

        var organelleTemplates = hexes
            .Select(hex => new OrganelleTemplate(organelleDefinition, hex.Hex, hex.Orientation)).ToList();

        var multiAction = new CombinedMicrobeEditorAction(organelleTemplates.Select(PlaceIfPossible).DiscardNulls()
            .Select(a => (MicrobeEditorAction)a).ToArray());

        return EnqueueAction(multiAction);
    }

    private IEnumerable<OrganelleTemplate> GetReplacedCytoplasm(IEnumerable<OrganelleTemplate> organelles)
    {
        return organelles
            .Where(o => o.Definition.InternalName != "cytoplasm")
            .SelectMany(o => o.Definition.Hexes.Select(hex => hex + o.Position))
            .Select(hex => editedMicrobeOrganelles.GetOrganelleAt(hex))
            .Where(o => o?.Definition.InternalName == "cytoplasm")!;
    }

    private IEnumerable<RemoveActionData> GetReplacedCytoplasmRemoveActionData(
        IEnumerable<OrganelleTemplate> organelles)
    {
        return GetReplacedCytoplasm(organelles)
            .Select(o => new RemoveActionData(o, o.Position, o.Orientation)
            {
                GotReplaced = true,
            });
    }

    private IEnumerable<SingleMicrobeEditorAction<RemoveActionData>> GetReplacedCytoplasmRemoveAction(
        IEnumerable<OrganelleTemplate> organelles)
    {
        var replacedCytoplasmData = GetReplacedCytoplasmRemoveActionData(organelles);
        return replacedCytoplasmData.Select(o =>
            new SingleMicrobeEditorAction<RemoveActionData>(DoOrganelleRemoveAction, UndoOrganelleRemoveAction, o));
    }

    /// <summary>
    ///   Helper for AddOrganelle
    /// </summary>
    private CombinedMicrobeEditorAction? PlaceIfPossible(OrganelleTemplate organelle)
    {
        if (MicrobePreviewMode)
            return null;

        if (!IsValidPlacement(organelle))
        {
            // Play Sound
            gui.OnInvalidHexLocationSelected();
            return null;
        }

        return AddOrganelle(organelle);
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
    private void DoOrganellePlaceAction(PlacementActionData data)
    {
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
    private void UndoOrganellePlaceAction(PlacementActionData data)
    {
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

    private CombinedMicrobeEditorAction? AddOrganelle(OrganelleTemplate organelle)
    {
        // 1 - you put a unique organelle (means only one instance allowed) but you already have it
        // 2 - you put an organelle that requires nucleus but you don't have one
        if ((organelle.Definition.Unique && HasOrganelle(organelle.Definition)) ||
            (organelle.Definition.RequiresNucleus && !HasNucleus))
            return null;

        var replacedCytoplasmActions =
            GetReplacedCytoplasmRemoveAction(new[] { organelle }).OfType<MicrobeEditorAction>().ToList();

        var action = new SingleMicrobeEditorAction<PlacementActionData>(DoOrganellePlaceAction,
            UndoOrganellePlaceAction, new PlacementActionData(organelle, organelle.Position, organelle.Orientation));

        replacedCytoplasmActions.Add(action);
        return new CombinedMicrobeEditorAction(replacedCytoplasmActions.ToArray());
    }

    [DeserializedCallbackAllowed]
    private void DoOrganelleRemoveAction(RemoveActionData data)
    {
        editedMicrobeOrganelles.Remove(data.Organelle);
    }

    [DeserializedCallbackAllowed]
    private void UndoOrganelleRemoveAction(RemoveActionData data)
    {
        editedMicrobeOrganelles.Add(data.Organelle);
    }

    private MicrobeEditorAction? RemoveOrganelleAt(OrganelleTemplate organelle)
    {
        // Dont allow deletion of nucleus or the last organelle
        if (organelle.Definition.InternalName == "nucleus" || MicrobeSize < 2)
            return null;

        return new SingleMicrobeEditorAction<RemoveActionData>(DoOrganelleRemoveAction, UndoOrganelleRemoveAction,
            new RemoveActionData(organelle, organelle.Position, organelle.Orientation));
    }

    [DeserializedCallbackAllowed]
    private void DoOrganelleMoveAction(MoveActionData data)
    {
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

        OnMembraneChanged();
    }

    [DeserializedCallbackAllowed]
    private void UndoOrganelleMoveAction(MoveActionData data)
    {
        data.Organelle.Position = data.OldLocation;
        data.Organelle.Orientation = data.OldRotation;

        UpdateAlreadyPlacedVisuals();
        StartAutoEvoPrediction();

        OnMembraneChanged();
    }

    /// <summary>
    ///   Finishes an organelle move
    /// </summary>
    /// <returns>True if the organelle move succeeded.</returns>
    private bool MoveOrganelle(List<OrganelleTemplate?> organelles, Hex newLocation, int newRotation)
    {
        var hexes = GetHexesWithSymmetryMode(newLocation.Q, newLocation.R);
        var occupiedHexes = new HashSet<Hex>();

        // Make sure placement is valid
        for (var i = 0; i < organelles.Count; i++)
        {
            var o = organelles[i];
            if (o == null)
                continue;

            var (hex, orientation) = hexes[i];
            if (!IsMoveTargetValid(hex, orientation, o))
                return false;

            var oHexes = o.Definition.GetRotatedHexes(orientation).Select(h => h + hex);
            if (oHexes.Any(h => !occupiedHexes.Add(h)))
                return false;
        }

        var multiAction = GetMultiActionWithOccupancies(hexes, organelles, true);

        // Too low mutation points, cancel move
        if (MutationPoints < History.WhatWouldActionsCost(multiAction.Data.ToList()))
        {
            CancelCurrentAction();
            gui.OnInsufficientMp(false);
            return false;
        }

        EnqueueAction(multiAction);

        // It's assumed that the above enqueue can't fail, otherwise the reference to MovingOrganelle may be
        // permanently lost (as the code that calls this assumes it's safe to set MovingOrganelle to null
        // when we return true)
        return true;
    }

    [DeserializedCallbackAllowed]
    private void DoNewMicrobeAction(NewMicrobeActionData data)
    {
        // TODO: could maybe grab the current organelles and put them in the action here? This could be more safe
        // against weird situations where it might be possible if the undo / redo system is changed to restore
        // the wrong organelles

        Membrane = SimulationParameters.Instance.GetMembrane("single");
        editedMicrobeOrganelles.Clear();
        editedMicrobeOrganelles.Add(new OrganelleTemplate(GetOrganelleDefinition("cytoplasm"),
            new Hex(0, 0), 0));

        OnPostNewMicrobeChange();
    }

    [DeserializedCallbackAllowed]
    private void UndoNewMicrobeAction(NewMicrobeActionData data)
    {
        editedMicrobeOrganelles.Clear();
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
        gui.UpdateSize(MicrobeHexSize);

        gui.UpdatePartsAvailability(PlacedUniqueOrganelles.ToList());

        UpdatePatchDependentBalanceData();

        gui.UpdateSpeed(CalculateSpeed());

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
                else if (PlacedThisSession(organelle))
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
                    placedModels.Add(CreateEditorOrganelle());
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

    private bool PlacedThisSession(OrganelleTemplate organelle)
    {
        return History.OrganellePlacedThisSession(organelle);
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
    private void DoMembraneChangeAction(MembraneActionData data)
    {
        var membrane = data.NewMembrane;
        GD.Print("Changing membrane to '", membrane.InternalName, "'");
        Membrane = membrane;
        OnMembraneChanged();
        gui.UpdateMembraneButtons(Membrane.InternalName);
        gui.UpdateSpeed(CalculateSpeed());
        gui.UpdateHitpoints(CalculateHitpoints());
        CalculateEnergyBalanceWithOrganellesAndMembraneType(
            editedMicrobeOrganelles.Organelles, Membrane, targetPatch);
        gui.SetMembraneTooltips(Membrane);

        StartAutoEvoPrediction();

        if (previewMicrobe != null)
        {
            previewMicrobe.Membrane.Type = membrane;
            previewMicrobe.Membrane.Dirty = true;
            previewMicrobe.ApplyMembraneWigglyness();
        }
    }

    [DeserializedCallbackAllowed]
    private void UndoMembraneChangeAction(MembraneActionData data)
    {
        Membrane = data.OldMembrane;
        GD.Print("Changing membrane back to '", Membrane.InternalName, "'");
        OnMembraneChanged();
    }

    private void OnMembraneChanged()
    {
        gui.UpdateMembraneButtons(Membrane.InternalName);
        gui.UpdateSpeed(CalculateSpeed());
        gui.UpdateHitpoints(CalculateHitpoints());
        CalculateEnergyBalanceWithOrganellesAndMembraneType(
            editedMicrobeOrganelles.Organelles, Membrane, targetPatch);
        gui.SetMembraneTooltips(Membrane);

        StartAutoEvoPrediction();

        if (previewMicrobe != null)
        {
            previewMicrobe.Membrane.Type = Membrane;
            previewMicrobe.Membrane.Dirty = true;
            previewMicrobe.ApplyMembraneWigglyness();
        }
    }

    [DeserializedCallbackAllowed]
    private void DoBehaviourChangeAction(BehaviourActionData data)
    {
        if (Behaviour == null)
            throw new InvalidOperationException($"Editor has no {nameof(Behaviour)} set for change action to use");

        Behaviour[data.Type] = data.NewValue;
        gui.UpdateBehaviourSlider(data.Type, data.NewValue);
    }

    [DeserializedCallbackAllowed]
    private void UndoBehaviourChangeAction(BehaviourActionData data)
    {
        if (Behaviour == null)
            throw new InvalidOperationException($"Editor has no {nameof(Behaviour)} set for change action to use");

        Behaviour[data.Type] = data.OldValue;
        gui.UpdateBehaviourSlider(data.Type, data.OldValue);
    }

    [DeserializedCallbackAllowed]
    private void DoRigidityChangeAction(RigidityActionData data)
    {
        Rigidity = data.NewRigidity;

        // TODO: when rigidity affects auto-evo this also needs to re-run the prediction, though there should probably
        // be some kind of throttling, this also applies to the behaviour values

        OnRigidityChanged();
    }

    [DeserializedCallbackAllowed]
    private void UndoRigidityChangeAction(RigidityActionData data)
    {
        Rigidity = data.PreviousRigidity;
        OnRigidityChanged();
    }

    private void OnRigidityChanged()
    {
        gui.UpdateRigiditySlider((int)Math.Round(Rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO));

        gui.UpdateSpeed(CalculateSpeed());
        gui.UpdateHitpoints(CalculateHitpoints());
    }

    /// <summary>
    ///   Perform all actions through this to make undo and redo work
    /// </summary>
    /// <returns>True when the action was successful</returns>
    /// <param name="action">The main action that will go into the history</param>
    private bool EnqueueAction(MicrobeEditorAction action)
    {
        var actionData = action.Data.ToList();

        // A sanity check to not let an action proceed if we don't have enough mutation points
        if (!(FreeBuilding || CheatManager.InfiniteMP) &&
            History.WhatWouldActionsCost(actionData) > MutationPoints)
        {
            // Flash the MP bar and play sound
            gui.OnInsufficientMp();
            return false;
        }

        // Block creating a new microbe when moving an organelle
        if (MovingOrganelles != null && actionData.OfType<NewMicrobeActionData>().Any())
        {
            // Play sound
            gui.OnActionBlockedWhileMoving();
            return false;
        }

        History.AddAction(action);

        DirtyMutationPointsCache();

        UpdateUndoRedoButtons();
        return true;
    }

    private void UpdateUndoRedoButtons()
    {
        gui.SetUndoButtonEnabled(History.CanUndo() && MovingOrganelles == null);
        gui.SetRedoButtonEnabled(History.CanRedo() && MovingOrganelles == null);
    }

    private void UpdateSymmetryButton()
    {
        gui.SetSymmetryButtonEnabled(MovingOrganelles == null);
    }

    /// <summary>
    ///   Called once auto-evo results are ready
    /// </summary>
    private void OnEditorReady()
    {
        Ready = true;
        LoadingScreen.Instance.Hide();

        GD.Print("Elapsing time on editor entry");

        // TODO: select which units will be used for the master elapsed time counter
        CurrentGame!.GameWorld.OnTimePassed(1);

        gui.UpdateTimeIndicator(CurrentGame.GameWorld.TotalPassedTime);

        // Get summary before applying results in order to get comparisons to the previous populations
        var run = CurrentGame.GameWorld.GetAutoEvoRun();

        if (run.Results == null)
        {
            gui.UpdateAutoEvoResults(TranslationServer.Translate("AUTO_EVO_FAILED"),
                TranslationServer.Translate("AUTO_EVO_RUN_STATUS") + " " + run.Status);
        }
        else
        {
            autoEvoSummary = run.Results.MakeSummary(CurrentGame.GameWorld.Map, true, run.ExternalEffects);
            autoEvoExternal = run.MakeSummaryOfExternalEffects();

            run.Results.LogResultsToTimeline(CurrentGame.GameWorld, run.ExternalEffects);

            gui.UpdateAutoEvoResults(autoEvoSummary.ToString(), autoEvoExternal.ToString());
        }

        ApplyAutoEvoResults();

        gui.UpdateReportTabStatistics(CurrentPatch);
        gui.UpdateTimeline();

        FadeIn();
    }

    private void OnLoadedEditorReady()
    {
        if (Ready != true)
            throw new InvalidOperationException("loaded editor isn't in the ready state");

        // The error conditions here probably shouldn't be able to trigger at all
        gui.UpdateAutoEvoResults(autoEvoSummary?.ToString() ?? "error", autoEvoExternal?.ToString() ?? "error");

        gui.UpdateTimeIndicator(CurrentGame!.GameWorld.TotalPassedTime);

        // Make absolutely sure the current game doesn't have an auto-evo run
        CurrentGame.GameWorld.ResetAutoEvoRun();

        gui.UpdateReportTabStatistics(CurrentPatch);
        gui.UpdateTimeline();

        FadeIn();
    }

    private void ApplyAutoEvoResults()
    {
        var run = CurrentGame!.GameWorld.GetAutoEvoRun();
        GD.Print("Applying auto-evo results. Auto-evo run took: ", run.RunDuration);
        run.ApplyExternalEffects();

        CurrentGame.GameWorld.Map.UpdateGlobalTimePeriod(CurrentGame.GameWorld.TotalPassedTime);

        // Update populations before recording conditions - should not affect per-patch population
        CurrentGame.GameWorld.Map.UpdateGlobalPopulations();

        // Needs to be before the remove extinct species call, so that extinct species could still be stored
        // for reference in patch history (e.g. displaying it as zero on the species population chart)
        foreach (var entry in CurrentGame.GameWorld.Map.Patches)
        {
            entry.Value.RecordSnapshot(true);
        }

        var extinct = CurrentGame.GameWorld.Map.RemoveExtinctSpecies(FreeBuilding);

        foreach (var species in extinct)
        {
            CurrentGame.GameWorld.RemoveSpecies(species);

            GD.Print("Species ", species.FormattedName, " has gone extinct from the world.");
        }

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

    /// <summary>
    ///   Starts a fade in transition
    /// </summary>
    private void FadeIn()
    {
        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeIn, 0.5f);
        TransitionManager.Instance.StartTransitions(this, nameof(OnFinishTransitioning));
    }

    private void SaveGame(string name)
    {
        SaveHelper.Save(name, this);
    }
}
