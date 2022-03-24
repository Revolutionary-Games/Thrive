using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Body plan editor component for making body plans from hexes (that represent cells)
/// </summary>
[SceneLoadedClass("res://src/early_multicellular_stage/editor/CellBodyPlanEditorComponent.tscn")]
public class CellBodyPlanEditorComponent :
    HexEditorComponentBase<EarlyMulticellularEditor, CellEditorAction, CellTemplate>,
    IGodotEarlyNodeResolve
{
    [Export]
    public NodePath CellTypeSelectionListPath = null!;

    private readonly Dictionary<string, CellTypeSelection> cellTypeSelectionButtons = new();

    private CollapsibleList cellTypeSelectionList = null!;

    private PackedScene cellTypeSelectionButtonScene = null!;

    private ButtonGroup cellTypeButtonGroup = new();

    [JsonProperty]
    private CellType? activeActionCell;

    public override bool HasIslands { get; }

    public bool NodeReferencesResolved { get; }

    protected override bool ForceHideHover { get; }

    public override void _Ready()
    {
        base._Ready();

        cellTypeSelectionList = GetNode<CollapsibleList>(CellTypeSelectionListPath);

        cellTypeSelectionButtonScene =
            GD.Load<PackedScene>("res://src/early_multicellular_stage/editor/CellTypeSelection.tscn");
    }

    public override void OnEditorSpeciesSetup(Species species)
    {
        SetupCellTypeSelections();
    }

    public override void OnFinishEditing()
    {
        throw new NotImplementedException();
    }

    protected override void OnTranslationsChanged()
    {
    }

    protected override int CalculateCurrentActionCost()
    {
        throw new NotImplementedException();
    }

    protected override void PerformActiveAction()
    {
        throw new NotImplementedException();
    }

    protected override bool DoesActionEndInProgressAction(CellEditorAction action)
    {
        throw new NotImplementedException();
    }

    protected override bool IsMoveTargetValid(Hex position, int rotation, CellTemplate hex)
    {
        throw new NotImplementedException();
    }

    protected override void OnCurrentActionCanceled()
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
        throw new NotImplementedException();
    }

    protected override void TryRemoveHexAt(Hex location)
    {
        throw new NotImplementedException();
    }

    protected override void UpdateCancelState()
    {
        throw new NotImplementedException();
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

        activeActionCell = cellTypeButton.CellType;

        // Update the icon highlightings
        foreach (var element in cellTypeSelectionButtons.Values)
        {
            element.Selected = element == cellTypeButton;
        }

        // TODO: handle the duplicate, delete, edit buttons for the cell type
    }
}
