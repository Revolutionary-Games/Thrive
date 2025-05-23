﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Popup menu when right clicking a metaball in the editor
/// </summary>
public partial class MetaballPopupMenu : HexPopupMenu
{
    private List<MacroscopicMetaball>? selectedMetaballs;

    public override bool EnableModifyOption
    {
        get => true;
        set
        {
            if (value != true)
                throw new NotSupportedException();
        }
    }

    public List<MacroscopicMetaball> SelectedMetaballs
    {
        get => selectedMetaballs ??
            throw new InvalidOperationException($"{nameof(MetaballPopupMenu)} was not opened with cells set");
        set
        {
            selectedMetaballs = value;
            UpdateTitleLabel();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        // Skip things that use the organelle to work on if we aren't open (no selected organelle set)
        if (selectedMetaballs != null)
        {
            UpdateTitleLabel();
            UpdateDeleteButton();
            UpdateMoveButton();
        }
    }

    public IEnumerable<MacroscopicMetaball> GetSelectedThatAreStillValid(
        IReadOnlyCollection<MacroscopicMetaball> allValidMetaballs)
    {
        return SelectedMetaballs.Where(allValidMetaballs.Contains);
    }

    protected override void UpdateTitleLabel()
    {
        if (titleLabel == null)
            return;

        var names = SelectedMetaballs.Select(m => m.CellType.TypeName).Distinct()
            .ToList();

        if (names.Count == 1)
        {
            titleLabel.Text = names[0];
        }
        else
        {
            titleLabel.Text = Localization.Translate("MULTIPLE_METABALLS");
        }
    }

    protected override void UpdateDeleteButton()
    {
        if (deleteButton == null)
            return;

        var mpCost = GetActionPrice?.Invoke(SelectedMetaballs
                .Select(o =>
                    (EditorCombinableActionData)new MetaballRemoveActionData<MacroscopicMetaball>(o, null))) ??
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

        var mpCost = GetActionPrice?.Invoke(SelectedMetaballs.Select(o =>
            (EditorCombinableActionData)new MetaballMoveActionData<MacroscopicMetaball>(o, o.Position,
                o.Position + Vector3.One, o.Parent,
                o.Parent, null))) ?? throw new ArgumentException($"{nameof(GetActionPrice)} not set");

        mpCost = Math.Round(mpCost, Constants.MUTATION_POINTS_DECIMALS);

        if (mpCost != 0)
            mpCost = -mpCost;

        var mpLabel = moveButton.GetNode<Label>("MarginContainer/HBoxContainer/MpCost");

        mpLabel.Text = new LocalizedString("MP_COST", mpCost).ToString();

        moveButton.Disabled = !EnableMoveOption;
    }
}
