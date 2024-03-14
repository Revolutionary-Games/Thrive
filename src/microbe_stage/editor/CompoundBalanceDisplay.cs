using System.Collections.Generic;
using Godot;

/// <summary>
///   Shows Compound balance information
/// </summary>
public partial class CompoundBalanceDisplay : VBoxContainer
{
    [Export]
    public NodePath? CompoundListContainerPath;

#pragma warning disable CA2213
    private VBoxContainer compoundListContainer = null!;
#pragma warning restore CA2213

    private ChildObjectCache<Compound, CompoundAmount> childCache = null!;

    public override void _Ready()
    {
        compoundListContainer = GetNode<VBoxContainer>(CompoundListContainerPath);

        childCache = new ChildObjectCache<Compound, CompoundAmount>(compoundListContainer,
            c => new CompoundAmount { Compound = c, PrefixPositiveWithPlus = true });
    }

    public void UpdateBalances(Dictionary<Compound, CompoundBalance> balances)
    {
        childCache.UnMarkAll();

        foreach (var entry in balances)
        {
            var compoundControl = childCache.GetChild(entry.Key);
            var amount = entry.Value.Balance;
            compoundControl.Amount = amount;

            compoundControl.ValueColour = amount < 0 ?
                CompoundAmount.Colour.Red :
                CompoundAmount.Colour.White;
        }

        childCache.DeleteUnmarked();
        childCache.ApplyOrder();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CompoundListContainerPath?.Dispose();
        }

        base.Dispose(disposing);
    }
}
