using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Editor component that specializes in hex-based stuff editing
/// </summary>
public abstract class
    HexEditorComponentBase<TEditor, TAction, THexMove> : EditorComponentWithActionsBase<TEditor, TAction>
    where TEditor : Godot.Object, IHexEditor
    where TAction : MicrobeEditorAction
    where THexMove : class
{
    [Export]
    public NodePath SymmetryButtonPath = null!;

    [Export]
    public NodePath SymmetryIconPath = null!;

    [Export]
    public NodePath CameraPath = null!;

    [Export]
    public NodePath EditorArrowPath = null!;

    [Export]
    public NodePath EditorGridPath = null!;

    [Export]
    public NodePath CameraFollowPath = null!;

    [Export]
    public NodePath IslandErrorPath = null!;

    private TextureButton symmetryButton = null!;
    private TextureRect symmetryIcon = null!;

    private Texture symmetryIconDefault = null!;
    private Texture symmetryIcon2X = null!;
    private Texture symmetryIcon4X = null!;
    private Texture symmetryIcon6X = null!;

    private CustomConfirmationDialog islandPopup = null!;

    private HexEditorSymmetry symmetry = HexEditorSymmetry.None;

    /// <summary>
    ///   The hexes that are positioned under the cursor to show where the player is about to place something.
    /// </summary>
    protected readonly List<MeshInstance> hoverHexes = new();

    /// <summary>
    ///   The sample models that are positioned to show what the player is about to place.
    /// </summary>
    protected readonly List<SceneDisplayer> hoverModels = new();

    /// <summary>
    ///   This is the hexes for the edited thing that are placed; this is the already placed hexes
    /// </summary>
    protected readonly List<MeshInstance> placedHexes = new();

    /// <summary>
    ///   The hexes that have been changed by a hovering hex and need to be reset to old material.
    /// </summary>
    protected readonly Dictionary<MeshInstance, Material> hoverOverriddenMaterials = new();

    /// <summary>
    ///   This is the placed down version of models, compare to <see cref="hoverModels"/>
    /// </summary>
    protected readonly List<SceneDisplayer> placedModels = new();

    /// <summary>
    ///   Object camera is over. Needs to be defined before camera for saving to work
    /// </summary>
    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    protected Spatial cameraFollow = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    protected MicrobeCamera camera = null!;

    [JsonIgnore]
    protected MeshInstance editorArrow = null!;

    protected MeshInstance editorGrid = null!;

    protected Material invalidMaterial = null!;
    protected Material validMaterial = null!;
    protected Material oldMaterial = null!;
    protected Material islandMaterial = null!;

    protected PackedScene hexScene = null!;
    protected PackedScene modelScene = null!;

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

    // TODO: rename this to placementRotation in the future (for now old name is kept to keep save compatibility)
    [JsonProperty]
    protected int organelleRot;

    /// <summary>
    ///   Where the user started panning with the mouse. Null if the user is not panning with the mouse
    /// </summary>
    protected Vector3? mousePanningStart;

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
    ///   If true a hex move is in progress and can be canceled
    /// </summary>
    [JsonIgnore]
    public bool CanCancelMove => MovingPlacedHex != null;

    [JsonIgnore]
    public abstract bool HasIslands { get; }

    protected abstract bool ForceHideHover { get; }

    // TODO: remove
    // protected override bool HasInProgressAction => CanCancelMove;

    public override void _Ready()
    {
        base._Ready();

        ResolveNodeReferences();

        symmetryIconDefault = GD.Load<Texture>("res://assets/textures/gui/bevel/1xSymmetry.png");
        symmetryIcon2X = GD.Load<Texture>("res://assets/textures/gui/bevel/2xSymmetry.png");
        symmetryIcon4X = GD.Load<Texture>("res://assets/textures/gui/bevel/4xSymmetry.png");
        symmetryIcon6X = GD.Load<Texture>("res://assets/textures/gui/bevel/6xSymmetry.png");

        LoadHexMaterials();
        LoadScenes();
    }

    public virtual void ResolveNodeReferences()
    {
        symmetryButton = GetNode<TextureButton>(SymmetryButtonPath);
        symmetryIcon = GetNode<TextureRect>(SymmetryIconPath);


        islandPopup = GetNode<CustomConfirmationDialog>(IslandErrorPath);

        // TODO: put these back
        /*camera = GetNode<MicrobeCamera>(CameraPath);
        editorArrow = GetNode<MeshInstance>(EditorArrowPath);
        editorGrid = GetNode<MeshInstance>(EditorGridPath);
        cameraFollow = GetNode<Spatial>(CameraFollowPath);*/
    }

    public override void Init(TEditor owningEditor, bool fresh)
    {
        base.Init(owningEditor, fresh);

        if (fresh)
        {
            camera.ObjectToFollow = cameraFollow;
            organelleRot = 0;

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

    public void SetSymmetry(HexEditorSymmetry newSymmetry)
    {
        Symmetry = newSymmetry;
        UpdateSymmetryIcon();
    }

    public void ResetSymmetryButton()
    {
        symmetryIcon.Texture = symmetryIconDefault;
        symmetry = 0;
    }

    public void SetEditorWorldGuideObjectVisibility(bool shown)
    {
        editorArrow.Visible = shown;
        editorGrid.Visible = shown;
    }

    /// <summary>
    ///   Updates the background shown in the editor
    /// </summary>
    public void UpdateBackgroundImage(Biome biomeToUseBackgroundFrom)
    {
        // TODO: make this be loaded in a background thread to avoid a lag spike
        camera.SetBackground(SimulationParameters.Instance.GetBackground(biomeToUseBackgroundFrom.Background));
    }

    [RunOnKeyDown("e_primary")]
    public virtual void PerformPrimaryAction()
    {
        if (MovingPlacedHex != null)
        {
            GetMouseHex(out int q, out int r);
            PerformMove(q, r);
        }
        else
        {
            if (string.IsNullOrEmpty(activeActionName))
                return;

            PerformActiveAction();
        }
    }

    [RunOnAxisGroup]
    [RunOnAxis(new[] { "e_pan_up", "e_pan_down" }, new[] { -1.0f, 1.0f })]
    [RunOnAxis(new[] { "e_pan_left", "e_pan_right" }, new[] { -1.0f, 1.0f })]
    public void PanCameraWithKeys(float delta, float upDown, float leftRight)
    {
        if (mousePanningStart != null)
            return;

        var movement = new Vector3(leftRight, 0, upDown);
        MoveObjectToFollow(movement.Normalized() * delta * camera.CameraHeight);
    }

    [RunOnKey("e_pan_mouse", CallbackRequiresElapsedTime = false)]
    public bool PanCameraWithMouse(float delta)
    {
        if (mousePanningStart == null)
        {
            mousePanningStart = camera.CursorWorldPos;
        }
        else
        {
            var mousePanDirection = mousePanningStart.Value - camera.CursorWorldPos;
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

    /// <summary>
    ///   Cancels the current editor action
    /// </summary>
    /// <returns>True when the input is consumed</returns>
    [RunOnKeyDown("e_cancel_current_action", Priority = 1)]
    public bool CancelCurrentAction()
    {
        if (MovingPlacedHex != null)
        {
            OnCurrentActionCanceled();
            MovingPlacedHex = null;

            // Re-enable undo/redo button
            Editor.NotifyUndoRedoStateChanged();

            return true;
        }

        return false;
    }

    /// <summary>
    ///   Begin hex movement under the cursor
    /// </summary>
    [RunOnKeyDown("e_move")]
    public void StartHexMoveAtCursor()
    {
        // Can't move anything while already moving one
        if (MovingPlacedHex != null)
        {
            Editor.OnActionBlockedWhileMoving();
            return;
        }

        GetMouseHex(out int q, out int r);

        var hex = GetHexAt(new Hex(q, r));

        if (hex == null)
            return;

        StartHexMove(hex);

        // Once a move has begun, the button visibility should be updated so it becomes visible
        UpdateCancelState();
    }

    public void StartHexMove(THexMove selectedHex)
    {
        if (MovingPlacedHex != null)
        {
            // Already moving something! some code went wrong
            throw new InvalidOperationException("Can't begin hex move while another in progress");
        }

        MovingPlacedHex = selectedHex;

        OnMoveActionStarted();

        // Disable undo/redo button while moving (enabled after finishing move)
        Editor.NotifyUndoRedoStateChanged();
    }

    public void RemoveHex(Hex hex)
    {
        int q = hex.Q;
        int r = hex.R;

        switch (Symmetry)
        {
            case HexEditorSymmetry.None:
            {
                TryRemoveHexAt(new Hex(q, r));
                break;
            }

            case HexEditorSymmetry.XAxisSymmetry:
            {
                TryRemoveHexAt(new Hex(q, r));

                if (q != -1 * q || r != r + q)
                {
                    TryRemoveHexAt(new Hex(-1 * q, r + q));
                }

                break;
            }

            case HexEditorSymmetry.FourWaySymmetry:
            {
                TryRemoveHexAt(new Hex(q, r));

                if (q != -1 * q || r != r + q)
                {
                    TryRemoveHexAt(new Hex(-1 * q, r + q));
                    TryRemoveHexAt(new Hex(-1 * q, -1 * r));
                    TryRemoveHexAt(new Hex(q, -1 * (r + q)));
                }
                else
                {
                    TryRemoveHexAt(new Hex(-1 * q, -1 * r));
                }

                break;
            }

            case HexEditorSymmetry.SixWaySymmetry:
            {
                TryRemoveHexAt(new Hex(q, r));

                TryRemoveHexAt(new Hex(-1 * r, r + q));
                TryRemoveHexAt(new Hex(-1 * (r + q), q));
                TryRemoveHexAt(new Hex(-1 * q, -1 * r));
                TryRemoveHexAt(new Hex(r, -1 * (r + q)));
                TryRemoveHexAt(new Hex(r, -1 * (r + q)));
                TryRemoveHexAt(new Hex(r + q, -1 * q));

                break;
            }
        }
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

    protected override void RegisterTooltips()
    {
        base.RegisterTooltips();
        symmetryButton.RegisterToolTipForControl("symmetryButton", "editor");
    }

    protected override void EnqueueAction(TAction action)
    {
        if (!Editor.CheckEnoughMPForAction(action.Cost))
            return;

        if (CanCancelMove)
        {
            if (!DoesActionEndInProgressAction(action))
            {
                // Play sound
                Editor.OnActionBlockedWhileMoving();
                return;
            }
        }

        Editor.EnqueueAction(action);
    }

    protected abstract void PerformActiveAction();
    protected abstract bool DoesActionEndInProgressAction(TAction action);

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

    protected void OnSymmetryClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();

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

    protected void OnSymmetryHold()
    {
        symmetryIcon.Modulate = new Color(0, 0, 0);
    }

    protected void OnSymmetryReleased()
    {
        symmetryIcon.Modulate = new Color(1, 1, 1);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        // We move all the hexes and the hover hexes to 0,0,0 so that
        // the editor is free to replace them wherever
        // TODO: it would be way better if we didn't have to do this and instead only updated
        // the hover hexes and models when there is some change to them
        foreach (var hex in hoverHexes)
        {
            hex.Translation = new Vector3(0, 0, 0);
            hex.Visible = false;
        }

        foreach (var model in hoverModels)
        {
            model.Translation = new Vector3(0, 0, 0);
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

        editorGrid.Translation = camera.CursorWorldPos;
        editorGrid.Visible = Editor.ShowHover && !ForceHideHover;
    }

    protected MeshInstance CreateEditorHex()
    {
        var hex = (MeshInstance)hexScene.Instance();
        Editor.RootOfDynamicallySpawned.AddChild(hex);
        return hex;
    }

    protected SceneDisplayer CreatePreviewModelHolder()
    {
        var node = (SceneDisplayer)modelScene.Instance();
        Editor.RootOfDynamicallySpawned.AddChild(node);
        return node;
    }

    /// <summary>
    ///   Returns the hex position the mouse is over
    /// </summary>
    protected void GetMouseHex(out int q, out int r)
    {
        // Get the position of the cursor in the plane that the microbes is floating in
        var cursorPos = camera.CursorWorldPos;

        // Convert to the hex the cursor is currently located over.
        var hex = Hex.CartesianToAxial(cursorPos);

        q = hex.Q;
        r = hex.R;
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
    protected abstract bool IsMoveTargetValid(Hex position, int rotation, THexMove hex);

    protected abstract void OnCurrentActionCanceled();
    protected abstract void OnMoveActionStarted();
    protected abstract void PerformMove(int q, int r);
    protected abstract THexMove? GetHexAt(Hex position);
    protected abstract void TryRemoveHexAt(Hex location);
    protected abstract void UpdateCancelState();

    /// <summary>
    ///   Moves the ObjectToFollow of the camera in a direction
    /// </summary>
    /// <param name="vector">The direction to move the camera</param>
    private void MoveObjectToFollow(Vector3 vector)
    {
        cameraFollow.Translation += vector;
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

    // TODO: make this method trigger automatically on Symmetry assignment
    private void UpdateSymmetryIcon()
    {
        switch (symmetry)
        {
            case HexEditorSymmetry.None:
                symmetryIcon.Texture = symmetryIconDefault;
                break;
            case HexEditorSymmetry.XAxisSymmetry:
                symmetryIcon.Texture = symmetryIcon2X;
                break;
            case HexEditorSymmetry.FourWaySymmetry:
                symmetryIcon.Texture = symmetryIcon4X;
                break;
            case HexEditorSymmetry.SixWaySymmetry:
                symmetryIcon.Texture = symmetryIcon6X;
                break;
        }
    }
}
