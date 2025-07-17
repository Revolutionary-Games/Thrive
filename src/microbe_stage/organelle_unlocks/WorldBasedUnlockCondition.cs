namespace UnlockConstraints;

using System;
using System.Globalization;
using Newtonsoft.Json;
using ThriveScriptsShared;

/// <summary>
///   An unlock condition that relies on world data to track progress
/// </summary>
public abstract class WorldBasedUnlockCondition : IUnlockCondition
{
    public abstract bool Satisfied(IUnlockStateDataSource data);

    public abstract float Progress(IUnlockStateDataSource data);

    public abstract void GenerateTooltip(LocalizedStringBuilder builder, IUnlockStateDataSource data);

    public virtual void Resolve(SimulationParameters parameters)
    {
    }

    public virtual void Check(string name)
    {
    }
}

/// <summary>
///   The player species produces more than a specified amount of ATP.
/// </summary>
public class AtpProductionAbove : WorldBasedUnlockCondition
{
    [JsonProperty]
    public float Atp;

    public override bool Satisfied(IUnlockStateDataSource data)
    {
        return AtpProduction(data) >= Atp;
    }

    public override float Progress(IUnlockStateDataSource data)
    {
        return Math.Clamp(AtpProduction(data) ?? 0 / Atp, 0, 1);
    }

    public override void GenerateTooltip(LocalizedStringBuilder builder, IUnlockStateDataSource data)
    {
        builder.Append(new LocalizedString("UNLOCK_CONDITION_ATP_PRODUCTION_ABOVE", Atp));
    }

    private float? AtpProduction(IUnlockStateDataSource data)
    {
        if (data is not WorldAndPlayerDataSource worldArgs)
            return null;

        return worldArgs.EnergyBalance?.TotalProduction;
    }
}

/// <summary>
///   The player species produces a specified amount more ATP than it uses.
/// </summary>
public class ExcessAtpAbove : WorldBasedUnlockCondition
{
    [JsonProperty]
    public float Atp;

    public override bool Satisfied(IUnlockStateDataSource data)
    {
        return FinalBalance(data) >= Atp;
    }

    public override float Progress(IUnlockStateDataSource data)
    {
        return Math.Clamp(FinalBalance(data) ?? 0 / Atp, 0, 1);
    }

    public override void GenerateTooltip(LocalizedStringBuilder builder, IUnlockStateDataSource data)
    {
        builder.Append(new LocalizedString("UNLOCK_CONDITION_EXCESS_ATP_ABOVE", Atp));
    }

    private float? FinalBalance(IUnlockStateDataSource data)
    {
        if (data is not WorldAndPlayerDataSource worldArgs)
            return null;

        return worldArgs.EnergyBalance?.FinalBalance;
    }
}

/// <summary>
///   The player species has speed that is slower than a certain speed.
/// </summary>
public class SpeedBelow : WorldBasedUnlockCondition
{
    [JsonProperty]
    public float Threshold;

    public override bool Satisfied(IUnlockStateDataSource data)
    {
        return GetPlayerSpeed(data) < Threshold;
    }

    public override float Progress(IUnlockStateDataSource data)
    {
        var overThreashold = Threshold - (GetPlayerSpeed(data) ?? 0);
        return Math.Clamp(overThreashold / Threshold, 0, 1);
    }

    public override void GenerateTooltip(LocalizedStringBuilder builder, IUnlockStateDataSource data)
    {
        builder.Append(new LocalizedString("UNLOCK_CONDITION_SPEED_BELOW", Threshold));
    }

    private float? GetPlayerSpeed(IUnlockStateDataSource data)
    {
        if (data is not WorldAndPlayerDataSource worldArgs)
            return null;

        var playerData = worldArgs.PlayerData;

        if (playerData == null)
            return null;

        var rawSpeed = MicrobeInternalCalculations.CalculateSpeed(playerData.Organelles.Organelles,
            playerData.MembraneType,
            playerData.MembraneRigidity,
            playerData.IsBacteria);

        // This needs to be user readable as it is shown by the tooltip
        return (float)Math.Round(MicrobeInternalCalculations.SpeedToUserReadableNumber(rawSpeed), 1);
    }
}

