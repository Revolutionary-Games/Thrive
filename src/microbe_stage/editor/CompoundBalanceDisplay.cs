using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Shows Compound balance information
/// </summary>
public partial class CompoundBalanceDisplay : VBoxContainer
{
#pragma warning disable CA2213
    [Export]
    private VBoxContainer compoundListContainer = null!;

    [Export]
    private OptionButton modeSelector = null!;
#pragma warning restore CA2213

    private ChildObjectCache<Compound, CompoundAmount> childCache = null!;

    private BalanceDisplayType currentDisplayType = BalanceDisplayType.EnergyEquilibrium;

    [Signal]
    public delegate void BalanceTypeChangedEventHandler(BalanceDisplayType newType);

    public BalanceDisplayType CurrentDisplayType
    {
        get => currentDisplayType;
        set
        {
            if (currentDisplayType == value)
                return;

            currentDisplayType = value;
            EmitSignal(SignalName.BalanceTypeChanged, Variant.From(currentDisplayType));
        }
    }

    public override void _Ready()
    {
        childCache = new ChildObjectCache<Compound, CompoundAmount>(compoundListContainer,
            c => new CompoundAmount { Compound = c, PrefixPositiveWithPlus = true });
    }

    public void UpdateBalances(Dictionary<Compound, CompoundBalance> balances, float dayLengthWarningThreshold)
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

            if (entry.Value.FillTime > 0)
            {
                UpdateExtraValueDescription(compoundControl, entry.Value, dayLengthWarningThreshold);
            }
            else
            {
                compoundControl.ExtraValueDescription = null;
            }
        }

        childCache.DeleteUnmarked();
        childCache.ApplyOrder();
    }

    private static void UpdateExtraValueDescription(CompoundAmount compoundControl, CompoundBalance balance,
        float dayLengthWarningThreshold)
    {
        if (compoundControl.ExtraValueDescription == null)
        {
            SetNewExtraDescription(compoundControl, balance, dayLengthWarningThreshold);
        }
        else
        {
            // For object allocation efficiency reuse the existing string but just update the value
            var updateTarget = compoundControl.ExtraValueDescription;

            if (balance.FillTime > dayLengthWarningThreshold)
            {
                if (updateTarget.TranslationKey != "COMPOUND_BALANCE_FILL_TIME_TOO_LONG")
                {
                    // Need to swap base key
                    SetNewExtraDescription(compoundControl, balance, dayLengthWarningThreshold);
                    return;
                }

                // Can just update the values to avoid one list allocation
                // TODO: test if this is any kind of improvement as there needs to be boxing of the primitive values
                // anyway

                updateTarget.UpdateFormatArgs(Math.Round(balance.FillTime), Math.Round(dayLengthWarningThreshold));
            }
            else
            {
                if (updateTarget.TranslationKey != "COMPOUND_BALANCE_FILL_TIME")
                {
                    // Need to swap base key
                    SetNewExtraDescription(compoundControl, balance, dayLengthWarningThreshold);
                    return;
                }

                updateTarget.UpdateFormatArgs(Math.Round(balance.FillTime, 1));
            }

            compoundControl.OnExtraTextChangedExternally();
        }
    }

    private static void SetNewExtraDescription(CompoundAmount compoundControl, CompoundBalance balance,
        float dayLengthWarningThreshold)
    {
        if (balance.FillTime > dayLengthWarningThreshold)
        {
            compoundControl.ExtraValueDescription = new LocalizedString("COMPOUND_BALANCE_FILL_TIME_TOO_LONG",
                Math.Round(balance.FillTime), Math.Round(dayLengthWarningThreshold));
        }
        else
        {
            compoundControl.ExtraValueDescription =
                new LocalizedString("COMPOUND_BALANCE_FILL_TIME", Math.Round(balance.FillTime, 1));
        }
    }

    private void OnBalanceModeChanged(int index)
    {
        CurrentDisplayType = (BalanceDisplayType)modeSelector.GetItemId(index);
    }
}
