using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Body plan editor component for making body plans from hexes (that represent cells)
/// </summary>
[SceneLoadedClass("res://src/early_multicellular_stage/editor/CellBodyPlanEditorComponent.tscn")]
public partial class CellBodyPlanEditorComponent :
    HexEditorComponentBase<EarlyMulticellularEditor, CellEditorAction, CellTemplate>,
    IGodotEarlyNodeResolve
{
    [Export]
    public NodePath StructureTabButtonPath = null!;

    [Export]
    public NodePath ReproductionTabButtonPath = null!;

    [Export]
    public NodePath BehaviourTabButtonPath = null!;

    [Export]
    public NodePath StructureTabPath = null!;

    [Export]
    public NodePath ReproductionTabPath = null!;

    [Export]
    public NodePath BehaviourTabPath = null!;

    [Export]
    public NodePath CellTypeSelectionListPath = null!;

    private readonly Dictionary<string, CellTypeSelection> cellTypeSelectionButtons = new();

    // Selection menu tab selector buttons
    private Button structureTabButton = null!;
    private Button reproductionTabButton = null!;
    private Button behaviourTabButton = null!;

    private PanelContainer structureTab = null!;
    private PanelContainer reproductionTab = null!;

    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    private BehaviourEditorSubComponent behaviourEditor = null!;

    private CollapsibleList cellTypeSelectionList = null!;

    private PackedScene cellTypeSelectionButtonScene = null!;

    private ButtonGroup cellTypeButtonGroup = new();

    private PackedScene microbeScene = null!;

    [JsonProperty]
    private string newName = "unset";

    [JsonProperty]
    private IndividualHexLayout<CellTemplate> editedMicrobeOrganelles = null!;

    /// <summary>
    ///   True when visuals of already placed things need to be updated
    /// </summary>
    private bool organelleDataDirty = true;

    [JsonProperty]
    private SelectionMenuTab selectedSelectionMenuTab = SelectionMenuTab.Structure;

    public enum SelectionMenuTab
    {
        Structure,
        Reproduction,
        Behaviour,
    }

    [JsonIgnore]
    public override bool HasIslands => editedMicrobeOrganelles.GetIslandHexes().Count > 0;

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

    public override void Init(EarlyMulticellularEditor owningEditor, bool fresh)
    {
        base.Init(owningEditor, fresh);
        behaviourEditor.Init(owningEditor, fresh);

        var newLayout = new IndividualHexLayout<CellTemplate>(OnCellAdded, OnCellRemoved);

        if (fresh)
        {
            editedMicrobeOrganelles = newLayout;
        }
        else
        {
            // We assume that the loaded save layout did not have anything weird set for the callbacks as we
            // do this rather than use SaveApplyHelpers
            foreach (var editedMicrobeOrganelle in editedMicrobeOrganelles)
            {
                newLayout.Add(editedMicrobeOrganelle);
            }

            editedMicrobeOrganelles = newLayout;

            if (Editor.EditedCellProperties != null)
            {
                UpdateGUIAfterLoadingSpecies(Editor.EditedBaseSpecies);
                UpdateArrow(false);
            }
            else
            {
                GD.Print("Loaded cell editor with no cell to edit set");
            }
        }

        UpdateCancelButtonVisibility();
    }

    public override void ResolveNodeReferences()
    {
        if (NodeReferencesResolved)
            return;

        base.ResolveNodeReferences();

        NodeReferencesResolved = true;

        structureTab = GetNode<PanelContainer>(StructureTabPath);
        structureTabButton = GetNode<Button>(StructureTabButtonPath);

        reproductionTab = GetNode<PanelContainer>(ReproductionTabPath);
        reproductionTabButton = GetNode<Button>(ReproductionTabButtonPath);

        behaviourTabButton = GetNode<Button>(BehaviourTabButtonPath);
        behaviourEditor = GetNode<BehaviourEditorSubComponent>(BehaviourTabPath);

        cellTypeSelectionList = GetNode<CollapsibleList>(CellTypeSelectionListPath);
    }

    public override void OnEditorSpeciesSetup(Species species)
    {
        SetupCellTypeSelections();

        behaviourEditor.OnEditorSpeciesSetup(species);

        // Undo the transformation that happens in OnFinishEditing to make the final layout, to go back to single hexes
        // representing each cell in the layout
        foreach (var cell in Editor.EditedSpecies.Cells)
        {
            var hex = new HexWithData<CellTemplate>((CellTemplate)cell.Clone());

            var originalPos = hex.Position;

            // This is a bit dirty but I couldn't figure out any other way to make these nested loops count either up
            // or down depending on the originalPos coordinate sign

            // Start at 0,0 and move towards the real position (loop is selected based on which direction the
            // original position is in)
            // TODO: should this also move to a vector direction approach as in OnFinishEditing?
            if (originalPos.Q < 0 && originalPos.R >= 0)
            {
                for (int q = 0; q >= originalPos.Q; --q)
                {
                    for (int r = 0; r <= originalPos.R; ++r)
                    {
                        if (TryAddHexToEditedLayout(hex, q, r))
                        {
                            goto successfulPlace;
                        }
                    }
                }
            }
            else if (originalPos.Q < 0 && originalPos.R < 0)
            {
                for (int q = 0; q >= originalPos.Q; --q)
                {
                    for (int r = 0; r >= originalPos.R; --r)
                    {
                        if (TryAddHexToEditedLayout(hex, q, r))
                        {
                            goto successfulPlace;
                        }
                    }
                }
            }
            else if (originalPos.Q >= 0 && originalPos.R >= 0)
            {
                for (int q = 0; q <= originalPos.Q; ++q)
                {
                    for (int r = 0; r <= originalPos.R; ++r)
                    {
                        if (TryAddHexToEditedLayout(hex, q, r))
                        {
                            goto successfulPlace;
                        }
                    }
                }
            }
            else if (originalPos.Q >= 0 && originalPos.R < 0)
            {
                for (int q = 0; q <= originalPos.Q; ++q)
                {
                    for (int r = 0; r >= originalPos.R; --r)
                    {
                        if (TryAddHexToEditedLayout(hex, q, r))
                        {
                            goto successfulPlace;
                        }
                    }
                }
            }
            else
            {
                throw new Exception(
                    "execution should not reach here (incorrect Q and R negativity combination checks)");
            }

            // We only get here if we didn't find a valid position to put the thing in
            throw new Exception("could not find position to put cell placeholder hex");

        successfulPlace:

            // This is needed to make code checks happy as with empty statement a different code check is unhappy
            // ReSharper disable once RedundantJumpStatement
            continue;
        }

        newName = species.FormattedName;

        UpdateGUIAfterLoadingSpecies(Editor.EditedBaseSpecies);

        UpdateArrow(false);
    }

    public override void OnFinishEditing()
    {
        var editedSpecies = Editor.EditedSpecies;

        // Compute final cell layout positions and update the species
        // TODO: maybe in the future we want to switch to editing the full hex layout with the entire cells in this
        // editor so this step can be skipped
        editedSpecies.Cells.Clear();

        foreach (var hexWithData in editedMicrobeOrganelles)
        {
            var direction = new Vector2(1, 0);

            if (hexWithData.Position != new Hex(0, 0))
            {
                direction = new Vector2(hexWithData.Position.Q, hexWithData.Position.R).Normalized();
            }

            hexWithData.Data!.Position = new Hex(0, 0);

            int distance = 0;

            while (true)
            {
                var positionVector = direction * distance;
                hexWithData.Data!.Position = new Hex((int)positionVector.x, (int)positionVector.y);

                if (editedSpecies.Cells.CanPlace(hexWithData.Data))
                {
                    editedSpecies.Cells.Add(hexWithData.Data);
                    break;
                }

                ++distance;
            }
        }

        editedSpecies.RepositionToOrigin();

        editedSpecies.UpdateInitialCompounds();

        editedSpecies.UpdateNameIfValid(newName);

        behaviourEditor.OnFinishEditing();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!Visible)
            return;

        if (organelleDataDirty)
        {
            OnOrganellesChanged();
            organelleDataDirty = false;
        }

        // Show the cell that is about to be placed
        if (activeActionName != null && Editor.ShowHover)
        {
            GetMouseHex(out int q, out int r);

            var effectiveSymmetry = Symmetry;

            if (MovingPlacedHex == null)
            {
                // Can place stuff at all?
                // TODO: should organelleRot be used here in some way?
                isPlacementProbablyValid = IsValidPlacement(
                    new HexWithData<CellTemplate>(new CellTemplate(CellTypeFromName(activeActionName)))
                    {
                        Position = new Hex(q, r),
                    });
            }
            else
            {
                isPlacementProbablyValid = IsMoveTargetValid(new Hex(q, r), organelleRot, MovingPlacedHex);
                effectiveSymmetry = HexEditorSymmetry.None;
            }

            // TODO: show the cell graphics that is about to be placed
            RunWithSymmetry(q, r, RenderHighlightedCell, effectiveSymmetry);
        }
    }

    public override bool CanFinishEditing(IEnumerable<EditorUserOverride> userOverrides)
    {
        var editorUserOverrides = userOverrides.ToList();
        if (!base.CanFinishEditing(editorUserOverrides))
            return false;

        if (editorUserOverrides.Contains(EditorUserOverride.NotProducingEnoughATP))
            return true;

        // TODO: warning about not producing enough ATP

        return true;
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

        var cost = CellTypeFromName(activeActionName).MPCost;

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

    protected override void LoadScenes()
    {
        base.LoadScenes();

        microbeScene = GD.Load<PackedScene>("res://src/microbe_stage/Microbe.tscn");
    }

    protected override void PerformActiveAction()
    {
        AddCell(CellTypeFromName(activeActionName ?? throw new InvalidOperationException("no action active")));
    }

    protected override bool DoesActionEndInProgressAction(CellEditorAction action)
    {
        throw new NotImplementedException();
    }

    protected override bool IsMoveTargetValid(Hex position, int rotation, CellTemplate hex)
    {
        throw new NotImplementedException();
    }

    protected override void OnMoveActionStarted()
    {
        throw new NotImplementedException();
    }

    protected override void PerformMove(int q, int r)
    {
        throw new NotImplementedException();
    }

    protected override CellTemplate? GetHexAt(Hex position)
    {
        return editedMicrobeOrganelles.GetElementAt(position)?.Data;
    }

    protected override void TryRemoveHexAt(Hex location)
    {
        var hexHere = editedMicrobeOrganelles.GetElementAt(location);
        if (hexHere == null)
            return;

        // Dont allow deletion of last cell
        if (editedMicrobeOrganelles.Count < 2)
            return;

        // If it was placed this session, just refund the cost of adding it.
        // TODO: this doesn't take being placed in current editor session into account, this is instead waiting for
        // the dynamic MP changes to be made
        int cost = Constants.ORGANELLE_REMOVE_COST;

        // TODO: move to using actions
        Editor.ChangeMutationPoints(-cost);
        editedMicrobeOrganelles.Remove(hexHere);

        /*
        var action = new CellEditorAction(Editor, cost,
            DoOrganelleRemoveAction, UndoOrganelleRemoveAction, new RemoveActionData(hexHere));

        EnqueueAction(action);*/
    }

    protected override float CalculateEditorArrowZPosition()
    {
        // The calculation falls back to 0 if there are no hexes found in the middle 3 rows
        var highestPointInMiddleRows = 0.0f;

        // Iterate through all hexes
        foreach (var hex in editedMicrobeOrganelles)
        {
            // Only consider the middle 3 rows
            if (hex.Position.Q is < -1 or > 1)
                continue;

            var cartesian = Hex.AxialToCartesian(hex.Position);

            // Get the min z-axis (highest point in the editor)
            highestPointInMiddleRows = Mathf.Min(highestPointInMiddleRows, cartesian.z);
        }

        return highestPointInMiddleRows - Constants.EDITOR_ARROW_OFFSET;
    }

    private void UpdateGUIAfterLoadingSpecies(Species species)
    {
        GD.Print("Starting early multicellular editor with: ", editedMicrobeOrganelles.Count,
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

    private bool TryAddHexToEditedLayout(HexWithData<CellTemplate> hex, int q, int r)
    {
        hex.Position = new Hex(q, r);
        if (editedMicrobeOrganelles.CanPlace(hex))
        {
            editedMicrobeOrganelles.Add(hex);

            return true;
        }

        return false;
    }

    private void RenderHighlightedCell(int q, int r, int rotation)
    {
        if (MovingPlacedHex == null && activeActionName == null)
            return;

        // For now a single hex represents entire cells
        RenderHoveredHex(q, r, new[] { new Hex(0, 0) }, isPlacementProbablyValid,
            out bool _);

        // TODO: to be placed cell visuals showing
        /*if (!string.IsNullOrEmpty(shownOrganelle.DisplayScene) && showModel)
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
        }*/
    }

    /// <summary>
    ///   Places an organelle of the specified type under the cursor and also applies symmetry to
    ///   place multiple at once.
    /// </summary>
    /// <returns>True when at least one organelle got placed</returns>
    private bool AddCell(CellType cellType)
    {
        GetMouseHex(out int q, out int r);

        bool placedSomething = false;

        RunWithSymmetry(q, r,
            (attemptQ, attemptR, rotation) =>
            {
                if (PlaceIfPossible(cellType, attemptQ, attemptR, rotation))
                    placedSomething = true;
            });

        return placedSomething;
    }

    /// <summary>
    ///   Helper for AddCell
    /// </summary>
    private bool PlaceIfPossible(CellType cellType, int q, int r, int rotation)
    {
        var cell = new HexWithData<CellTemplate>(new CellTemplate(cellType, new Hex(q, r), rotation))
        {
            Position = new Hex(q, r),
        };

        if (!IsValidPlacement(cell))
        {
            // Play Sound
            Editor.OnInvalidAction();
            return false;
        }

        if (AddCell(cell))
            return true;

        return false;
    }

    private bool IsValidPlacement(HexWithData<CellTemplate> cell)
    {
        return editedMicrobeOrganelles.CanPlaceAndIsTouching(cell);
    }

    private bool AddCell(HexWithData<CellTemplate> cell)
    {
        // TODO: editor actions
        Editor.ChangeMutationPoints(-cell.Data!.CellType.MPCost);
        editedMicrobeOrganelles.Add(cell);

        /*var action = new CellEditorAction(Editor, organelle.Definition.MPCost,
            DoOrganellePlaceAction, UndoOrganellePlaceAction, new PlacementActionData(organelle));

        EnqueueAction(action);*/

        return true;
    }

    private void SetupCellTypeSelections()
    {
        // TODO: generalize this method to allow creating / destroying buttons as cell types are added / removed

        foreach (var cellType in Editor.EditedSpecies.CellTypes)
        {
            var control = (CellTypeSelection)cellTypeSelectionButtonScene.Instance();
            control.PartName = cellType.TypeName;
            control.SelectionGroup = cellTypeButtonGroup;
            control.MPCost = cellType.MPCost;
            control.CellType = cellType;
            control.Name = cellType.TypeName;

            // TODO: tooltips for these

            cellTypeSelectionList.AddItem(control);
            cellTypeSelectionButtons.Add(cellType.TypeName, control);

            control.Connect(nameof(MicrobePartSelection.OnPartSelected), this, nameof(OnCellToPlaceSelected));
        }
    }

    private void OnCellToPlaceSelected(string cellTypeName)
    {
        if (!cellTypeSelectionButtons.TryGetValue(cellTypeName, out var cellTypeButton))
        {
            GD.PrintErr("Attempted to select an unknown cell type");
            return;
        }

        activeActionName = cellTypeName;

        // Update the icon highlightings
        foreach (var element in cellTypeSelectionButtons.Values)
        {
            element.Selected = element == cellTypeButton;
        }

        // TODO: handle the duplicate, delete, edit buttons for the cell type
    }

    private void OnOrganellesChanged()
    {
        UpdateAlreadyPlacedVisuals();

        UpdateArrow();
    }

    /// <summary>
    ///   This destroys and creates again entities to represent all the currently placed cells. Call this whenever
    ///   editedMicrobeOrganelles is changed.
    /// </summary>
    private void UpdateAlreadyPlacedVisuals()
    {
        var islands = editedMicrobeOrganelles.GetIslandHexes();

        // Build the entities to show the current microbe
        // TODO: implement placed this session flag
        UpdateAlreadyPlacedHexes(
            editedMicrobeOrganelles.Select(o => (o.Position, new[] { new Hex(0, 0) }.AsEnumerable(), false)), islands);
    }

    private void OnSpeciesNameChanged(string newText)
    {
        newName = newText;
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

        // Show selected
        switch (selectedSelectionMenuTab)
        {
            case SelectionMenuTab.Structure:
            {
                structureTab.Show();
                structureTabButton.Pressed = true;
                break;
            }

            case SelectionMenuTab.Reproduction:
            {
                reproductionTab.Show();
                reproductionTabButton.Pressed = true;
                break;
            }

            case SelectionMenuTab.Behaviour:
            {
                behaviourEditor.Show();
                behaviourTabButton.Pressed = true;
                break;
            }

            default:
                throw new Exception("Invalid selection menu tab");
        }
    }
}
