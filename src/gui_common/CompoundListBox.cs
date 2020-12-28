using System.Collections.Generic;
using Godot;

/// <summary>
///   Shows a list of Compounds with their amounts
/// </summary>
public class CompoundListBox : HBoxContainer
{
    private readonly ChildObjectCache<Compound, CompoundAmount> compoundAmountControls;

    public CompoundListBox()
    {
        compoundAmountControls = new ChildObjectCache<Compound, CompoundAmount>(this, CreateChild);
    }

    /// <summary>
    ///   The separator between the parts. Needs to be set before UpdateCompounds is called
    ///   as changing this won't recreate already created children.
    /// </summary>
    public string PartSeparator { get; set; } = " + ";

    /// <summary>
    ///   If true positive (>= 0) numbers are prefixed with a plus.
    ///   Needs to be set before UpdateCompounds is called
    /// </summary>
    public bool PrefixPositiveWithPlus { get; set; }

    /// <summary>
    ///   If true  numbers are shown as percentages.
    ///   Needs to be set before UpdateCompounds is called
    /// </summary>
    public bool UsePercentageDisplay { get; set; }

    public void UpdateCompounds(IEnumerable<KeyValuePair<Compound, float>> compounds)
    {
        compoundAmountControls.UnMarkAll();

        foreach (var entry in compounds)
        {
            compoundAmountControls.GetChild(entry.Key).Amount = entry.Value;
        }

        compoundAmountControls.DeleteUnmarked();
    }

    private CompoundAmount CreateChild(Compound forCompound)
    {
        // A bit of a hacky way to detect if we need to add a separator between children
        if (GetChildCount() > 0)
            AddSeparator();

        var compoundDisplay = new CompoundAmount
        {
            Compound = forCompound,
            PrefixPositiveWithPlus = PrefixPositiveWithPlus,
            UsePercentageDisplay = UsePercentageDisplay,
        };
        return compoundDisplay;
    }

    private void AddSeparator()
    {
        var label = new Label { Text = " + " };
        AddChild(label);
    }
}
