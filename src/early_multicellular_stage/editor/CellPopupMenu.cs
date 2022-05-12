using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class CellPopupMenu : HexPopupMenu
{
    private List<CellTemplate>? selectedCells;

    public override bool EnableModifyOption
    {
        get => true;
        set
        {
            if (value != true)
                throw new NotSupportedException();
        }
    }

    public List<CellTemplate> SelectedCells
    {
        get => selectedCells ??
            throw new InvalidOperationException($"{nameof(CellPopupMenu)} was not opened with cells set");
        set
        {
            selectedCells = value;
            UpdateTitleLabel();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        // Skip things that use the organelle to work on if we aren't open (no selected organelle set)
        if (selectedCells != null)
        {
            UpdateTitleLabel();
            UpdateDeleteButton();
            UpdateMoveButton();
        }
    }

    protected override void UpdateTitleLabel()
    {
        if (titleLabel == null)
            return;

        var names = SelectedCells.Select(c => c.CellType.TypeName).Distinct()
            .ToList();

        if (names.Count == 1)
        {
            titleLabel.Text = names[0];
        }
        else
        {
            titleLabel.Text = TranslationServer.Translate("MULTIPLE_CELLS");
        }
    }

    protected override void UpdateDeleteButton()
    {
        if (deleteButton == null)
            return;

        var mpCost = GetActionPrice?.Invoke(
                SelectedCells
                    .Select(o =>
                        (EditorCombinableActionData)new CellRemoveActionData(new HexWithData<CellTemplate>(o)
                            { Position = o.Position }))) ??
            throw new ArgumentException($"{nameof(GetActionPrice)} not set");

        var mpLabel = deleteButton.GetNode<Label>("MarginContainer/HBoxContainer/MpCost");
        mpCost = (int)(mpCost * editorCostFactor);

        mpLabel.Text = new LocalizedString("MP_COST", -mpCost).ToString();

        deleteButton.Disabled = !EnableDeleteOption;
    }

    protected override void UpdateMoveButton()
    {
        if (moveButton == null)
            return;

        var mpCost = GetActionPrice?.Invoke(SelectedCells.Select(o =>
            (EditorCombinableActionData)new CellMoveActionData(new HexWithData<CellTemplate>(o)
                    { Position = o.Position }, o.Position, o.Position, o.Orientation,
                o.Orientation))) ?? throw new ArgumentException($"{nameof(GetActionPrice)} not set");

        var mpLabel = moveButton.GetNode<Label>("MarginContainer/HBoxContainer/MpCost");
        mpCost = (int)(mpCost * editorCostFactor);

        mpLabel.Text = new LocalizedString("MP_COST", mpCost).ToString();

        moveButton.Disabled = !EnableMoveOption;
    }
}
