using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Displays a list of cell types that the player can choose from. Emits a signal when the choice is done.
/// </summary>
public partial class CellTypePickerPopup : CustomWindow
{
#pragma warning disable CA2213
    [Export]
    private Control cellTypeList = null!;

    [Export]
    private PackedScene cellTypeButton = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void OnCellTypePickedEventHandler(string name);

    public void UpdateCellTypeList(List<CellType> types)
    {
        cellTypeList.QueueFreeChildren();

        var buttonGroup = new ButtonGroup();

        foreach (var cellType in types)
        {
            var button = cellTypeButton.Instantiate<CellTypeSelection>();
            cellTypeList.AddChild(button);

            button.SelectionGroup = buttonGroup;
            button.PartName = cellType.CellTypeName;
            button.CellType = cellType;
            button.Name = cellType.CellTypeName;

            button.ShowMPIcon = false;
            button.MPCost = Constants.SPORE_CELL_TYPE_CHANGE_COST;

            button.Connect(CellTypeSelection.SignalName.OnPartSelected,
                new Callable(this, MethodName.OnCellTypeButtonClicked));
        }
    }

    private void OnCellTypeButtonClicked(string name)
    {
        EmitSignal(SignalName.OnCellTypePicked, name);
        Close();
    }
}
