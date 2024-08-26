﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Editor component that specializes in metaball-based stuff editing
/// </summary>
/// <typeparam name="TEditor">Type of editor this class can be put in</typeparam>
/// <typeparam name="TCombinedAction">Type of editor action this class works with</typeparam>
/// <typeparam name="TAction">Type of single action this works with</typeparam>
/// <typeparam name="TMetaball">Type of metaballs this editor works with</typeparam>
[GodotAbstract]
public partial class MetaballEditorComponentBase<TEditor, TCombinedAction, TAction, TMetaball> :
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
    protected MeshInstance3D editorArrow = null!;

    protected MeshInstance3D editorGround = null!;

    protected AudioStream hexPlacementSound = null!;
#pragma warning restore CA2213

    [JsonProperty]
    protected float metaballSize = 1.0f;

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

    /// <summary>
    ///   Displays structure of metaballs in the editor
    /// </summary>
    protected IMetaballDisplayer<TMetaball>? structuralMetaballDisplayer;

    /// <summary>
    ///   Displays shape of the metaballs the way it would actually look in game. Used in preview mode.
    ///   Should display metaballs using the convolution displayer.
    /// </summary>
    protected IMetaballDisplayer<TMetaball>? visualMetaballDisplayer;

    protected IMetaballDisplayer<TMetaball>? hoverMetaballDisplayer;

    private const float DefaultHoverAlpha = 0.8f;
    private const float CannotPlaceHoverAlpha = 0.2f;

    private readonly NodePath positionReference = new("position");

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
    [Export]
    private HSlider? metaballResizeScroll;

    private CustomConfirmationDialog islandPopup = null!;
