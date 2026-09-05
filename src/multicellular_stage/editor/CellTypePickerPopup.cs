using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Displays a list of cell types that the player can choose from. Emits a signal when the choice is done.
/// </summary>
public partial class CellTypePickerPopup : CustomWindow
{
    private Action<string, int>? onChosenCellTypeCallback;

    private SpecialCellArchetype cellArchetype;

#pragma warning disable CA2213
    [Export]
    private Control cellTypeList = null!;

    [Export]
    private PackedScene cellTypeButton = null!;
#pragma warning restore CA2213

    public void UpdateCellTypeList(List<CellType> types, Func<CellType, CellType> getUpdatedCellType,
        Action<string, int> onChosenCellType, SpecialCellArchetype specialCellArchetype)
    {
        onChosenCellTypeCallback = onChosenCellType;
        cellArchetype = specialCellArchetype;

        cellTypeList.QueueFreeChildren();

        var buttonGroup = new ButtonGroup();

        foreach (var cellType in types)
        {
            var updatedCellType = getUpdatedCellType(cellType);

            var button = cellTypeButton.Instantiate<CellTypeSelection>();
            cellTypeList.AddChild(button);

            button.SelectionGroup = buttonGroup;
            button.PartName = updatedCellType.CellTypeName;
            button.CellType = updatedCellType;
            button.Name = updatedCellType.CellTypeName;
            button.ShowInsufficientATPWarning = false;

            button.MPCost = 0.0f;

            button.Connect(MicrobePartSelection.SignalName.OnPartSelected,
                new Callable(this, MethodName.OnCellTypeButtonClicked));
        }
    }

    private void OnCellTypeButtonClicked(string name)
    {
        onChosenCellTypeCallback?.Invoke(name, (int)cellArchetype);
        Close();
    }
}
