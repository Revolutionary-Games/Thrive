using System;
using System.Collections.Generic;
using System.Text;
using Godot;

/// <summary>
///   Shows storage compound information (how long storage lasts) and calculates if the player can survive the night
///   with the storage they have and compound production.
/// </summary>
public partial class CompoundStorageStatistics : VBoxContainer
{
    private readonly StringBuilder storageWarningBuilder = new();

#pragma warning disable CA2213
    [Export]
    private VBoxContainer compoundListContainer = null!;
#pragma warning restore CA2213

    private ChildObjectCache<Compound, CompoundAmount> childCache = null!;

    private string valueSuffix = Localization.Translate("STORAGE_STATISTICS_SECONDS_OF_COMPOUND");

    public override void _Ready()
    {
        childCache = new ChildObjectCache<Compound, CompoundAmount>(compoundListContainer,
            c => new CompoundAmount { Compound = c, PrefixPositiveWithPlus = false, AmountSuffix = valueSuffix });
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Localization.Instance.OnTranslationsChanged += UpdateLabelTextFormat;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Localization.Instance.OnTranslationsChanged -= UpdateLabelTextFormat;
    }

    public void UpdateStorage(Dictionary<Compound, CompoundBalance> daytimeBalances,
        Dictionary<Compound, CompoundBalance> nightBalance, float nominalStorage,
        Dictionary<Compound, float> specialCapacities, float nightDuration, float fillTimeWarning,
        CustomRichTextLabel notEnoughStorageWarning)
    {
        childCache.UnMarkAll();

        storageWarningBuilder.Clear();

        foreach (var entry in daytimeBalances)
        {
            var storage = specialCapacities.GetValueOrDefault(entry.Key, nominalStorage);

            if (nightBalance.TryGetValue(entry.Key, out var nightEntry) && nightEntry.Balance < 0)
            {
                var compoundControl = childCache.GetChild(entry.Key);

                // The balance is negated here to get a positive result for how long the storage lasts
                var lastingTime = storage / -nightEntry.Balance;

                compoundControl.Amount = lastingTime;

                // Provide info on what to do with resources that are generated during the day. For other resource
                // types it is not very useful to know if it lasts the night
                // TODO: if we ever have stuff like compound clouds or cell spawns that are less during the night then
                // this could make sense to enable for other compound types as well.
                if (entry.Value.Balance > 0)
                {
                    if (lastingTime < nightDuration)
                    {
                        UpdateExtraValueDescription(compoundControl, nightDuration);
                        compoundControl.ValueColour = CompoundAmount.Colour.Red;

                        storageWarningBuilder.Append(new LocalizedString("COMPOUND_STORAGE_NOT_ENOUGH_SPACE",
                            entry.Key.InternalName, Math.Round(lastingTime, 1),
                            Math.Round(nightDuration)));
                        storageWarningBuilder.Append(' ');
                    }
                    else
                    {
                        compoundControl.ExtraValueDescription = null;
                        compoundControl.ValueColour = CompoundAmount.Colour.White;
                    }

                    // Check and give warnings if not enough compound can be generated during the day to survive
                    // the night
                    if (!MicrobeInternalCalculations.CanGenerateEnoughCompoundToSurviveNight(nightEntry.Balance,
                            entry.Value.Balance, nightDuration, fillTimeWarning, out var generated, out var required))
                    {
                        storageWarningBuilder.Append(new LocalizedString(
                            "COMPOUND_STORAGE_NOT_ENOUGH_GENERATED_DURING_DAY", entry.Key.InternalName,
                            Math.Round(generated, 1),
                            Math.Round(required, 1)));
                        storageWarningBuilder.Append(' ');
                    }
                }
                else
                {
                    compoundControl.ValueColour = CompoundAmount.Colour.White;
                }
            }
            else
            {
                // Compound that doesn't change during the night (or not negative during the night), show still a
                // depletion time if valid (balance is negative)
                // It should be the case that not many compounds would ever fall into this category but for
                // completeness this is handled as well

                if (entry.Value.Balance < 0)
                {
                    var compoundControl = childCache.GetChild(entry.Key);

                    var lastingTime = storage / -entry.Value.Balance;
                    compoundControl.Amount = lastingTime;

                    compoundControl.ExtraValueDescription = null;
                    compoundControl.ValueColour = CompoundAmount.Colour.White;
                }
            }
        }

        childCache.DeleteUnmarked();
        childCache.ApplyOrder();

        if (storageWarningBuilder.Length > 0)
        {
            notEnoughStorageWarning.ExtendedBbcode = storageWarningBuilder.ToString();
            notEnoughStorageWarning.Visible = true;
        }
        else
        {
            notEnoughStorageWarning.Visible = false;
        }
    }

    private static void UpdateExtraValueDescription(CompoundAmount compoundControl, float nightLength)
    {
        // See CompoundBalanceDisplay.UpdateExtraValueDescription for why this method is written like this

        if (compoundControl.ExtraValueDescription == null)
        {
            SetNewExtraDescription(compoundControl, nightLength);
        }
        else
        {
            // For object allocation efficiency reuse the existing string but just update the value
            var updateTarget = compoundControl.ExtraValueDescription;

            if (updateTarget.TranslationKey != "COMPOUND_STORAGE_AMOUNT_DOES_NOT_LAST_NIGHT")
            {
                // Need to swap base key
                SetNewExtraDescription(compoundControl, nightLength);
                return;
            }

            updateTarget.UpdateFormatArgs(Math.Round(nightLength));

            compoundControl.OnExtraTextChangedExternally();
        }
    }

    private static void SetNewExtraDescription(CompoundAmount compoundControl, float nightLength)
    {
        compoundControl.ExtraValueDescription =
            new LocalizedString("COMPOUND_STORAGE_AMOUNT_DOES_NOT_LAST_NIGHT", Math.Round(nightLength));
    }

    private void UpdateLabelTextFormat()
    {
        valueSuffix = Localization.Translate("STORAGE_STATISTICS_SECONDS_OF_COMPOUND");

        foreach (var compoundAmount in childCache.GetChildren())
        {
            compoundAmount.AmountSuffix = valueSuffix;
        }
    }
}