/// <summary>
///   The player's current patch has a certain amount of certain compound.
/// </summary>
public class PatchCompound : WorldBasedUnlockCondition
{
    [JsonProperty]
    public int? Min;

    [JsonProperty]
    public int? Max;

    [JsonProperty]
    public Compound Compound;

    public override bool Satisfied(IUnlockStateDataSource data)
    {
        if (data is not WorldAndPlayerDataSource worldArgs)
            return false;

        // TODO: is it correct that this uses display adjusted values?
        var current = worldArgs.CurrentPatch.GetCompoundAmountForDisplay(Compound, CompoundAmountType.Biome);

        var minSatisfied = !Min.HasValue || current >= Min;
        var maxSatisfied = !Max.HasValue || current <= Max;
        return minSatisfied && maxSatisfied;
    }

    public override float Progress(IUnlockStateDataSource data)
    {
        return Satisfied(data) ? 1 : 0;
    }

    public override void GenerateTooltip(LocalizedStringBuilder builder, IUnlockStateDataSource data)
    {
        var compoundDefinition = SimulationParameters.GetCompound(Compound);

        // These are objects to allow LocalizedString and pure string parameters used in the final localized string
        // which anyway takes plain objects as the format parameters
        object? formattedMin;
        object? formattedMax;

        switch (Compound)
        {
            case Compound.Sunlight:
                formattedMin = Min.HasValue ?
                    new LocalizedString("VALUE_WITH_UNIT", new LocalizedString("PERCENTAGE_VALUE", Min), "lx") :
                    null;
                formattedMax = Max.HasValue ?
                    new LocalizedString("VALUE_WITH_UNIT", new LocalizedString("PERCENTAGE_VALUE", Max), "lx") :
                    null;
                break;
            case Compound.Temperature:
                // TODO: automatically handle any compounds with unit set?
                var unit = compoundDefinition.Unit ?? "MISSING CONFIGURED UNIT";
                formattedMin = Min.HasValue ? new LocalizedString("VALUE_WITH_UNIT", Min / 100, unit) : null;
                formattedMax = Max.HasValue ? new LocalizedString("VALUE_WITH_UNIT", Max / 100, unit) : null;
                break;
            case Compound.Oxygen:
            case Compound.Carbondioxide:
            case Compound.Nitrogen:
                formattedMin = Min.HasValue ? new LocalizedString("PERCENTAGE_VALUE", Min) : null;
                formattedMax = Max.HasValue ? new LocalizedString("PERCENTAGE_VALUE", Max) : null;
                break;
            default:
                formattedMin = Min?.ToString(CultureInfo.CurrentCulture);
                formattedMax = Max?.ToString(CultureInfo.CurrentCulture);
                break;
        }

        var compoundName = compoundDefinition.InternalName;

        if (formattedMin != null && formattedMax != null)
        {
            builder.Append(new LocalizedString("UNLOCK_CONDITION_COMPOUND_IS_BETWEEN", compoundName, formattedMin,
                formattedMax));
        }
        else if (formattedMin != null)
        {
            builder.Append(new LocalizedString("UNLOCK_CONDITION_COMPOUND_IS_ABOVE", compoundName, formattedMin));
        }
        else if (formattedMax != null)
        {
            builder.Append(new LocalizedString("UNLOCK_CONDITION_COMPOUND_IS_BELOW", compoundName, formattedMax));
        }
    }

    public override void Check(string name)
    {
        if (Compound == Compound.Invalid)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Compound is not set");
        }

        if (Min == null && Max == null)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Min and Max are both null");
        }
    }
}
