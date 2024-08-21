using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Body plan editor component for making body plans from metaballs
/// </summary>
[SceneLoadedClass("res://src/late_multicellular_stage/editor/MetaballBodyEditorComponent.tscn")]
public partial class MetaballBodyEditorComponent :
    MetaballEditorComponentBase<LateMulticellularEditor, CombinedEditorAction, EditorAction, MulticellularMetaball>,
    IGodotEarlyNodeResolve
{
    [Export]
    public NodePath? TabButtonsPath;

    [Export]
    public NodePath StructureTabButtonPath = null!;

    [Export]
    public NodePath ReproductionTabButtonPath = null!;

    [Export]
    public NodePath BehaviourTabButtonPath = null!;

    [Export]
    public NodePath AppearanceTabButtonPath = null!;

    [Export]
    public NodePath StructureTabPath = null!;

    [Export]
    public NodePath ReproductionTabPath = null!;

    [Export]
    public NodePath BehaviourTabPath = null!;

    [Export]
    public NodePath AppearanceTabPath = null!;

    [Export]
    public NodePath CellTypeSelectionListPath = null!;

    [Export]
    public NodePath ModifyTypeButtonPath = null!;

    [Export]
    public NodePath DeleteTypeButtonPath = null!;

    [Export]
    public NodePath DuplicateTypeButtonPath = null!;

    [Export]
    public NodePath CannotDeleteInUseTypeDialogPath = null!;

    [Export]
    public NodePath DuplicateCellTypeDialogPath = null!;

    [Export]
    public NodePath DuplicateCellTypeNamePath = null!;

    [Export]
    public NodePath MetaballPopupMenuPath = null!;

    [Export]
    public NodePath CannotReduceBrainPowerPopupPath = null!;

    private readonly Dictionary<string, CellTypeSelection> cellTypeSelectionButtons = new();

#pragma warning disable CA2213

    // Selection menu tab selector buttons
    private Button structureTabButton = null!;
    private Button reproductionTabButton = null!;
    private Button behaviourTabButton = null!;
    private Button appearanceTabButton = null!;

    private PanelContainer structureTab = null!;
    private PanelContainer reproductionTab = null!;
    private PanelContainer appearanceTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private BehaviourEditorSubComponent behaviourEditor = null!;

    private CollapsibleList cellTypeSelectionList = null!;

    private Button modifyTypeButton = null!;

    private Button deleteTypeButton = null!;

    private Button duplicateTypeButton = null!;

    private CustomWindow cannotDeleteInUseTypeDialog = null!;

    private CustomWindow duplicateCellTypeDialog = null!;

    private LineEdit duplicateCellTypeName = null!;

    private PackedScene cellTypeSelectionButtonScene = null!;

    private ButtonGroup cellTypeButtonGroup = new();

    private MetaballPopupMenu metaballPopupMenu = null!;

    private CustomConfirmationDialog cannotReduceBrainPowerPopup = null!;

    private PackedScene visualMetaballDisplayerScene = null!;

    private PackedScene structuralMetaballDisplayerScene = null!;
#pragma warning restore CA2213

    // TODO: add way to control the size of the placed metaball
    [JsonProperty]
    private float metaballSize = 1.0f;

    [JsonProperty]
    private string newName = "unset";

    private bool previewMode;

    /// <summary>
    ///   True when visuals of already placed things need to be updated
    /// </summary>
    private bool metaballDisplayDataDirty = true;

    [JsonProperty]
    private SelectionMenuTab selectedSelectionMenuTab = SelectionMenuTab.Structure;

    [Signal]
    public delegate void OnCellTypeToEditSelectedEventHandler(string name);

    public enum SelectionMenuTab
    {
        Structure,
        Reproduction,
        Behaviour,
        Appearance,
    }

    /// <summary>
    ///   If this is enabled, the creature will be shown as it would actually look in game. Editing metaballs is
    ///   disabled during preview (except for undo/redo).
    /// </summary>
    public bool PreviewMode
    {
        get => previewMode;
        set
        {
            bool updateNeeded = false;

            if (previewMode != value)
            {
                updateNeeded = true;
            }

            previewMode = value;

            if (updateNeeded)
                UpdateAlreadyPlacedVisuals();

            if (hoverMetaballDisplayer != null)
                hoverMetaballDisplayer.Visible = !PreviewMode;
        }
    }

    [JsonIgnore]
    public override bool HasIslands => editedMetaballs.GetMetaballsNotTouchingParents().Any();

    [JsonIgnore]
    public bool NodeReferencesResolved { get; private set; }

    protected override bool ForceHideHover => false;

    public override void _Ready()
    {
        base._Ready();

        ResolveNodeReferences();

        cellTypeSelectionButtonScene =
            GD.Load<PackedScene>("res://src/early_multicellular_stage/editor/CellTypeSelection.tscn");

        ApplySelectionMenuTab();

        RegisterTooltips();
    }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        NodeReferencesResolved = true;

        if (TabButtonsPath == null)
            throw new MissingExportVariableValueException();

        var tabButtons = GetNode<TabButtons>(TabButtonsPath);

        structureTab = GetNode<PanelContainer>(StructureTabPath);
        structureTabButton = GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, StructureTabButtonPath));

        reproductionTab = GetNode<PanelContainer>(ReproductionTabPath);
        reproductionTabButton =
            GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, ReproductionTabButtonPath));

        behaviourEditor = GetNode<BehaviourEditorSubComponent>(BehaviourTabPath);
        behaviourTabButton = GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, BehaviourTabButtonPath));

        appearanceTab = GetNode<PanelContainer>(AppearanceTabPath);
        appearanceTabButton =
            GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, AppearanceTabButtonPath));

        cellTypeSelectionList = GetNode<CollapsibleList>(CellTypeSelectionListPath);

        modifyTypeButton = GetNode<Button>(ModifyTypeButtonPath);

        deleteTypeButton = GetNode<Button>(DeleteTypeButtonPath);

        duplicateTypeButton = GetNode<Button>(DuplicateTypeButtonPath);

        cannotDeleteInUseTypeDialog = GetNode<CustomWindow>(CannotDeleteInUseTypeDialogPath);

        duplicateCellTypeDialog = GetNode<CustomWindow>(DuplicateCellTypeDialogPath);

        duplicateCellTypeName = GetNode<LineEdit>(DuplicateCellTypeNamePath);

        metaballPopupMenu = GetNode<MetaballPopupMenu>(MetaballPopupMenuPath);

        cannotReduceBrainPowerPopup = GetNode<CustomConfirmationDialog>(CannotReduceBrainPowerPopupPath);
    }

    public override void Init(LateMulticellularEditor owningEditor, bool fresh)
    {
        base.Init(owningEditor, fresh);
        behaviourEditor.Init(owningEditor, fresh);

        if (!fresh)
        {
            UpdateGUIAfterLoadingSpecies();
        }

        UpdateCancelButtonVisibility();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Visible)
            return;

        var debugOverlays = DebugOverlays.Instance;

        if (debugOverlays.PerformanceMetricsVisible)
        {
            var roughCount = Editor.RootOfDynamicallySpawned.GetChildCount();
            debugOverlays.ReportEntities(roughCount);
        }

        if (metaballDisplayDataDirty)
        {
            OnMetaballsChanged();
            metaballDisplayDataDirty = false;
        }

        // Show the ball that is about to be placed
        if (activeActionName != null && Editor.ShowHover && !PreviewMode)
        {
            GetMouseMetaball(out var position, out var parentMetaball);

            var effectiveSymmetry = Symmetry;

            var cellType = CellTypeFromName(activeActionName);

            if (MovingPlacedMetaball == null)
            {
                // Can place stuff at all?
                isPlacementProbablyValid = IsValidPlacement(position, parentMetaball);
            }
            else
            {
                isPlacementProbablyValid = IsMoveTargetValid(position, parentMetaball, MovingPlacedMetaball);

                if (!Settings.Instance.MoveOrganellesWithSymmetry)
                    effectiveSymmetry = HexEditorSymmetry.None;
            }

            RunWithSymmetry(position, parentMetaball,
                (finalPosition, finalParent) => RenderHighlightedMetaball(finalPosition, finalParent, cellType),
                effectiveSymmetry);
        }
    }

    public override void OnEditorSpeciesSetup(Species species)
    {
        UpdateCellTypeSelections();

        behaviourEditor.OnEditorSpeciesSetup(species);

        var metaballMapping = new Dictionary<Metaball, MulticellularMetaball>();

        foreach (var metaball in Editor.EditedSpecies.BodyLayout)
        {
            editedMetaballs.Add(metaball.Clone(metaballMapping));
        }

        newName = species.FormattedName;

        UpdateGUIAfterLoadingSpecies();

        UpdateArrow(false);
    }

    public override void OnFinishEditing()
    {
        var editedSpecies = Editor.EditedSpecies;

        editedSpecies.BodyLayout.Clear();

        var metaballMapping = new Dictionary<Metaball, MulticellularMetaball>();

        // Make sure we process things with parents first
        // TODO: if the tree depth calculation is too expensive here, we'll need to cache the values in the metaball
        // objects
        foreach (var metaball in editedMetaballs.OrderBy(m => m.CalculateTreeDepth()))
        {
            editedSpecies.BodyLayout.Add(metaball.Clone(metaballMapping));
        }

        var previousStage = editedSpecies.MulticellularType;

        editedSpecies.OnEdited();

        // Make awakening an explicit step instead of automatic
        if (previousStage != editedSpecies.MulticellularType &&
            editedSpecies.MulticellularType == MulticellularSpeciesType.Awakened)
        {
            GD.Print("Player is now eligible for awakening, preventing automatic move there");
            editedSpecies.KeepPlayerInAwareStage();
        }

        editedSpecies.UpdateNameIfValid(newName);

        behaviourEditor.OnFinishEditing();
    }

    public override bool CanFinishEditing(IEnumerable<EditorUserOverride> userOverrides)
    {
        var editorUserOverrides = userOverrides.ToList();
        if (!base.CanFinishEditing(editorUserOverrides))
            return false;

        // TODO: hook this up once we have editing the creature scale in the editor
        var creatureScale = 1.0f;
        var newTypeWouldBe =
            LateMulticellularSpecies.CalculateMulticellularTypeFromLayout(editedMetaballs, creatureScale);

        // Disallow going back stages
        if (newTypeWouldBe < Editor.EditedSpecies.MulticellularType)
        {
            GD.Print("Reducing brain power would go back a stage, not allowing");
            cannotReduceBrainPowerPopup.PopupCenteredShrink();
            return false;
        }

        if (editorUserOverrides.Contains(EditorUserOverride.NotProducingEnoughATP))
            return true;

        // TODO: warning about not producing enough ATP if entire body plan would be negative

        return true;
    }

    public void OnTissueTypeEdited(CellType changedType)
    {
        // TODO: check that undo/redo while in a different tab doesn't cause this to make unintended things visible
        UpdateAlreadyPlacedVisuals();

        UpdateCellTypeSelections();

        RegenerateCellTypeIcon(changedType);
    }

    [RunOnKeyDown("e_secondary")]
    public bool ShowMetaballOptions()
    {
        // Need to prevent this from running when not visible to not conflict in an editor with multiple tabs
        if (!Visible)
            return false;

        if (PreviewMode)
            return false;

        // Can't open popup menu while moving something
        if (MovingPlacedMetaball != null)
        {
            Editor.OnActionBlockedWhileMoving();
            return true;
        }

        GetMouseMetaball(out _, out var metaball);

        var metaballs = new List<MulticellularMetaball>();

        if (metaball != null)
            metaballs.Add(metaball);

        /*
        RunWithSymmetry(q, r, (symmetryQ, symmetryR, _) =>
        {
            var cell = editedMetaballs.GetElementAt(new Hex(symmetryQ, symmetryR));

            if (cell != null)
                metaballs.Add(cell);
        });*/

        if (metaballs.Count < 1)
            return true;

        ShowCellMenu(metaballs.Select(h => h).Distinct());
        return true;
    }

    public override void SetEditorWorldTabSpecificObjectVisibility(bool shown)
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

    protected CellType CellTypeFromName(string name)
    {
        return Editor.EditedSpecies.CellTypes.First(c => c.TypeName == name);
    }

    protected override void OnTranslationsChanged()
    {
    }

    protected override int CalculateCurrentActionCost()
    {
        if (activeActionName == null || !Editor.ShowHover)
            return 0;

        var cellType = CellTypeFromName(activeActionName);

        if (MouseHoverPositions == null)
            return Constants.METABALL_ADD_COST * Symmetry.PositionCount();

        var positions = MouseHoverPositions.ToList();

        var cellTemplates = positions.Select(p => new MulticellularMetaball(cellType)
        {
            Position = p.Position,
            Parent = p.Parent,
        }).ToList();

        // TODO: it's extremely unlikely that metaballs would overlap exactly so we can probably remove the occupancy
        // check here
        CombinedEditorAction moveOccupancies;

        if (MovingPlacedMetaball == null)
        {
            moveOccupancies = GetMultiActionWithOccupancies(positions, cellTemplates, false);
        }
        else
        {
            moveOccupancies = GetMultiActionWithOccupancies(positions.Take(1).ToList(),
                new List<MulticellularMetaball> { MovingPlacedMetaball }, true);
        }

        return Editor.WhatWouldActionsCost(moveOccupancies.Data);
    }

    protected override void LoadScenes()
    {
        base.LoadScenes();

        visualMetaballDisplayerScene =
            GD.Load<PackedScene>("res://src/late_multicellular_stage/MulticellularConvolutionDisplayer.tscn");
        structuralMetaballDisplayerScene =
            GD.Load<PackedScene>("res://src/late_multicellular_stage/MulticellularMetaballDisplayer.tscn");
    }

    protected override MetaballLayout<MulticellularMetaball> CreateLayout()
    {
        return new MetaballLayout<MulticellularMetaball>(OnMetaballAdded, OnMetaballRemoved);
    }

    protected override IMetaballDisplayer<MulticellularMetaball> CreateVisualMetaballDisplayer()
    {
        var displayer = visualMetaballDisplayerScene.Instantiate<MulticellularConvolutionDispayer>();
        Editor.RootOfDynamicallySpawned.AddChild(displayer);
        return displayer;
    }

    protected override IMetaballDisplayer<MulticellularMetaball> CreateStructuralMetaballDisplayer()
    {
        var displayer = structuralMetaballDisplayerScene.Instantiate<MulticellularMetaballDisplayer>();
        Editor.RootOfDynamicallySpawned.AddChild(displayer);
        return displayer;
    }

    protected override void PerformActiveAction()
    {
        bool added =
            AddMetaball(CellTypeFromName(activeActionName ?? throw new InvalidOperationException("no action active")));

        if (added)
        {
            // TODO: maybe a tutorial for this editor?
        }
    }

    protected override void PerformMove(Vector3 position, MulticellularMetaball parent)
    {
        if (!MoveMetaball(MovingPlacedMetaball!, position, parent))
        {
            Editor.OnInvalidAction();
        }
    }

    protected override bool IsMoveTargetValid(Vector3 position, MulticellularMetaball? parent,
        MulticellularMetaball metaball)
    {
        return editedMetaballs.CanAdd(metaball);
    }

    protected override void OnCurrentActionCanceled()
    {
        editedMetaballs.Add(MovingPlacedMetaball!);
        MovingPlacedMetaball = null;
        base.OnCurrentActionCanceled();
    }

    protected override void OnMoveActionStarted()
    {
        editedMetaballs.Remove(MovingPlacedMetaball!);
    }

    protected override EditorAction? TryCreateMetaballRemoveAction(MulticellularMetaball metaball,
        ref int alreadyDeleted)
    {
        // Dont allow deletion of last metaball
        if (editedMetaballs.Count - alreadyDeleted < 2)
            return null;

        ++alreadyDeleted;
        return new SingleEditorAction<MetaballRemoveActionData<MulticellularMetaball>>(DoMetaballRemoveAction,
            UndoMetaballRemoveAction,
            new MetaballRemoveActionData<MulticellularMetaball>(metaball,
                MetaballRemoveActionData<MulticellularMetaball>.CreateMovementActionForChildren(metaball,
                    editedMetaballs)));
    }

    protected override float CalculateEditorArrowZPosition()
    {
        // The calculation falls back to 0 if there are no metaballs found in the middle "column"
        var highestPointInMiddleRows = 0.0f;

        foreach (var metaball in editedMetaballs)
        {
            // Only consider the middle 3 rows
            if (metaball.Position.X is < -3 or > 3)
                continue;

            // Get the min z-axis (highest point in the editor)
            highestPointInMiddleRows = Mathf.Min(highestPointInMiddleRows, metaball.Position.Z);
        }

        return highestPointInMiddleRows;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (TabButtonsPath != null)
            {
                TabButtonsPath.Dispose();
                StructureTabButtonPath.Dispose();
                ReproductionTabButtonPath.Dispose();
                BehaviourTabButtonPath.Dispose();
                AppearanceTabButtonPath.Dispose();
                StructureTabPath.Dispose();
                ReproductionTabPath.Dispose();
                BehaviourTabPath.Dispose();
                AppearanceTabPath.Dispose();
                CellTypeSelectionListPath.Dispose();
                ModifyTypeButtonPath.Dispose();
                DeleteTypeButtonPath.Dispose();
                DuplicateTypeButtonPath.Dispose();
                CannotDeleteInUseTypeDialogPath.Dispose();
                DuplicateCellTypeDialogPath.Dispose();
                DuplicateCellTypeNamePath.Dispose();
                MetaballPopupMenuPath.Dispose();
                CannotReduceBrainPowerPopupPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateGUIAfterLoadingSpecies()
    {
        GD.Print("Starting early multicellular editor with: ", editedMetaballs.Count,
            " cells in the microbe");

        SetSpeciesInfo(newName,
            behaviourEditor.Behaviour ?? throw new Exception("Editor doesn't have Behaviour setup"));
    }

    private void SetSpeciesInfo(string name, BehaviourDictionary behaviour)
    {
        componentBottomLeftButtons.SetNewName(name);

        // TODO: put this call in some better place (also in CellEditorComponent)
        behaviourEditor.UpdateAllBehaviouralSliders(behaviour);
    }

    private void ShowCellMenu(IEnumerable<MulticellularMetaball> selectedMetaballs)
    {
        metaballPopupMenu.SelectedMetaballs = selectedMetaballs.ToList();
        metaballPopupMenu.GetActionPrice = Editor.WhatWouldActionsCost;
        metaballPopupMenu.ShowPopup = true;

        metaballPopupMenu.EnableDeleteOption = editedMetaballs.Count > 1;
        metaballPopupMenu.EnableMoveOption = editedMetaballs.Count > 1;
    }

    private Vector3 FinalMetaballPosition(Vector3 position, MulticellularMetaball parent, float? size = null)
    {
        size ??= metaballSize;
        var direction = (position - parent.Position).Normalized();

        return parent.Position + direction * (parent.Radius + size.Value * 0.5f);
    }

    private void RenderHighlightedMetaball(Vector3 position, MulticellularMetaball? parent, CellType cellToPlace)
    {
        if (MovingPlacedMetaball == null && activeActionName == null)
            return;

        var metaball = new MulticellularMetaball(cellToPlace)
        {
            Parent = parent,
            Position = parent != null ? FinalMetaballPosition(position, parent) : position,
            Size = metaballSize,
        };

        if (hoverMetaballData.Count <= usedHoverMetaballIndex)
        {
            hoverMetaballData.Add(metaball);
            hoverMetaballsChanged = true;
        }
        else
        {
            var existing = hoverMetaballData[usedHoverMetaballIndex];

            if (!existing.Equals(metaball))
            {
                // TODO: should we reuse object instances when possible?
                hoverMetaballData[usedHoverMetaballIndex] = metaball;
                hoverMetaballsChanged = true;
            }
        }

        ++usedHoverMetaballIndex;
    }

    /// <summary>
    ///   Places a cell of the specified type under the cursor and also applies symmetry to place multiple
    /// </summary>
    /// <returns>True when at least one metaball got placed</returns>
    private bool AddMetaball(CellType cellType)
    {
        GetMouseMetaball(out var position, out var parentMetaball);

        var placementActions = new List<EditorAction>();

        // For symmetrically placed cells keep track of where we already placed something
        var usedPositions = new HashSet<Vector3>();

        RunWithSymmetry(position, parentMetaball,
            (symmetryPosition, symmetryParent) =>
            {
                if (symmetryParent == null)
                    return;

                if (usedPositions.Contains(symmetryPosition))
                {
                    // Duplicate with already placed
                    return;
                }

                var placed = CreatePlaceActionIfPossible(cellType, symmetryPosition, metaballSize, symmetryParent);

                if (placed != null)
                {
                    placementActions.Add(placed);

                    usedPositions.Add(symmetryPosition);
                }
            });

        if (placementActions.Count < 1)
            return false;

        var multiAction = new CombinedEditorAction(placementActions);

        return EnqueueAction(multiAction);
    }

    /// <summary>
    ///   Helper for AddMetaball
    /// </summary>
    private EditorAction? CreatePlaceActionIfPossible(CellType cellType, Vector3 position, float size,
        MulticellularMetaball parent)
    {
        var metaball = new MulticellularMetaball(cellType)
        {
            Position = FinalMetaballPosition(position, parent, size),
            Parent = parent,
            Size = size,
        };

        if (!IsValidPlacement(metaball))
        {
            // Play Sound
            Editor.OnInvalidAction();
            return null;
        }

        return CreateAddCellAction(metaball, parent);
    }

    private bool IsValidPlacement(MulticellularMetaball metaball)
    {
        return IsValidPlacement(metaball.Position, metaball.Parent);
    }

    private bool IsValidPlacement(Vector3 position, Metaball? parent)
    {
        // TODO: in the future we might want to prevent metaballs from overlapping too much, for now just check it has
        // a parent
        _ = position;

        return parent != null;
    }

    private EditorAction CreateAddCellAction(MulticellularMetaball metaball, MulticellularMetaball parent)
    {
        return new SingleEditorAction<MetaballPlacementActionData<MulticellularMetaball>>(DoMetaballPlaceAction,
            UndoMetaballPlaceAction,
            new MetaballPlacementActionData<MulticellularMetaball>(metaball)
            {
                Parent = parent,
            });
    }

    /// <summary>
    ///   See: <see cref="CellEditorComponent.GetOccupancies"/>
    /// </summary>
    private IEnumerable<
            (Vector3 Position, MulticellularMetaball Metaball, MulticellularMetaball? Parent, bool Occupied)>
        GetOccupancies(List<(Vector3 Position, MulticellularMetaball? Parent)> metaballPositions,
            List<MulticellularMetaball> metaballs)
    {
        var cellPositions =
            new List<
                (Vector3 Position, MulticellularMetaball Metaball, MulticellularMetaball? Parent, bool Occupied)>();
        for (var i = 0; i < metaballPositions.Count; i++)
        {
            var (hex, orientation) = metaballPositions[i];
            var cell = metaballs[i];

            // TODO: find metaball that overlaps with the position too much
            // var oldCell = cellPositions.FirstOrDefault(p => p.Hex == hex);
            // bool occupied = oldCell != default;
            bool occupied = false;

            cellPositions.Add((hex, cell, orientation, occupied));
        }

        return cellPositions;
    }

    private CombinedEditorAction GetMultiActionWithOccupancies(
        List<(Vector3 Position, MulticellularMetaball? Parent)> metaballPositions,
        List<MulticellularMetaball> metaballs, bool moving)
    {
        var moveActionData = new List<EditorAction>();
        foreach (var (position, metaball, parent, occupied) in GetOccupancies(metaballPositions, metaballs))
        {
            EditorAction action;
            if (occupied)
            {
                // TODO: should this remove the existing ones?
                continue;
            }

            if (moving)
            {
                // If the metaball is moved to its descendant, then the move is much more complicated
                // And currently not supported
                if (parent != null && editedMetaballs.IsDescendantsOf(parent, metaball))
                {
                    GD.PrintErr("Logic for moving metaball to its descendant tree not implemented");
                    continue;
                }

                var childMoves =
                    MetaballMoveActionData<MulticellularMetaball>.CreateMovementActionForChildren(metaball,
                        metaball.Position, position, editedMetaballs);

                var data = new MetaballMoveActionData<MulticellularMetaball>(metaball, metaball.Position, position,
                    metaball.Parent, parent, childMoves);
                action = new SingleEditorAction<MetaballMoveActionData<MulticellularMetaball>>(DoMetaballMoveAction,
                    UndoMetaballMoveAction, data);
            }
            else
            {
                action = new SingleEditorAction<MetaballPlacementActionData<MulticellularMetaball>>(
                    DoMetaballPlaceAction,
                    UndoMetaballPlaceAction,
                    new MetaballPlacementActionData<MulticellularMetaball>(metaball, position, metaballSize,
                        parent));
            }

            moveActionData.Add(action);
        }

        return new CombinedEditorAction(moveActionData);
    }

    private bool MoveMetaball(MulticellularMetaball metaball, Vector3 newLocation, MulticellularMetaball? newParent)
    {
        // Make sure placement is valid
        if (!IsMoveTargetValid(newLocation, newParent, metaball))
            return false;

        // For now moving to descendant tree is not implement as it would be pretty tricky to get working correctly
        if (newParent != null && editedMetaballs.IsDescendantsOf(newParent, metaball))
        {
            ToolTipManager.Instance.ShowPopup(Localization.Translate("CANNOT_MOVE_METABALL_TO_DESCENDANT_TREE"),
                3.0f);
            return false;
        }

        var multiAction = GetMultiActionWithOccupancies(
            new List<(Vector3 Position, MulticellularMetaball? Parent)> { (newLocation, newParent) },
            new List<MulticellularMetaball> { metaball },
            true);

        // Too low mutation points, cancel move
        if (Editor.MutationPoints < Editor.WhatWouldActionsCost(multiAction.Data))
        {
            CancelCurrentAction();
            Editor.OnInsufficientMP(false);
            return false;
        }

        EnqueueAction(multiAction);

        // It's assumed that the above enqueue can't fail, otherwise the reference to MovingPlacedMetaball may be
        // permanently lost (as the code that calls this assumes it's safe to set MovingPlacedMetaball to null
        // when we return true)
        return true;
    }

    private void OnMovePressed()
    {
        if (Settings.Instance.MoveOrganellesWithSymmetry.Value)
        {
            // Start moving the cells symmetrical to the clicked cell.
            StartMetaballMoveWithSymmetry(metaballPopupMenu.SelectedMetaballs);
        }
        else
        {
            StartMetaballMove(metaballPopupMenu.SelectedMetaballs.First());
        }
    }

    private void OnDeletePressed()
    {
        int alreadyDeleted = 0;
        var action =
            new CombinedEditorAction(metaballPopupMenu.SelectedMetaballs
                .Select(m => TryCreateMetaballRemoveAction(m, ref alreadyDeleted)).WhereNotNull());
        EnqueueAction(action);
    }

    private void OnModifyPressed()
    {
        EmitSignal(SignalName.OnCellTypeToEditSelected, metaballPopupMenu.SelectedMetaballs.First().CellType.TypeName);
    }

    /// <summary>
    ///   Sets up or updates the list of buttons to select cell types to place
    /// </summary>
    private void UpdateCellTypeSelections()
    {
        // Re-use / create more buttons to hold all the cell types
        foreach (var cellType in Editor.EditedSpecies.CellTypes.OrderBy(t => t.TypeName, StringComparer.Ordinal))
        {
            if (!cellTypeSelectionButtons.TryGetValue(cellType.TypeName, out var control))
            {
                // Need new button
                control = cellTypeSelectionButtonScene.Instantiate<CellTypeSelection>();
                control.SelectionGroup = cellTypeButtonGroup;

                control.PartName = cellType.TypeName;
                control.CellType = cellType;
                control.Name = cellType.TypeName;

                cellTypeSelectionList.AddItem(control);
                cellTypeSelectionButtons.Add(cellType.TypeName, control);

                control.Connect(MicrobePartSelection.SignalName.OnPartSelected,
                    new Callable(this, nameof(OnCellToPlaceSelected)));
            }

            control.MPCost = Constants.METABALL_ADD_COST;

            // TODO: tooltips for these
        }

        bool clearSelection = false;

        // Delete no longer needed buttons
        foreach (var key in cellTypeSelectionButtons.Keys.ToList())
        {
            if (Editor.EditedSpecies.CellTypes.All(t => t.TypeName != key))
            {
                var control = cellTypeSelectionButtons[key];
                cellTypeSelectionButtons.Remove(key);

                control.DetachAndQueueFree();

                if (activeActionName == key)
                    clearSelection = true;
            }
        }

        if (clearSelection)
            ClearSelectedAction();
    }

    private void OnCellToPlaceSelected(string cellTypeName)
    {
        if (!cellTypeSelectionButtons.TryGetValue(cellTypeName, out _))
        {
            GD.PrintErr("Attempted to select an unknown cell type");
            return;
        }

        activeActionName = cellTypeName;

        OnCurrentActionChanged();
    }

    private void OnCurrentActionChanged()
    {
        // Enable the duplicate, delete, edit buttons for the cell type
        if (!string.IsNullOrEmpty(activeActionName))
        {
            modifyTypeButton.Disabled = false;
            deleteTypeButton.Disabled = false;
            duplicateTypeButton.Disabled = false;

            if (!cellTypeSelectionButtons.TryGetValue(activeActionName!, out var cellTypeButton))
            {
                GD.PrintErr("Invalid active action for highlight update");
                return;
            }

            // Update the icon highlightings
            foreach (var element in cellTypeSelectionButtons.Values)
            {
                element.Selected = element == cellTypeButton;
            }
        }
        else
        {
            // Clear all highlights
            foreach (var element in cellTypeSelectionButtons.Values)
            {
                element.Selected = false;
            }
        }
    }

    private void ClearSelectedAction()
    {
        activeActionName = null;
        OnCurrentActionChanged();
    }

    private void OnMetaballsChanged()
    {
        UpdateAlreadyPlacedVisuals();

        UpdateArrow();
    }

    /// <summary>
    ///   This updates the metaball displayer that is used to show the currently placed metaballs in the edited layout
    /// </summary>
    private void UpdateAlreadyPlacedVisuals()
    {
        if (visualMetaballDisplayer == null || structuralMetaballDisplayer == null)
            throw new InvalidOperationException("Editor component not initialized");

        visualMetaballDisplayer.Visible = PreviewMode;
        structuralMetaballDisplayer.Visible = !PreviewMode;

        if (PreviewMode)
        {
            visualMetaballDisplayer.DisplayFromLayout(editedMetaballs);
        }
        else
        {
            structuralMetaballDisplayer.DisplayFromLayout(editedMetaballs);
        }
    }

    private void OnSpeciesNameChanged(string newText)
    {
        newName = newText;
    }

    private void OnTypeDuplicateStartPressed()
    {
        if (string.IsNullOrEmpty(activeActionName))
            return;

        GUICommon.Instance.PlayButtonPressSound();

        var type = CellTypeFromName(activeActionName!);

        duplicateCellTypeName.Text = type.TypeName;

        // Make sure it's shown in red initially as it is a duplicate name
        OnNewCellTypeNameChanged(type.TypeName);

        duplicateCellTypeDialog.PopupCenteredShrink();

        duplicateCellTypeName.GrabFocusInOpeningPopup();
        duplicateCellTypeName.SelectAll();
        duplicateCellTypeName.CaretColumn = type.TypeName.Length;
    }

    private void OnNewCellTypeNameChanged(string newText)
    {
        if (!IsNewCellTypeNameValid(newText))
        {
            GUICommon.MarkInputAsInvalid(duplicateCellTypeName);
        }
        else
        {
            GUICommon.MarkInputAsValid(duplicateCellTypeName);
        }
    }

    private bool IsNewCellTypeNameValid(string text)
    {
        // Name is invalid if it is empty or a duplicate
        // TODO: should this ensure the name doesn't have trailing whitespace?
        return !string.IsNullOrWhiteSpace(text) && !Editor.EditedSpecies.CellTypes.Any(c =>
            c.TypeName.Equals(text, StringComparison.InvariantCultureIgnoreCase));
    }

    private void OnNewCellTextAccepted(string text)
    {
        // Just to make sure the latest text is already available to us
        if (duplicateCellTypeName.Text != text)
            GD.PrintErr("Text not updated");

        OnNewCellTypeConfirmed();
    }

    private void OnNewCellTypeConfirmed()
    {
        var newTypeName = duplicateCellTypeName.Text;

        if (!IsNewCellTypeNameValid(newTypeName))
        {
            GD.Print("Bad name for new cell type");
            Editor.OnInvalidAction();
            return;
        }

        var type = CellTypeFromName(activeActionName!);

        // TODO: make this a reversible action
        var newType = (CellType)type.Clone();
        newType.TypeName = newTypeName;

        Editor.EditedSpecies.CellTypes.Add(newType);
        GD.Print("New cell type created: ", newType.TypeName);

        UpdateCellTypeSelections();

        duplicateCellTypeDialog.Hide();
    }

    private void OnDeleteCellTypePressed()
    {
        if (string.IsNullOrEmpty(activeActionName))
            return;

        GUICommon.Instance.PlayButtonPressSound();

        var type = CellTypeFromName(activeActionName!);

        // Disallow deleting a type that is in use currently
        if (editedMetaballs.Any(c => c.CellType == type))
        {
            GD.Print("Can't delete in use cell type");
            cannotDeleteInUseTypeDialog.PopupCenteredShrink();
            return;
        }

        // TODO: make a reversible action
        if (!Editor.EditedSpecies.CellTypes.Remove(type))
        {
            GD.PrintErr("Failed to delete cell type from species");
        }

        UpdateCellTypeSelections();
    }

    private void OnModifyCurrentCellTypePressed()
    {
        if (string.IsNullOrEmpty(activeActionName))
            return;

        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(SignalName.OnCellTypeToEditSelected, activeActionName);
    }

    private void RegenerateCellTypeIcon(CellType type)
    {
        foreach (var entry in cellTypeSelectionButtons)
        {
            if (entry.Value.CellType == type)
            {
                entry.Value.ReportTypeChanged();
            }
        }
    }

    private void SetSelectionMenuTab(string tab)
    {
        var selection = (SelectionMenuTab)Enum.Parse(typeof(SelectionMenuTab), tab);

        if (selection == selectedSelectionMenuTab)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        selectedSelectionMenuTab = selection;
        ApplySelectionMenuTab();
    }

    private void ApplySelectionMenuTab()
    {
        // Hide all
        structureTab.Hide();
        reproductionTab.Hide();
        behaviourEditor.Hide();
        appearanceTab.Hide();

        // Show selected
        switch (selectedSelectionMenuTab)
        {
            case SelectionMenuTab.Structure:
            {
                structureTab.Show();
                structureTabButton.ButtonPressed = true;
                PreviewMode = false;
                break;
            }

            case SelectionMenuTab.Reproduction:
            {
                reproductionTab.Show();
                reproductionTabButton.ButtonPressed = true;
                PreviewMode = false;
                break;
            }

            case SelectionMenuTab.Behaviour:
            {
                behaviourEditor.Show();
                behaviourTabButton.ButtonPressed = true;
                PreviewMode = false;
                break;
            }

            case SelectionMenuTab.Appearance:
            {
                appearanceTab.Show();
                appearanceTabButton.ButtonPressed = true;
                PreviewMode = true;
                break;
            }

            default:
                throw new Exception("Invalid selection menu tab");
        }
    }
}
