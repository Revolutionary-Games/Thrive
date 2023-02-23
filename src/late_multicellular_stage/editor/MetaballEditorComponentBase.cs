using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

public abstract class MetaballEditorComponentBase<TEditor, TCombinedAction, TAction, TMetaball> :
    EditorComponentWithActionsBase<TEditor, TCombinedAction>,
    ISaveLoadedTracked, IChildPropertiesLoadCallback
    where TEditor : class, IHexEditor, IEditorWithActions
    where TCombinedAction : CombinedEditorAction
    where TAction : EditorAction
    where TMetaball : Metaball
{
    [Export]
    public NodePath? CameraPath;

    [Export]
    public NodePath EditorArrowPath = null!;

    [Export]
    public NodePath EditorGroundPath = null!;

    [Export]
    public NodePath IslandErrorPath = null!;

    /// <summary>
    ///   Set above 0 to make sure the arrow doesn't overlap with the ground circle graphics
    /// </summary>
    [Export]
    public float ForwardArrowOffsetFromGround = 0.1f;

#pragma warning disable CA2213
    protected EditorCamera3D? camera;

    [JsonIgnore]
    protected MeshInstance editorArrow = null!;

    protected MeshInstance editorGround = null!;

    protected AudioStream hexPlacementSound = null!;
#pragma warning restore CA2213

    [JsonProperty]
    protected string? activeActionName;

    /// <summary>
    ///   This is a global assessment if the currently being placed thing / action is valid
    /// </summary>
    protected bool isPlacementProbablyValid;

    [JsonProperty]
    protected MetaballLayout<TMetaball> editedMetaballs = null!;

    /// <summary>
    ///   This is used to keep track of used hover metaballs
    /// </summary>
    protected int usedHoverMetaballIndex;

    protected bool hoverMetaballsChanged = true;

    protected List<TMetaball> hoverMetaballData = new();

    protected IMetaballDisplayer<TMetaball>? alreadyPlacedVisuals;
    protected IMetaballDisplayer<TMetaball>? hoverMetaballDisplayer;

    private const float DefaultHoverAlpha = 0.8f;
    private const float CannotPlaceHoverAlpha = 0.2f;

    private readonly List<Plane> cursorHitWorldPlanes = new()
    {
        new Plane(new Vector3(0, 0, -1), 0.0f),
        new Plane(new Vector3(1, 0, 0), 0.0f),
        new Plane(new Vector3(0, 0, 1), 0.0f),
        new Plane(new Vector3(-1, 0, 0), 0.0f),
        new Plane(new Vector3(0, 1, 0), 0.0f),
    };

    // Another section of Godot objects here as these are private (and not protected like the above set)
#pragma warning disable CA2213
    private CustomConfirmationDialog islandPopup = null!;
#pragma warning restore CA2213

    private HexEditorSymmetry symmetry = HexEditorSymmetry.None;

    private IEnumerable<(Vector3 Position, TMetaball? Parent)>? mouseHoverPositions;

    /// <summary>
    ///   The symmetry setting of the editor.
    /// </summary>
    [JsonProperty]
    public HexEditorSymmetry Symmetry
    {
        get => symmetry;
        set
        {
            symmetry = value;

            if (symmetry != HexEditorSymmetry.None)
                throw new NotSupportedException("Symmetry editing not implemented yet");
        }
    }

    /// <summary>
    ///   Metaball that is in the process of being moved but a new location hasn't been selected yet.
    ///   If null, nothing is in the process of moving.
    /// </summary>
    [JsonProperty]
    public TMetaball? MovingPlacedMetaball { get; protected set; }

    // TODO: implement 3D editor camera position and rotation saving

    [JsonIgnore]
    public IEnumerable<(Vector3 Position, TMetaball? Parent)>? MouseHoverPositions
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
    public bool CanCancelMove => MovingPlacedMetaball != null;

    [JsonIgnore]
    public override bool CanCancelAction => CanCancelMove;

    [JsonIgnore]
    public abstract bool HasIslands { get; }

    public bool IsLoadedFromSave { get; set; }

    protected abstract bool ForceHideHover { get; }

    public override void _Ready()
    {
        base._Ready();

        ResolveNodeReferences();

        LoadScenes();
        LoadAudioStreams();
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

        camera = GetNode<EditorCamera3D>(CameraPath);
        editorArrow = GetNode<MeshInstance>(EditorArrowPath);
        editorGround = GetNode<MeshInstance>(EditorGroundPath);

        camera.Connect(nameof(EditorCamera3D.OnPositionChanged), this, nameof(OnCameraPositionChanged));
    }

    public override void Init(TEditor owningEditor, bool fresh)
    {
        base.Init(owningEditor, fresh);

        if (camera == null)
        {
            throw new InvalidOperationException(
                "This editor component was loaded from a save and is not fully functional");
        }

        var newLayout = CreateLayout();

        if (fresh)
        {
            ResetSymmetryButton();
            editedMetaballs = newLayout;
        }
        else
        {
            // We assume that the loaded save layout did not have anything weird set for the callbacks as we
            // do this rather than use SaveApplyHelpers
            foreach (var editedMicrobeOrganelle in editedMetaballs)
            {
                newLayout.Add(editedMicrobeOrganelle);
            }

            editedMetaballs = newLayout;

            // TODO: check are these checks needed
            /*if (Editor.EditedCellProperties != null)
            {*/
            UpdateArrow(false);
            /*}
            else
            {
                GD.Print("Loaded metaball editor with no cell to edit set");
            }*/
        }

        UpdateSymmetryIcon();

        LoadMetaballDisplayers();

        hoverMetaballsChanged = true;
        hoverMetaballData.Clear();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (hoverMetaballDisplayer == null)
            throw new InvalidOperationException($"{GetType().Name} not initialized");

        // TODO: should we display the hover metaballs setup on the previous frame here?
        hoverMetaballsChanged = true;
        if (hoverMetaballsChanged)
        {
            hoverMetaballDisplayer.OverrideColourAlpha =
                isPlacementProbablyValid ? DefaultHoverAlpha : CannotPlaceHoverAlpha;

            // Remove excess hover metaball data
            while (hoverMetaballData.Count > usedHoverMetaballIndex)
                hoverMetaballData.RemoveAt(hoverMetaballData.Count - 1);

            hoverMetaballDisplayer.DisplayFromList(hoverMetaballData);

            hoverMetaballsChanged = false;
        }

        // Clear the hover metaballs for the concrete editor type to use
        hoverMetaballsChanged = false;
        usedHoverMetaballIndex = 0;
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

        if (alreadyPlacedVisuals != null)
        {
            alreadyPlacedVisuals.Visible = shown;
            hoverMetaballDisplayer!.Visible = shown;
        }
    }

    public void SetEditorWorldGuideObjectVisibility(bool shown)
    {
        editorArrow.Visible = shown;
        editorGround.Visible = shown;
    }

    /// <summary>
    ///   Updates the background shown in the editor
    /// </summary>
    public void UpdateBackgroundImage(Biome biomeToUseBackgroundFrom)
    {
        GD.Print("TODO: 3D editor background for patches");
    }

    [RunOnKeyDown("e_primary")]
    public virtual bool PerformPrimaryAction()
    {
        if (!Visible)
            return false;

        if (MovingPlacedMetaball != null)
        {
            GetMouseMetaball(out var position, out var parent);

            if (parent != null)
                PerformMove(position, parent);
        }
        else
        {
            if (string.IsNullOrEmpty(activeActionName))
                return true;

            PerformActiveAction();
        }

        return true;
    }

    /// <summary>
    ///   Cancels the current editor action
    /// </summary>
    /// <returns>True when the input is consumed</returns>
    [RunOnKeyDown("e_cancel_current_action", Priority = 1)]
    public bool CancelCurrentAction()
    {
        if (!Visible)
            return false;

        if (MovingPlacedMetaball != null)
        {
            OnCurrentActionCanceled();

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
    public bool StartHexMoveAtCursor()
    {
        if (!Visible)
            return false;

        // Can't move anything while already moving one
        if (MovingPlacedMetaball != null)
        {
            Editor.OnActionBlockedWhileMoving();
            return true;
        }

        throw new NotImplementedException();

        /*
        GetMouseMetaball(out int q, out int r);

        var hex = GetHexAt(new Hex(q, r));

        if (hex == null)
            return true;

        StartHexMove(hex);

        // Once a move has begun, the button visibility should be updated so it becomes visible
        UpdateCancelState();
        return true;
        */
    }

    public void StartHexMove(TMetaball selectedHex)
    {
        if (MovingPlacedMetaball != null)
        {
            // Already moving something! some code went wrong
            throw new InvalidOperationException("Can't begin hex move while another in progress");
        }

        MovingPlacedMetaball = selectedHex;

        OnMoveActionStarted();

        // Disable undo/redo/symmetry button while moving (enabled after finishing move)
        Editor.NotifyUndoRedoStateChanged();

        // TODO: change this to go through the editor as well for consistency
        UpdateSymmetryButton();
    }

    public void StartHexMoveWithSymmetry(IEnumerable<TMetaball> selectedHexes)
    {
        // TODO: implement symmetry move for metaballs (note also not implemented for hex editor yet)
        throw new NotImplementedException();
    }

    public void RemoveAtPosition(Vector3 basePosition, TMetaball? baseMetaball)
    {
        var actions = new List<TAction>();
        int alreadyDeleted = 0;

        RunWithSymmetry(basePosition, baseMetaball, (_, metaball) =>
        {
            if (metaball == null)
                return;

            var removed = TryCreateMetaballRemoveAction(metaball, ref alreadyDeleted);

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
        throw new NotImplementedException();

        /*
        GetMouseMetaball(out int q, out int r);

        Hex mouseHex = new Hex(q, r);

        var hex = GetHexAt(mouseHex);

        if (hex == null)
            return;

        RemoveAtPosition(mouseHex);
        */
    }

    public override bool CanFinishEditing(IEnumerable<EditorUserOverride> userOverrides)
    {
        if (!base.CanFinishEditing(userOverrides))
            return false;

        // Can't exit the editor with metaballs too far away from their parents
        // TODO: implement drawing the links between metaballs that are too far away in red
        if (HasIslands)
        {
            islandPopup.PopupCenteredShrink();
            return false;
        }

        return true;
    }

    public override void OnValidAction()
    {
        GUICommon.Instance.PlayCustomSound(hexPlacementSound, 0.7f);
    }

    public void OnNoPropertiesLoaded()
    {
        // Something is wrong if this is called
        throw new InvalidOperationException();
    }

    public virtual void OnPropertiesLoaded()
    {
    }

    /// <summary>
    ///   Updates the forward pointing arrow to not overlap the edited species. Should be called on any layout change
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is public so that editors with multiple tabs can make the arrow fix itself after switching tabs
    ///   </para>
    /// </remarks>
    public void UpdateArrow(bool animateMovement = true)
    {
        var arrowPosition = new Vector3(0, ForwardArrowOffsetFromGround,
            CalculateEditorArrowZPosition() - Constants.EDITOR_ARROW_OFFSET);

        if (animateMovement)
        {
            // TODO: check that this works
            GUICommon.Instance.Tween.InterpolateProperty(editorArrow, "translation", editorArrow.Translation,
                arrowPosition, Constants.EDITOR_ARROW_INTERPOLATE_SPEED,
                Tween.TransitionType.Expo, Tween.EaseType.Out);
            GUICommon.Instance.Tween.Start();
        }
        else
        {
            editorArrow.Translation = arrowPosition;
        }
    }

    protected abstract MetaballLayout<TMetaball> CreateLayout();
    protected abstract IMetaballDisplayer<TMetaball> CreateMetaballDisplayer();

    protected virtual void LoadMetaballDisplayers()
    {
        alreadyPlacedVisuals = CreateMetaballDisplayer();
        hoverMetaballDisplayer = CreateMetaballDisplayer();
        hoverMetaballDisplayer.OverrideColourAlpha = DefaultHoverAlpha;
    }

    protected virtual void LoadScenes()
    {
    }

    protected virtual void LoadAudioStreams()
    {
        hexPlacementSound = GD.Load<AudioStream>("res://assets/sounds/soundeffects/gui/click_place_success.ogg");
    }

    protected override bool EnqueueAction(TCombinedAction action)
    {
        if (!Editor.CheckEnoughMPForAction(Editor.WhatWouldActionsCost(action.Data)))
            return false;

        if (CanCancelMove)
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
        Editor.OnValidAction();
        UpdateSymmetryButton();
        return true;
    }

    protected virtual TCombinedAction CreateCombinedAction(IEnumerable<EditorAction> actions)
    {
        return (TCombinedAction)new CombinedEditorAction(actions);
    }

    protected void OnSymmetryPressed()
    {
        throw new NotImplementedException("symmetry not implemented");

        /*
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
        */
    }

    /// <inheritdoc cref="HexEditorComponentBase{TEditor,TCombinedAction,TAction,THexMove}.OnHexEditorMouseEntered"/>
    protected void OnMetaballEditorMouseEntered()
    {
        if (!Visible)
            return;

        Editor.ShowHover = true;
        UpdateMutationPointsBar();
    }

    /// <inheritdoc cref="HexEditorComponentBase{TEditor,TCombinedAction,TAction,THexMove}.OnHexEditorMouseExited"/>
    protected void OnMetaballEditorMouseExited()
    {
        Editor.ShowHover = false;
        UpdateMutationPointsBar();
    }

    /// <inheritdoc cref="HexEditorComponentBase{TEditor,TCombinedAction,TAction,THexMove}.OnHexEditorGuiInput"/>
    protected void OnMetaballEditorGuiInput(InputEvent inputEvent)
    {
        if (!Editor.ShowHover)
            return;

        InputManager.ForwardInput(inputEvent);
    }

    /// <summary>
    ///   Returns the world position the mouse is pointing at (and if any) the hit metaball
    /// </summary>
    protected void GetMouseMetaball(out Vector3 position, out TMetaball? metaball, float maxIntersectDistance = 1000)
    {
        var viewPort = GetViewport();

        if (viewPort == null)
            throw new InvalidOperationException("No viewport");

        if (camera == null)
            throw new InvalidOperationException("No camera");

        var mousePos = viewPort.GetMousePosition();

        var rayOrigin = camera.ProjectRayOrigin(mousePos);
        var rayNormal = camera.ProjectRayNormal(mousePos);
        var rayEnd = rayOrigin + rayNormal * maxIntersectDistance;

        float closestMetaball = float.MaxValue;
        metaball = null;

        // This is to make the compiler realize we've assigned something to position if metaball is set after the next
        // loop
        position = Vector3.Zero;

        foreach (var testedMetaball in editedMetaballs)
        {
            // TODO: check if the math is faster if we roll our custom sphere intersection rather than call into native
            // Godot code here
            var potentialIntersection = Geometry.SegmentIntersectsSphere(rayOrigin, rayEnd, testedMetaball.Position,
                testedMetaball.Size * 0.5f);

            if (potentialIntersection.Length < 1)
                continue;

            // Because multiple metaballs can be hit with a single ray, we actually want to pick the one with the hit
            // location being closest to the camera
            var distance = rayOrigin.DistanceSquaredTo(potentialIntersection[0]);

            if (distance < closestMetaball)
            {
                closestMetaball = distance;
                metaball = testedMetaball;
                position = potentialIntersection[0];
            }
        }

        if (metaball != null)
            return;

        // If the ray didn't hit any metaball, hit some helper planes

        foreach (var plane in cursorHitWorldPlanes)
        {
            var intersection = plane.IntersectRay(rayOrigin, rayNormal);

            if (intersection != null)
            {
                position = intersection.Value;
                return;
            }
        }

        GD.PrintErr("No mouse ray intersection with anything");
        position = new Vector3(0, 0, 0);
    }

    /// <summary>
    ///   Runs given callback for all symmetry positions
    /// </summary>
    /// <param name="position">The base position</param>
    /// <param name="parent">The base parent</param>
    /// <param name="callback">The callback that is called based on symmetry, parameters are: q, r, rotation</param>
    /// <param name="overrideSymmetry">If set, overrides the current symmetry</param>
    /// <remarks>
    ///   <para>
    ///     TODO: this is not implemented currently and just returns the given primary position
    ///   </para>
    /// </remarks>
    protected void RunWithSymmetry(Vector3 position, TMetaball? parent, Action<Vector3, TMetaball?> callback,
        HexEditorSymmetry? overrideSymmetry = null)
    {
        overrideSymmetry ??= Symmetry;

        switch (overrideSymmetry)
        {
            case HexEditorSymmetry.None:
            {
                callback(position, parent);
                break;
            }

            default:
                throw new NotSupportedException("symmetry editing not implemented yet");
        }
    }

    protected virtual void OnCurrentActionCanceled()
    {
        UpdateCancelButtonVisibility();

        // TODO: switch to this going through the editor
        UpdateSymmetryButton();
    }

    protected virtual void OnMoveWillSucceed()
    {
        MovingPlacedMetaball = null;

        // Move succeeded; Update the cancel button visibility so it's hidden because the move has completed
        // TODO: should this call be made through Editor here?
        UpdateCancelButtonVisibility();

        // Re-enable undo/redo button
        Editor.NotifyUndoRedoStateChanged();
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
        throw new NotImplementedException();
    }

    protected void UpdateAlreadyPlacedHexes(
        IEnumerable<(Hex BasePosition, IEnumerable<Hex> Hexes, bool PlacedThisSession)> hexes, List<Hex> islands,
        bool forceHide = false)
    {
        throw new NotImplementedException();
    }

    protected abstract void PerformActiveAction();

    protected virtual bool DoesActionEndInProgressAction(TCombinedAction action)
    {
        // Allow only move actions with an in-progress move
        return action.Data.Any(d => d is MetaballMoveActionData<TMetaball>);
    }

    /// <summary>
    ///   Checks if the target position is valid to place hex.
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="rotation">
    ///   The rotation to check for the hex (only makes sense when placing a group of hexes)
    /// </param>
    /// <param name="metaball">The move data to try to move to the position</param>
    /// <returns>True if valid</returns>
    protected abstract bool IsMoveTargetValid(Vector3 position, MulticellularMetaball? rotation, TMetaball metaball);

    protected abstract void OnMoveActionStarted();
    protected abstract void PerformMove(Vector3 position, TMetaball parent);
    protected abstract TAction? TryCreateMetaballRemoveAction(TMetaball metaball, ref int alreadyDeleted);

    protected abstract float CalculateEditorArrowZPosition();

    protected virtual void UpdateCancelState()
    {
        UpdateCancelButtonVisibility();
    }

    protected void UpdateSymmetryButton()
    {
        componentBottomLeftButtons.SymmetryEnabled = MovingPlacedMetaball == null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (CameraPath != null)
            {
                CameraPath.Dispose();
                EditorArrowPath.Dispose();
                EditorGroundPath.Dispose();
                IslandErrorPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OnCameraPositionChanged(Transform newPosition)
    {
        // TODO: implement camera position saving
    }

    // TODO: make this method trigger automatically on Symmetry assignment
    private void UpdateSymmetryIcon()
    {
        componentBottomLeftButtons.SetSymmetry(symmetry);
    }
}
