using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Editor component that specializes in hex-based stuff editing
/// </summary>
/// <typeparam name="TEditor">Type of editor this class can be put in</typeparam>
/// <typeparam name="TCombinedAction">Type of editor action this class works with</typeparam>
/// <typeparam name="TAction">Type of single action this works with</typeparam>
/// <typeparam name="THexMove">Type of hex move action</typeparam>
/// <typeparam name="TContext">Extra action context data this manages on actions</typeparam>
[GodotAbstract]
public partial class HexEditorComponentBase<TEditor, TCombinedAction, TAction, THexMove, TContext> :
    EditorComponentWithActionsBase<TEditor, TCombinedAction>,
    ISaveLoadedTracked, IChildPropertiesLoadCallback
    where TEditor : class, IHexEditor, IEditorWithActions
    where TCombinedAction : CombinedEditorAction
    where TAction : EditorAction
    where THexMove : class, IActionHex
{
    [Export]
    public NodePath? CameraPath;

    [Export]
    public NodePath EditorArrowPath = null!;

    [Export]
    public NodePath EditorGridPath = null!;

    [Export]
    public NodePath CameraFollowPath = null!;

    [Export]
    public NodePath IslandErrorPath = null!;

    /// <summary>
    ///   The hexes that are positioned under the cursor to show where the player is about to place something.
    /// </summary>
    protected readonly List<MeshInstance3D> hoverHexes = new();

    /// <summary>
    ///   The sample models that are positioned to show what the player is about to place.
    /// </summary>
    protected readonly List<SceneDisplayer> hoverModels = new();

    /// <summary>
    ///   This is the hexes for the edited thing that are placed; this is the already placed hexes
    /// </summary>
    protected readonly List<MeshInstance3D> placedHexes = new();

    /// <summary>
    ///   The hexes that have been changed by a hovering hex and need to be reset to old material.
    /// </summary>
    protected readonly Dictionary<MeshInstance3D, Material> hoverOverriddenMaterials = new();

    /// <summary>
    ///   This is the placed down version of models, compare to <see cref="hoverModels"/>
    /// </summary>
    protected readonly List<SceneDisplayer> placedModels = new();

#pragma warning disable CA2213

    /// <summary>
    ///   Object camera is over. Used to move the camera around
    /// </summary>
    protected Node3D cameraFollow = null!;

    protected MicrobeCamera? camera;

    [JsonIgnore]
    protected MeshInstance3D editorArrow = null!;

    protected MeshInstance3D editorGrid = null!;

    protected Material invalidMaterial = null!;
    protected Material validMaterial = null!;
    protected Material oldMaterial = null!;
    protected Material islandMaterial = null!;

    protected PackedScene hexScene = null!;
    protected PackedScene modelScene = null!;

    protected AudioStream hexPlacementSound = null!;
#pragma warning restore CA2213

    [JsonProperty]
    protected string? activeActionName;

    /// <summary>
    ///   This is a global assessment if the currently being placed thing / action is valid (if not all hover hexes
    ///   will be shown as invalid)
    /// </summary>
    protected bool isPlacementProbablyValid;

    /// <summary>
    ///   This is used to keep track of used hover hexes
    /// </summary>
    protected int usedHoverHex;

    protected int usedHoverModel;

    [JsonProperty]
    protected int placementRotation;

    private readonly NodePath positionZReference = new("position:z");

    // This is separate from the other Godot resources as this is private and they are protected
#pragma warning disable CA2213
    private CustomConfirmationDialog islandPopup = null!;
#pragma warning restore CA2213

    private HexEditorSymmetry symmetry = HexEditorSymmetry.None;

    private IEnumerable<(Hex Hex, int Orientation)>? mouseHoverPositions;

    private Vector3 cameraPosition;

    /// <summary>
    ///   Where the user started panning with the mouse. Null if the user is not panning with the mouse
    /// </summary>
    private Vector3? mousePanningStart;

    protected HexEditorComponentBase()
    {
    }

    /// <summary>
    ///   The symmetry setting of the editor.
    /// </summary>
    [JsonProperty]
    public HexEditorSymmetry Symmetry
    {
        get => symmetry;
        set => symmetry = value;
    }

    /// <summary>
    ///   Hex that is in the process of being moved but a new location hasn't been selected yet.
    ///   If null, nothing is in the process of moving.
    /// </summary>
    [JsonProperty]
    public THexMove? MovingPlacedHex { get; protected set; }

    /// <summary>
    ///   Camera position. Y-position should always be 0
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is a separate property instead of using <see cref="AssignOnlyChildItemsOnDeserializeAttribute"/>
    ///     as this component derived scenes don't have the camera paths set (as they are on the higher level).
    ///     This approach also allows different editor components to remember where they placed the camera.
    ///   </para>
    /// </remarks>
    [JsonProperty]
    public Vector3 CameraPosition
    {
        get => cameraPosition;
        set
        {
            cameraPosition = value;
            UpdateCamera();
        }
    }

    [JsonProperty]
    public float CameraHeight { get; private set; } = Constants.EDITOR_DEFAULT_CAMERA_HEIGHT;

    [JsonIgnore]
    public IEnumerable<(Hex Hex, int Orientation)>? MouseHoverPositions
    {
        get => mouseHoverPositions;
        set
        {
            if (mouseHoverPositions == null && value == null)
                return;

            if (mouseHoverPositions != null && value != null && mouseHoverPositions.SequenceEqual(value))
                return;

            mouseHoverPositions = value;
            UpdateMutationPointsBar();
        }
    }

    /// <summary>
    ///   If true a hex move is in progress and can be canceled
    /// </summary>
    [JsonIgnore]
    public bool CanCancelMove => MovingPlacedHex != null;

    [JsonIgnore]
    public override bool CanCancelAction => CanCancelMove;

    [JsonIgnore]
    public virtual bool HasIslands => throw new GodotAbstractPropertyNotOverriddenException();

    public bool IsLoadedFromSave { get; set; }

    protected virtual bool ForceHideHover => throw new GodotAbstractPropertyNotOverriddenException();

    public override void _Ready()
    {
        base._Ready();

        ResolveNodeReferences();

        LoadHexMaterials();
        LoadScenes();
        LoadAudioStreams();

        UpdateCamera();
    }

    public virtual void ResolveNodeReferences()
    {
        islandPopup = GetNode<CustomConfirmationDialog>(IslandErrorPath);

        if (IsLoadedFromSave)
        {
            // When directly loaded from the base scene (which is done when loading from a save), some of our
            // node paths are not set so we need to skip them
            return;
        }

        camera = GetNode<MicrobeCamera>(CameraPath);
        editorArrow = GetNode<MeshInstance3D>(EditorArrowPath);
        editorGrid = GetNode<MeshInstance3D>(EditorGridPath);
        cameraFollow = GetNode<Node3D>(CameraFollowPath);

        camera.Connect(MicrobeCamera.SignalName.OnZoomChanged, new Callable(this, nameof(OnZoomChanged)));
    }

    public override void Init(TEditor owningEditor, bool fresh)
    {
        base.Init(owningEditor, fresh);

        if (camera == null)
        {
            throw new InvalidOperationException(
                "This editor component was loaded from a save and is not fully functional");
        }

        if (fresh)
        {
            placementRotation = 0;

            ResetSymmetryButton();
        }

        UpdateSymmetryIcon();

        // For now we never reuse editors so it isn't worth the trouble to have code to properly clear these
        if (hoverHexes.Count > 0 || hoverModels.Count > 0 || hoverOverriddenMaterials.Count > 0)
            throw new InvalidOperationException("This editor has already been initialized (hexes not empty)");

        // Create new hover hexes. See the TODO comment in _Process
        // This seems really cluttered, there must be a better way.
        for (int i = 0; i < Constants.MAX_HOVER_HEXES; ++i)
        {
            hoverHexes.Add(CreateEditorHex());
        }

        for (int i = 0; i < Constants.MAX_SYMMETRY; ++i)
        {
            hoverModels.Add(CreatePreviewModelHolder());
        }

        // The world is reset each time so these are gone. We throw an exception if that's not the case as that
        // indicates a programming bug
        if (placedHexes.Count > 0 || placedModels.Count > 0)
            throw new InvalidOperationException("This editor has already been initialized (placed hexes not empty)");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // We move all the hexes and the hover hexes to 0,0,0 so that
        // the editor is free to replace them wherever
        // TODO: it would be way better if we didn't have to do this and instead only updated
        // the hover hexes and models when there is some change to them
        foreach (var hex in hoverHexes)
        {
            hex.Position = new Vector3(0, 0, 0);
            hex.Visible = false;
        }

        foreach (var model in hoverModels)
        {
            model.Position = new Vector3(0, 0, 0);
            model.Visible = false;
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
        usedHoverModel = 0;

        if (!Visible)
            return;

        editorGrid.Position = camera!.CursorWorldPos;
        editorGrid.Visible = Editor.ShowHover && !ForceHideHover;

        camera.UpdateCameraPosition(delta, cameraFollow.GlobalPosition);
    }

    public void ResetSymmetryButton()
    {
        componentBottomLeftButtons.ResetSymmetry();
        symmetry = 0;
    }

    /// <summary>
    ///   Set tab specific editor world object visibility
    /// </summary>
    /// <param name="shown">True if they should be visible</param>
    public virtual void SetEditorWorldTabSpecificObjectVisibility(bool shown)
    {
        SetEditorWorldGuideObjectVisibility(shown);

        if (!shown)
        {
            foreach (var hoverHex in hoverHexes)
            {
                hoverHex.Visible = false;
            }

            foreach (var hoverModel in hoverModels)
            {
                hoverModel.Visible = false;
            }
        }

        foreach (var placedHex in placedHexes)
        {
            placedHex.Visible = shown;
        }

        foreach (var placedModel in placedModels)
        {
            placedModel.Visible = shown;
        }
    }

    public void SetEditorWorldGuideObjectVisibility(bool shown)
    {
        editorArrow.Visible = shown;
        editorGrid.Visible = shown;
    }

    public void UpdateCamera()
    {
        if (camera == null)
            return;

        camera.CameraHeight = CameraHeight;
        cameraFollow.Position = CameraPosition;
    }

    /// <summary>
    ///   Updates the background shown in the editor
    /// </summary>
    public void UpdateBackgroundImage(Biome biomeToUseBackgroundFrom)
    {
        // TODO: make this be loaded in a background thread to avoid a lag spike
        camera!.SetBackground(SimulationParameters.Instance.GetBackground(biomeToUseBackgroundFrom.Background));
    }

    [RunOnKeyDown("e_primary")]
    public virtual bool PerformPrimaryAction()
    {
        if (!Visible)
            return false;

        if (MovingPlacedHex != null)
        {
            GetMouseHex(out int q, out int r);
            PerformMove(q, r);
        }
        else
        {
            if (string.IsNullOrEmpty(activeActionName))
                return true;

            PerformActiveAction();
        }

        return true;
    }

    [RunOnAxisGroup]
    [RunOnAxis(new[] { "e_pan_up", "e_pan_down" }, new[] { -1.0f, 1.0f })]
    [RunOnAxis(new[] { "e_pan_left", "e_pan_right" }, new[] { -1.0f, 1.0f })]
    public bool PanCameraWithKeys(double delta, float upDown, float leftRight)
    {
        if (!Visible)
            return false;

        if (mousePanningStart != null)
            return true;

        var movement = new Vector3(leftRight, 0, upDown);
        MoveCamera(movement.Normalized() * (float)delta * CameraHeight);
        return true;
    }

    [RunOnKey("e_pan_mouse", CallbackRequiresElapsedTime = false, OnlyUnhandled = false)]
    public bool PanCameraWithMouse(double dummy)
    {
        _ = dummy;

        // TODO: somehow this doesn't seem to experience the same bug as there is in EditorCamera3D where this needs a
        // workaround
        if (!Visible)
            return false;

        if (mousePanningStart == null)
        {
            mousePanningStart = camera!.CursorWorldPos;
        }
        else
        {
            var mousePanDirection = mousePanningStart.Value - camera!.CursorWorldPos;
            MoveCamera(mousePanDirection);
        }

        return false;
    }

    [RunOnKeyUp("e_pan_mouse", OnlyUnhandled = false)]
    public bool ReleasePanCameraWithMouse()
    {
        mousePanningStart = null;
        return false;
    }

    [RunOnKeyDown("e_reset_camera")]
    public bool ResetCamera()
    {
        if (!Visible)
            return false;

        if (camera == null)
        {
            GD.PrintErr("Editor camera isn't set");
            return false;
        }

        CameraPosition = new Vector3(0, 0, 0);
        UpdateCamera();

        camera.ResetHeight();
        return true;
    }

    [RunOnKeyDown("e_rotate_right")]
    public bool RotateRight()
    {
        if (!Visible)
            return false;

        placementRotation = (placementRotation + 1) % 6;
        return true;
    }

    [RunOnKeyDown("e_rotate_left")]
    public bool RotateLeft()
    {
        if (!Visible)
            return false;

        --placementRotation;

        if (placementRotation < 0)
            placementRotation = 5;

        return true;
    }

    /// <summary>
    ///   Cancels the current editor action
    /// </summary>
    /// <returns>True when the input is consumed</returns>
    [RunOnKeyDown("e_cancel_current_action", Priority = 1)]
    public virtual bool CancelCurrentAction()
    {
        if (!Visible)
            return false;

        if (MovingPlacedHex != null)
        {
            OnCurrentActionCanceled();

            return true;
        }

        return false;
    }

    /// <summary>
    ///   Begin hex movement under the cursor
    /// </summary>
    [RunOnKeyDown("e_move")]
    public bool StartHexMoveAtCursor()
    {
        if (!Visible)
            return false;

        // Can't move anything while already moving one (or another pending action)
        if (MovingPlacedHex != null || CanCancelAction)
        {
            Editor.OnActionBlockedWhileMoving();
            return true;
        }

        GetMouseHex(out int q, out int r);

        var hex = GetHexAt(new Hex(q, r));

        if (hex == null)
            return true;

        StartHexMove(hex);
        return true;
    }

    public void StartHexMove(THexMove selectedHex)
    {
        if (MovingPlacedHex != null || CanCancelAction)
        {
            // Already moving something! some code went wrong
            throw new InvalidOperationException(
                "Can't begin hex move while another in progress (or another in-progress action)");
        }

        MovingPlacedHex = selectedHex;

        OnMoveActionStarted();
        OnActionStatusChanged();
    }

    public void StartHexMoveWithSymmetry(IEnumerable<THexMove> selectedHexes)
    {
        // TODO: implement: https://github.com/Revolutionary-Games/Thrive/issues/3318
        throw new NotImplementedException();

        /*
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
        Editor.NotifyUndoRedoStateChanged();
        Editor.NotifySymmetryAllowedStateChanged();
        */
    }

    public void RemoveHex(Hex hex)
    {
        var actions = new List<TAction>();
        int alreadyDeleted = 0;

        RunWithSymmetry(hex.Q, hex.R, (q, r, _) =>
        {
            var removed = TryCreateRemoveHexAtAction(new Hex(q, r), ref alreadyDeleted);

            if (removed != null)
                actions.Add(removed);
        });

        if (actions.Count < 1)
            return;

        var combinedAction = CreateCombinedAction(actions);

        EnqueueAction(combinedAction);
    }

    /// <summary>
    ///   Remove the hex under the cursor (if there is one)
    /// </summary>
    [RunOnKeyDown("e_delete")]
    public void RemoveHexAtCursor()
    {
        GetMouseHex(out int q, out int r);

        Hex mouseHex = new Hex(q, r);

        var hex = GetHexAt(mouseHex);

        if (hex == null)
            return;

        RemoveHex(mouseHex);
    }

    public override bool CanFinishEditing(IEnumerable<EditorUserOverride> userOverrides)
    {
        if (!base.CanFinishEditing(userOverrides))
            return false;

        // Can't exit the editor with disconnected organelles
        if (HasIslands)
        {
            islandPopup.PopupCenteredShrink();
            return false;
        }

        return true;
    }

    public override void OnValidAction(IEnumerable<CombinableActionData> actions)
    {
        var anyPlacement = typeof(HexPlacementActionData<,>);
        var anyMove = typeof(HexMoveActionData<,>);

        foreach (var data in actions)
        {
            var type = data.GetType();
            if (type.IsAssignableToGenericType(anyPlacement) || type.IsAssignableToGenericType(anyMove))
            {
                PlayHexPlacementSound();
                break;
            }
        }
    }

    public void PlayHexPlacementSound()
    {
        GUICommon.Instance.PlayCustomSound(hexPlacementSound, 0.7f);
    }

    public void OnNoPropertiesLoaded()
    {
        // Something is wrong if a hex editor has this method called on it
        throw new InvalidOperationException();
    }

    public virtual void OnPropertiesLoaded()
    {
        // A bit of a hack to make sure our camera doesn't lose its zoom level
        camera!.IsLoadedFromSave = true;
    }

    /// <summary>
    ///   Updates the forward pointing arrow to not overlap the edited species. Should be called on any layout change.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is public so that editors with multiple tabs can make the arrow fix itself after switching tabs
    ///   </para>
    /// </remarks>
    public void UpdateArrow(bool animateMovement = true)
    {
        var arrowPosition = CalculateEditorArrowZPosition() - Constants.EDITOR_ARROW_OFFSET;

        if (animateMovement)
        {
            var tween = CreateTween();
            tween.SetTrans(Tween.TransitionType.Expo);
            tween.SetEase(Tween.EaseType.Out);

            tween.TweenProperty(editorArrow, positionZReference, arrowPosition,
                Constants.EDITOR_ARROW_INTERPOLATE_SPEED);
        }
        else
        {
            editorArrow.Position = new Vector3(0, 0, arrowPosition);
        }
    }

    protected MeshInstance3D CreateEditorHex()
    {
        var hex = (MeshInstance3D)hexScene.Instantiate();
        Editor.RootOfDynamicallySpawned.AddChild(hex);
        return hex;
    }

    protected SceneDisplayer CreatePreviewModelHolder()
    {
        var node = (SceneDisplayer)modelScene.Instantiate();
        Editor.RootOfDynamicallySpawned.AddChild(node);
        return node;
    }

    protected virtual void LoadHexMaterials()
    {
        invalidMaterial = GD.Load<Material>("res://src/microbe_stage/editor/InvalidHex.material");
        validMaterial = GD.Load<Material>("res://src/microbe_stage/editor/ValidHex.material");
        oldMaterial = GD.Load<Material>("res://src/microbe_stage/editor/OldHex.material");
        islandMaterial = GD.Load<Material>("res://src/microbe_stage/editor/IslandHex.material");
    }

    protected virtual void LoadScenes()
    {
        hexScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/EditorHex.tscn");
        modelScene = GD.Load<PackedScene>("res://src/general/SceneDisplayer.tscn");
    }

    protected virtual void LoadAudioStreams()
    {
        hexPlacementSound = GD.Load<AudioStream>("res://assets/sounds/soundeffects/gui/click_place_success.ogg");
    }

    protected override bool EnqueueAction(TCombinedAction action)
    {
        if (!Editor.CheckEnoughMPForAction(Editor.WhatWouldActionsCost(action.Data)))
            return false;

        if (CanCancelAction)
        {
            if (!DoesActionEndInProgressAction(action))
            {
                // Play sound
                Editor.OnActionBlockedWhileMoving();
                return false;
            }
        }

        OnMoveWillSucceed();

        Editor.EnqueueAction(action);
        Editor.OnValidAction(action.Data);
        UpdateSymmetryButton();
        return true;
    }

    protected virtual TCombinedAction CreateCombinedAction(IEnumerable<EditorAction> actions)
    {
        return (TCombinedAction)new CombinedEditorAction(actions);
    }

    protected override void OnActionStatusChanged()
    {
        base.OnActionStatusChanged();

        // TODO: change this to go through the editor object for consistency
        UpdateSymmetryButton();
    }

    protected void OnSymmetryPressed()
    {
        if (symmetry == HexEditorSymmetry.SixWaySymmetry)
        {
            ResetSymmetryButton();
        }
        else if (symmetry == HexEditorSymmetry.None)
        {
            symmetry = HexEditorSymmetry.XAxisSymmetry;
        }
        else if (symmetry == HexEditorSymmetry.XAxisSymmetry)
        {
            symmetry = HexEditorSymmetry.FourWaySymmetry;
        }
        else if (symmetry == HexEditorSymmetry.FourWaySymmetry)
        {
            symmetry = HexEditorSymmetry.SixWaySymmetry;
        }

        Symmetry = symmetry;
        UpdateSymmetryIcon();
    }

    /// <summary>
    ///   Called once when the mouse enters the background.
    /// </summary>
    protected void OnHexEditorMouseEntered()
    {
        if (!Visible)
            return;

        Editor.ShowHover = true;
        UpdateMutationPointsBar();
    }

    /// <summary>
    ///   Called when the mouse is no longer hovering the background.
    /// </summary>
    protected void OnHexEditorMouseExited()
    {
        Editor.ShowHover = false;
        UpdateMutationPointsBar();
    }

    /// <summary>
    ///   To get MouseEnter/Exit the CellEditor needs MouseFilter != Ignore.
    ///   Controls with MouseFilter != Ignore always handle mouse events.
    ///   So to get MouseClicks via the normal InputManager, this must be forwarded.
    ///   This is needed to respect the current Key Settings.
    /// </summary>
    /// <param name="inputEvent">The event the user fired</param>
    protected void OnHexEditorGuiInput(InputEvent inputEvent)
    {
        if (!Editor.ShowHover)
            return;

        InputManager.ForwardInput(inputEvent);
    }

    /// <summary>
    ///   Returns the hex position the mouse is over
    /// </summary>
    protected void GetMouseHex(out int q, out int r)
    {
        // Get the position of the cursor in the plane that the microbes is floating in
        var cursorPos = camera!.CursorWorldPos;

        // Convert to the hex the cursor is currently located over.
        var hex = Hex.CartesianToAxial(cursorPos);

        q = hex.Q;
        r = hex.R;
    }

    /// <summary>
    ///   Runs given callback for all symmetry positions and rotations
    /// </summary>
    /// <param name="q">The base q</param>
    /// <param name="r">The base r value of the coordinate</param>
    /// <param name="callback">The callback that is called based on symmetry, parameters are: q, r, rotation</param>
    /// <param name="overrideSymmetry">If set, overrides the current symmetry</param>
    protected void RunWithSymmetry(int q, int r, Action<int, int, int> callback,
        HexEditorSymmetry? overrideSymmetry = null)
    {
        overrideSymmetry ??= Symmetry;

        switch (overrideSymmetry)
        {
            case HexEditorSymmetry.None:
            {
                callback(q, r, placementRotation);
                break;
            }

            case HexEditorSymmetry.XAxisSymmetry:
            {
                callback(q, r, placementRotation);

                if (q != -1 * q || r != r + q)
                {
                    callback(-1 * q, r + q, 6 + (-1 * placementRotation));
                }

                break;
            }

            case HexEditorSymmetry.FourWaySymmetry:
            {
                callback(q, r, placementRotation);

                if (q != -1 * q || r != r + q)
                {
                    callback(-1 * q, r + q, 6 + (-1 * placementRotation));
                    callback(-1 * q, -1 * r, (placementRotation + 3) % 6);
                    callback(q, -1 * (r + q), 9 + (-1 * placementRotation) % 6);
                }
                else
                {
                    callback(-1 * q, -1 * r, (placementRotation + 3) % 6);
                }

                break;
            }

            case HexEditorSymmetry.SixWaySymmetry:
            {
                callback(q, r, placementRotation);
                callback(-1 * r, r + q, (placementRotation + 1) % 6);
                callback(-1 * (r + q), q, (placementRotation + 2) % 6);
                callback(-1 * q, -1 * r, (placementRotation + 3) % 6);
                callback(r, -1 * (r + q), (placementRotation + 4) % 6);
                callback(r + q, -1 * q, (placementRotation + 5) % 6);
                break;
            }
        }
    }

    protected override void OnCurrentActionCanceled()
    {
        base.OnCurrentActionCanceled();

        // TODO: switch to this going through the editor
        UpdateSymmetryButton();
    }

    protected virtual void OnMoveWillSucceed()
    {
        MovingPlacedHex = null;

        // Move succeeded; Update the cancel button visibility, so it is not hidden because the move has completed
        OnActionStatusChanged();
    }

    /// <summary>
    ///   Handles positioning hover hexes at the coordinates to show what is about to be places. Handles conflicts with
    ///   already placed hexes. <see cref="isPlacementProbablyValid"/> should be set to an initial good value before
    ///   calling this.
    /// </summary>
    /// <param name="q">Q coordinate</param>
    /// <param name="r">R coordinate</param>
    /// <param name="toBePlacedHexes">
    ///   List of hexes to show at the coordinates, need to have at least one to do anything useful
    /// </param>
    /// <param name="canPlace">
    ///   True if the editor logic thinks this is a valid placement (selects material for used hover hexes)
    /// </param>
    /// <param name="hadDuplicate">Set to true if an already placed hex was conflicted with</param>
    protected void RenderHoveredHex(int q, int r, IEnumerable<Hex> toBePlacedHexes, bool canPlace,
        out bool hadDuplicate)
    {
        hadDuplicate = false;

        foreach (var hex in toBePlacedHexes)
        {
            int posQ = hex.Q + q;
            int posR = hex.R + r;

            var pos = Hex.AxialToCartesian(new Hex(posQ, posR));

            bool duplicate = false;

            // Skip if there is a placed organelle here already
            foreach (var placed in placedHexes)
            {
                if ((pos - placed.Position).LengthSquared() < 0.001f)
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

                        hadDuplicate = true;
                    }

                    break;
                }
            }

            // Or if there is already a hover hex at this position
            for (int i = 0; i < usedHoverHex; ++i)
            {
                if ((pos - hoverHexes[i].Position).LengthSquared() < 0.001f)
                {
                    duplicate = true;
                    break;
                }
            }

            if (duplicate)
                continue;

            var hoverHex = hoverHexes[usedHoverHex++];

            hoverHex.Position = pos;
            hoverHex.Visible = true;

            hoverHex.MaterialOverride = canPlace ? validMaterial : invalidMaterial;
        }
    }

    protected void UpdateAlreadyPlacedHexes(
        IEnumerable<(Hex BasePosition, IEnumerable<Hex> Hexes, bool PlacedThisSession)> hexes, List<Hex> islands,
        bool forceHide = false)
    {
        int nextFreeHex = 0;

        foreach (var (position, itemHexes, placedThisSession) in hexes)
        {
            foreach (var hex in itemHexes)
            {
                var pos = Hex.AxialToCartesian(hex + position);

                if (nextFreeHex >= placedHexes.Count)
                {
                    // New hex needed
                    placedHexes.Add(CreateEditorHex());
                }

                var hexNode = placedHexes[nextFreeHex++];

                if (islands.Contains(position))
                {
                    hexNode.MaterialOverride = islandMaterial;
                }
                else if (placedThisSession)
                {
                    hexNode.MaterialOverride = validMaterial;
                }
                else
                {
                    hexNode.MaterialOverride = oldMaterial;
                }

                // As we set the correct material, we don't need to remember to restore it anymore
                hoverOverriddenMaterials.Remove(hexNode);

                hexNode.Position = pos;

                hexNode.Visible = !forceHide;
            }
        }

        // Delete excess entities
        while (nextFreeHex < placedHexes.Count)
        {
            placedHexes[placedHexes.Count - 1].DetachAndQueueFree();
            placedHexes.RemoveAt(placedHexes.Count - 1);
        }
    }

    protected virtual void PerformActiveAction()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual bool DoesActionEndInProgressAction(TCombinedAction action)
    {
        // Allow only move actions with an in-progress move
        return action.Data.Any(d => d is HexMoveActionData<THexMove, TContext>);
    }

    /// <summary>
    ///   Checks if the target position is valid to place hex.
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="rotation">
    ///   The rotation to check for the hex (only makes sense when placing a group of hexes)
    /// </param>
    /// <param name="hex">The move data to try to move to the position</param>
    /// <returns>True if valid</returns>
    protected virtual bool IsMoveTargetValid(Hex position, int rotation, THexMove hex)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual void OnMoveActionStarted()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual void PerformMove(int q, int r)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual THexMove? GetHexAt(Hex position)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual TAction? TryCreateRemoveHexAtAction(Hex location, ref int alreadyDeleted)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual float CalculateEditorArrowZPosition()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected void UpdateSymmetryButton()
    {
        componentBottomLeftButtons.SymmetryEnabled = !CanCancelAction;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (CameraPath != null)
            {
                CameraPath.Dispose();
                EditorArrowPath.Dispose();
                EditorGridPath.Dispose();
                CameraFollowPath.Dispose();
                IslandErrorPath.Dispose();
            }

            positionZReference.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Moves the camera in a direction (note that height (y-axis) should not be used)
    /// </summary>
    /// <param name="vector">The direction to move the camera</param>
    private void MoveCamera(Vector3 vector)
    {
        CameraPosition += vector;
    }

    private void OnZoomChanged(float zoom)
    {
        CameraHeight = zoom;
    }

    // TODO: make this method trigger automatically on Symmetry assignment
    private void UpdateSymmetryIcon()
    {
        componentBottomLeftButtons.SetSymmetry(symmetry);
    }
}
