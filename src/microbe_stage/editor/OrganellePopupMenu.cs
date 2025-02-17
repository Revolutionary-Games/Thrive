using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Manages a custom context menu solely for showing list of options for a placed organelle in the microbe editor.
/// </summary>
public partial class OrganellePopupMenu : HexPopupMenu
{
    private List<OrganelleTemplate>? selectedOrganelles;

    /// <summary>
    ///   The placed organelles to be shown options of. The main organelle is at index 0, if there are multiple
    ///   selected with symmetry. Note that most uses should use <see cref="GetSelectedThatAreStillValid"/> instead.
    /// </summary>
    public List<OrganelleTemplate> SelectedOrganelles
    {
        get => selectedOrganelles ??
            throw new InvalidOperationException("OrganellePopup was not opened with organelle set");
        set
        {
            selectedOrganelles = value;
            UpdateTitleLabel();
        }
    }

    public float CostMultiplier { get; set; } = 1.0f;

    public override void _Ready()
    {
        base._Ready();

        // Skip things that use the organelle to work on if we aren't open (no selected organelle set)
        if (selectedOrganelles != null)
        {
            UpdateTitleLabel();
            UpdateDeleteButton();
            UpdateMoveButton();
        }
    }

    /// <summary>
    ///   Filters the list of organelles this is used on to ones that are still within the list of valid ones to
    ///   prevent removed organelles from being used.
    /// </summary>
    /// <param name="allValidOrganelles">Collection of all valid organelles</param>
    /// <returns>Organelles filtered to just valid ones from <see cref="SelectedOrganelles"/></returns>
    public IEnumerable<OrganelleTemplate> GetSelectedThatAreStillValid(
        IReadOnlyCollection<OrganelleTemplate> allValidOrganelles)
    {
        return SelectedOrganelles.Where(allValidOrganelles.Contains);
    }

    protected override void UpdateTitleLabel()
    {
        if (titleLabel == null)
            return;

        var names = SelectedOrganelles.Select(p => p.Definition.Name).Distinct()
            .ToList();

        if (names.Count == 1)
        {
            titleLabel.Text = names[0];
        }
        else
        {
            titleLabel.Text = Localization.Translate("MULTIPLE_ORGANELLES");
        }
    }

    protected override void UpdateDeleteButton()
    {
        if (deleteButton == null)
            return;

        var mpCost = GetActionPrice?.Invoke(SelectedOrganelles
                .Select(o => (EditorCombinableActionData)new OrganelleRemoveActionData(o)
                {
                    CostMultiplier = CostMultiplier,
                })) ??
            throw new ArgumentException($"{nameof(GetActionPrice)} not set");

        mpCost = Math.Round(mpCost, Constants.MUTATION_POINTS_DECIMALS);

        var mpLabel = deleteButton.GetNode<Label>("MarginContainer/HBoxContainer/MpCost");

        mpLabel.Text = new LocalizedString("MP_COST", -mpCost).ToString();

        deleteButton.Disabled = !EnableDeleteOption;
        deleteButton.TooltipText = DeleteOptionTooltip;
    }

    protected override void UpdateMoveButton()
    {
        if (moveButton == null)
            return;

        var mpCost = GetActionPrice?.Invoke(SelectedOrganelles.Select(o =>
            (EditorCombinableActionData)new OrganelleMoveActionData(o, o.Position, o.Position + new Hex(5, 5),
                o.Orientation, o.Orientation)
            {
                CostMultiplier = CostMultiplier,
            })) ?? throw new ArgumentException($"{nameof(GetActionPrice)} not set");

        mpCost = Math.Round(mpCost, Constants.MUTATION_POINTS_DECIMALS);

        if (mpCost != 0)
            mpCost = -mpCost;

        var mpLabel = moveButton.GetNode<Label>("MarginContainer/HBoxContainer/MpCost");

        mpLabel.Text = new LocalizedString("MP_COST", mpCost).ToString();

        moveButton.Disabled = !EnableMoveOption;
    }
}
