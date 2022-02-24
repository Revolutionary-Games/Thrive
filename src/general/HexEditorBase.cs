using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   More specialized editor base type that supports hex based editors. Due to C# not supporting multiple inheritance
///   this needs to inherit the patch handling.
/// </summary>
public abstract class HexEditorBase<TAction, TStage, THexMove> : EditorWithPatchesBase<TAction, TStage>
    where TAction : MicrobeEditorAction
    where TStage : Node, IReturnableGameState
    where THexMove : class
{
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

    protected EditorSymmetry symmetry = EditorSymmetry.None;

    /// <summary>
    ///   Where the user started panning with the mouse. Null if the user is not panning with the mouse
    /// </summary>
    protected Vector3? mousePanningStart;

    /// <summary>
    ///   The Symmetry setting of the hex based Editor.
    /// </summary>
    public enum EditorSymmetry
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
    ///   The symmetry setting of the editor.
    /// </summary>
    public EditorSymmetry Symmetry
    {
        get => symmetry;
        set => symmetry = value;
    }

    /// <summary>
    ///   Hover hexes and models are only shown if this is true. This is saved to make this work better when the player
    ///   was in the cell editor tab and saved.
    /// </summary>
    public bool ShowHover { get; set; }

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

    protected override bool HasInProgressAction => CanCancelMove;

    public override void _Ready()
    {
        base._Ready();

        LoadHexMaterials();
        LoadScenes();

        if (!IsLoadedFromSave)
            camera.ObjectToFollow = cameraFollow;
    }

    public override void SetEditorObjectVisibility(bool shown)
    {
        base.SetEditorObjectVisibility(shown);

        editorArrow.Visible = shown;
        editorGrid.Visible = shown;
    }

    public virtual void SetPlayerPatch(Patch? patch)
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

        UpdatePatchBackgroundImage();
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
            UpdateUndoRedoButtons();

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
            OnActionBlockedWhileMoving();
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
        UpdateUndoRedoButtons();
    }

    public void RemoveHex(Hex hex)
    {
        int q = hex.Q;
        int r = hex.R;

        switch (Symmetry)
        {
            case EditorSymmetry.None:
            {
                TryRemoveHexAt(new Hex(q, r));
                break;
            }

            case EditorSymmetry.XAxisSymmetry:
            {
                TryRemoveHexAt(new Hex(q, r));

                if (q != -1 * q || r != r + q)
                {
                    TryRemoveHexAt(new Hex(-1 * q, r + q));
                }

                break;
            }

            case EditorSymmetry.FourWaySymmetry:
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

            case EditorSymmetry.SixWaySymmetry:
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

    public override void PerformPrimaryAction()
    {
        if (MovingPlacedHex != null)
        {
            GetMouseHex(out int q, out int r);
            PerformMove(q, r);
        }
        else
        {
            base.PerformPrimaryAction();
        }
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

    protected override void OnEnterEditor()
    {
        base.OnEnterEditor();

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
    }

    protected override void InitEditor()
    {
        // The world is reset each time so these are gone. We throw an exception if that's not the case as that
        // indicates a programming bug
        if (placedHexes.Count > 0 || placedModels.Count > 0)
            throw new InvalidOperationException("This editor has already been initialized (placed hexes not empty)");

        base.InitEditor();

        if (!IsLoadedFromSave)
        {
            Symmetry = 0;
        }

        UpdatePatchBackgroundImage();
    }

    protected override void InitEditorFresh()
    {
        base.InitEditorFresh();

        organelleRot = 0;
    }

    protected override void ResolveDerivedTypeNodeReferences()
    {
        camera = world.GetNode<MicrobeCamera>("PrimaryCamera");
        editorArrow = world.GetNode<MeshInstance>("EditorArrow");
        editorGrid = world.GetNode<MeshInstance>("Grid");
        cameraFollow = world.GetNode<Spatial>("CameraLookAt");
    }

    protected override void UpdateEditor(float delta)
    {
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
        editorGrid.Visible = ShowHover && !ForceHideHover;
    }

    protected MeshInstance CreateEditorHex()
    {
        var hex = (MeshInstance)hexScene.Instance();
        rootOfDynamicallySpawned.AddChild(hex);
        return hex;
    }

    protected SceneDisplayer CreatePreviewModelHolder()
    {
        var node = (SceneDisplayer)modelScene.Instance();
        rootOfDynamicallySpawned.AddChild(node);
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
    ///   Updates the background shown in the editor
    /// </summary>
    private void UpdatePatchBackgroundImage()
    {
        camera.SetBackground(SimulationParameters.Instance.GetBackground(CurrentPatch.BiomeTemplate.Background));
    }

    /// <summary>
    ///   Moves the ObjectToFollow of the camera in a direction
    /// </summary>
    /// <param name="vector">The direction to move the camera</param>
    private void MoveObjectToFollow(Vector3 vector)
    {
        cameraFollow.Translation += vector;
    }
}