#pragma warning restore CA2213

    private HexEditorSymmetry symmetry = HexEditorSymmetry.None;

    private bool previewMode;

    private IEnumerable<(Vector3 Position, TMetaball? Parent)>? mouseHoverPositions;

    protected MetaballEditorComponentBase()
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
    ///   If this is enabled, the creature will be shown as it would actually look in game. Editing metaballs is
    ///   disabled during preview (except for undo/redo).
    /// </summary>
    [JsonProperty]
    public bool PreviewMode
    {
        get => previewMode;
        set
        {
            bool updateNeeded = previewMode != value;

            previewMode = value;

            if (updateNeeded)
                UpdateAlreadyPlacedVisuals();

            if (hoverMetaballDisplayer != null)
                hoverMetaballDisplayer.Visible = !PreviewMode;
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
    ///   If true a metaball move is in progress and can be canceled
    /// </summary>
    [JsonIgnore]
    public bool CanCancelMove => MovingPlacedMetaball != null;

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
        editorArrow = GetNode<MeshInstance3D>(EditorArrowPath);
        editorGround = GetNode<MeshInstance3D>(EditorGroundPath);

        camera.Connect(EditorCamera3D.SignalName.OnPositionChanged,
            new Callable(this, nameof(OnCameraPositionChanged)));
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

        if (metaballResizeScroll != null)
        {
            metaballResizeScroll.Value = metaballSize;
            metaballResizeScroll.MinValue = Constants.METABALL_MIN_SIZE;
            metaballResizeScroll.MaxValue = Constants.METABALL_MAX_SIZE;

            // Allow GUI to have smaller step but not bigger
            if (metaballResizeScroll.Step > Constants.METABALL_SIZE_STEP)
                metaballResizeScroll.Step = Constants.METABALL_SIZE_STEP;
        }
    }

    public override void _Process(double delta)
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

            hoverMetaballDisplayer.DisplayFromLayout(hoverMetaballData);

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

        if (structuralMetaballDisplayer != null)
        {
            structuralMetaballDisplayer.Visible = shown && !PreviewMode;
            hoverMetaballDisplayer!.Visible = shown && !PreviewMode;
        }

        if (visualMetaballDisplayer != null)
        {
            visualMetaballDisplayer.Visible = shown && PreviewMode;
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
    ///   Begin metaball movement under the cursor
    /// </summary>
    [RunOnKeyDown("e_move")]
    public bool StartMetaballMoveAtCursor()
    {
        if (!Visible)
            return false;

        // Can't move anything while already moving one
        if (MovingPlacedMetaball != null)
        {
            Editor.OnActionBlockedWhileMoving();
            return true;
        }

        GetMouseMetaball(out _, out var metaball);

        if (metaball != null)
            StartMetaballMove(metaball);

        return true;
    }

    public void StartMetaballMove(TMetaball selectedMetaball)
    {
        if (MovingPlacedMetaball != null)
        {
            // Already moving something! some code went wrong
            throw new InvalidOperationException("Can't begin metaball move while another in progress");
        }

        MovingPlacedMetaball = selectedMetaball;

        OnMoveActionStarted();

        // Once a move has begun, the button visibility should be updated to make it become visible
        OnActionStatusChanged();
    }

    public void StartMetaballMoveWithSymmetry(IEnumerable<TMetaball> selectedMetaballs)
    {
        // TODO: implement symmetry move for metaballs (note also not implemented for hex editor yet)
        throw new NotImplementedException();
    }

    public void RemoveAtPosition(Vector3 basePosition, TMetaball? baseMetaball)
    {
        var actions = new List<TAction>();
        int alreadyDeleted = 0;

        RunWithSymmetry(0, basePosition, baseMetaball, (_, metaball) =>
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
    ///   Remove the metaball under the cursor (if there is one)
    /// </summary>
    [RunOnKeyDown("e_delete")]
    public void RemoveMetaballAtCursor()
    {
        GetMouseMetaball(out var position, out var metaball);

        if (metaball != null)
            RemoveAtPosition(position, metaball);
    }

    [RunOnKeyDown("e_increase_size", Priority = 1)]
    public void IncreaseMetaballSize()
    {
        metaballSize += Constants.METABALL_SIZE_STEP;

        if (metaballSize > Constants.METABALL_MAX_SIZE)
            metaballSize = Constants.METABALL_MAX_SIZE;

        if (metaballResizeScroll != null)
            metaballResizeScroll.Value = metaballSize;
    }

    [RunOnKeyDown("e_decrease_size", Priority = 1)]
    public void DecreaseMetaballSize()
    {
        metaballSize -= Constants.METABALL_SIZE_STEP;

        if (metaballSize < Constants.METABALL_MIN_SIZE)
            metaballSize = Constants.METABALL_MIN_SIZE;

        if (metaballResizeScroll != null)
            metaballResizeScroll.Value = metaballSize;
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

    public override void OnValidAction(IEnumerable<CombinableActionData> actions)
    {
        var anyPlacement = typeof(MetaballPlacementActionData<>);
        var anyMove = typeof(MetaballMoveActionData<>);

        foreach (var data in actions)
        {
            var type = data.GetType();
            if (type.IsAssignableToGenericType(anyPlacement) || type.IsAssignableToGenericType(anyMove))
            {
                PlayMetaballPlacementSound();
                break;
            }
        }
    }

    public void PlayMetaballPlacementSound()
    {
        GUICommon.Instance.PlayCustomSound(hexPlacementSound, 0.7f);
    }

    public bool IsMetaballAction(EditorCombinableActionData actionData)
    {
        // TODO: add a metaball action base class if that would be easier to handle?
        switch (actionData)
        {
            case MetaballMoveActionData<TMetaball>:
            case MetaballPlacementActionData<TMetaball>:
            case MetaballRemoveActionData<TMetaball>:
            case MetaballResizeActionData<TMetaball>:
                return true;

            default:
                return false;
        }
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

            var tween = CreateTween();
            tween.SetTrans(Tween.TransitionType.Expo);
            tween.SetEase(Tween.EaseType.Out);

            tween.TweenProperty(editorArrow, positionReference, arrowPosition,
                Constants.EDITOR_ARROW_INTERPOLATE_SPEED);
        }
        else
        {
            editorArrow.Position = arrowPosition;
        }
    }

    protected virtual MetaballLayout<TMetaball> CreateLayout()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual IMetaballDisplayer<TMetaball> CreateVisualMetaballDisplayer()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual IMetaballDisplayer<TMetaball> CreateStructuralMetaballDisplayer()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual void LoadMetaballDisplayers()
    {
        visualMetaballDisplayer = CreateVisualMetaballDisplayer();
        structuralMetaballDisplayer = CreateStructuralMetaballDisplayer();
        hoverMetaballDisplayer = CreateStructuralMetaballDisplayer();
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
        Editor.OnValidAction(action.Data);
        UpdateSymmetryButton();
        return true;
    }

    protected virtual TCombinedAction CreateCombinedAction(IEnumerable<EditorAction> actions)
    {
        return (TCombinedAction)new CombinedEditorAction(actions);
    }

    protected void OnSymmetryPressed()
    {
        if (symmetry == HexEditorSymmetry.XAxisSymmetry)
        {
            ResetSymmetryButton();
        }
        else if (symmetry == HexEditorSymmetry.None)
        {
            symmetry = HexEditorSymmetry.XAxisSymmetry;
        }

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
        }*/

        Symmetry = symmetry;
        UpdateSymmetryIcon();
    }

    // LineLengthCheckDisable is needed here as our XML indent check doesn't support splitting tags on multiple lines
    /// <inheritdoc cref="HexEditorComponentBase{TEditor,TCombinedAction,TAction,THexMove,TContext}.OnHexEditorMouseEntered"/>
    protected void OnMetaballEditorMouseEntered()
    {
        if (!Visible)
            return;

        Editor.ShowHover = true;
        UpdateMutationPointsBar();
    }

    /// <inheritdoc cref="HexEditorComponentBase{TEditor,TCombinedAction,TAction,THexMove,TContext}.OnHexEditorMouseExited"/>
    protected void OnMetaballEditorMouseExited()
    {
        Editor.ShowHover = false;
        UpdateMutationPointsBar();
    }

    /// <inheritdoc cref="HexEditorComponentBase{TEditor,TCombinedAction,TAction,THexMove,TContext}.OnHexEditorGuiInput"/>
    protected void OnMetaballEditorGuiInput(InputEvent inputEvent)
    {
        if (!Editor.ShowHover)
            return;

        InputManager.ForwardInput(inputEvent);
    }

    // LineLengthCheckEnable

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
            var potentialIntersection = Geometry3D.SegmentIntersectsSphere(rayOrigin, rayEnd, testedMetaball.Position,
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
            var intersection = plane.IntersectsRay(rayOrigin, rayNormal);

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
    /// <param name="diameter">Diameter of placed metaball</param>
    /// <param name="position">The base position</param>
    /// <param name="parent">The base parent</param>
    /// <param name="callback">The callback that is called based on symmetry, parameters are: q, r, rotation</param>
    /// <param name="overrideSymmetry">If set, overrides the current symmetry</param>
    protected void RunWithSymmetry(float diameter, Vector3 position, TMetaball? parent,
        Action<Vector3, TMetaball?> callback, HexEditorSymmetry? overrideSymmetry = null)
    {
        overrideSymmetry ??= Symmetry;

        switch (overrideSymmetry)
        {
            case HexEditorSymmetry.None:
            {
                callback(position, parent);
                break;
            }

            case HexEditorSymmetry.XAxisSymmetry:
            {
                var symmetryPosition = position * new Vector3(-1, 1, 1);

                if (position.DistanceTo(symmetryPosition) < diameter / 2)
                {
                    // If too close with the symmetry position, run as if symmetry was turned off to not have
                    // overlapping metaballs
                    callback(position, parent);
                }
                else
                {
                    callback(position, parent);

                    if (parent != null)
                    {
                        // Resolve symmetry position's parent if there was a parent for the primary position
                        var symmetryParent = editedMetaballs.GetClosestMetaballToPosition(
                            parent.Position * new Vector3(-1, 1, 1));

                        // TODO: should this verify that the parent is close enough to the actual position to use?

                        callback(symmetryPosition, symmetryParent);
                    }
                    else
                    {
                        callback(symmetryPosition, null);
                    }
                }

                break;
            }

            default:
                throw new NotSupportedException("Other symmetry modes are not implemented");
        }
    }

    protected override void OnCurrentActionCanceled()
    {
        base.OnCurrentActionCanceled();

        // TODO: switch to this going through the editor
        UpdateSymmetryButton();
    }

    protected override void OnActionStatusChanged()
    {
        base.OnActionStatusChanged();

        UpdateSymmetryButton();
    }

    protected virtual void OnMoveWillSucceed()
    {
        MovingPlacedMetaball = null;

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
        throw new NotImplementedException();
    }

    protected void UpdateAlreadyPlacedHexes(
        IEnumerable<(Hex BasePosition, IEnumerable<Hex> Hexes, bool PlacedThisSession)> hexes, List<Hex> islands,
        bool forceHide = false)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///   This updates the metaball displayer that is used to show the currently placed metaballs in the edited layout
    /// </summary>
    protected void UpdateAlreadyPlacedVisuals()
    {
        if (visualMetaballDisplayer == null || structuralMetaballDisplayer == null)
            throw new InvalidOperationException("Editor component not initialized");

        // If not currently visible at all, don't update visibility, it'll be updated once this tab becomes active
        // again
        if (visualMetaballDisplayer.Visible || structuralMetaballDisplayer.Visible)
        {
            visualMetaballDisplayer.Visible = PreviewMode;
            structuralMetaballDisplayer.Visible = !PreviewMode;
        }

        if (PreviewMode)
        {
            visualMetaballDisplayer.DisplayFromLayout(editedMetaballs);
        }
        else
        {
            structuralMetaballDisplayer.DisplayFromLayout(editedMetaballs);
        }
    }

    protected virtual void PerformActiveAction()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

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
    protected virtual bool IsMoveTargetValid(Vector3 position, MulticellularMetaball? rotation, TMetaball metaball)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual void OnMoveActionStarted()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual void PerformMove(Vector3 position, TMetaball parent)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual TAction? TryCreateMetaballRemoveAction(TMetaball metaball, ref int alreadyDeleted)
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

    protected virtual float CalculateEditorArrowZPosition()
    {
        throw new GodotAbstractMethodNotOverriddenException();
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

            positionReference.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnCameraPositionChanged(Transform3D newPosition)
    {
        // TODO: implement camera position saving
        _ = newPosition;
    }

    // TODO: make this method trigger automatically on Symmetry assignment
    private void UpdateSymmetryIcon()
    {
        componentBottomLeftButtons.SetSymmetry(symmetry);
    }

    private void OnResizeMetaballSliderDragged(float value)
    {
        metaballSize = value;
    }
}
