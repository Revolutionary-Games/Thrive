using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Right click popup menu that opens when clicked on a body plan cell
/// </summary>
public partial class CellPopupMenu : HexPopupMenu
{
    private List<HexWithData<CellTemplate>>? selectedCells;

#pragma warning disable CA2213
    [Export]
    private CheckBox cavityCheckBox = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void CavityToggledEventHandler(bool isCavity);

    public override bool EnableModifyOption
    {
        get => true;
        set
        {
            if (!value)
                throw new NotSupportedException();
        }
    }

    public List<HexWithData<CellTemplate>> SelectedCells
    {
        get => selectedCells ??
            throw new InvalidOperationException($"{nameof(CellPopupMenu)} was not opened with cells set");
        set
        {
            selectedCells = value;
            UpdateTitleLabel();
            UpdateCavityCheckBox();
        }
    }

    public override void _Ready()
    {
        base._Ready();
        UpdateCavityCheckBox();

        // Skip things that use the organelle to work on if we aren't open (no selected organelle set)
        if (selectedCells != null)
        {
            UpdateTitleLabel();
            UpdateDeleteButton();
            UpdateMoveButton();
        }
    }

    public IEnumerable<HexWithData<CellTemplate>> GetSelectedThatAreStillValid(
        IReadOnlyCollection<HexWithData<CellTemplate>> allValidCells)
    {
        return SelectedCells.Where(allValidCells.Contains);
    }

    protected override void UpdateTitleLabel()
    {
        if (titleLabel == null)
            return;

        var names = SelectedCells.Select(c => c.Data!.ModifiableCellType.CellTypeName).Distinct()
            .ToList();

        if (names.Count == 1)
        {
            titleLabel.Text = names[0];
        }
        else
        {
            titleLabel.Text = Localization.Translate("MULTIPLE_CELLS");
        }
    }

    protected override void UpdateDeleteButton()
    {
        if (deleteButton == null)
            return;

        if (!ShowDeleteOption)
        {
            deleteButton.Visible = false;
            return;
        }

        deleteButton.Visible = true;

        var mpCost = GetActionPrice?.Invoke(SelectedCells
                .Select(o =>
                    (EditorCombinableActionData)new CellRemoveActionData(o))) ??
            throw new ArgumentException($"{nameof(GetActionPrice)} not set");

        mpCost = Math.Round(mpCost, Constants.MUTATION_POINTS_DECIMALS);

        var mpLabel = deleteButton.GetNode<Label>("MarginContainer/HBoxContainer/MpCost");

        mpLabel.Text = new LocalizedString("MP_COST", -mpCost).ToString();

        deleteButton.Disabled = !EnableDeleteOption;
    }

    protected override void UpdateMoveButton()
    {
        if (moveButton == null)
            return;

        var mpCost = GetActionPrice?.Invoke(SelectedCells.Select(o =>
            (EditorCombinableActionData)new CellMoveActionData(o, o.Position, o.Position + new Hex(5, 5), o.Orientation,
                0))) ?? throw new ArgumentException($"{nameof(GetActionPrice)} not set");

        mpCost = Math.Round(mpCost, Constants.MUTATION_POINTS_DECIMALS);

        if (mpCost != 0)
            mpCost = -mpCost;

        var mpLabel = moveButton.GetNode<Label>("MarginContainer/HBoxContainer/MpCost");

        mpLabel.Text = new LocalizedString("MP_COST", mpCost).ToString();

        moveButton.Disabled = !EnableMoveOption;
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        cavityCheckBox.GrabFocus();
    }

    private void UpdateCavityCheckBox()
    {
        cavityCheckBox.SetPressedNoSignal(selectedCells?.Count > 0 && selectedCells.All(c => c.Data!.IsCavity));
    }

    private void OnCavityToggled(bool isCavity)
    {
        ShowPopup = false;
        EmitSignal(SignalName.CavityToggled, isCavity);
    }
}
