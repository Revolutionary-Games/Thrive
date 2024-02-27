namespace UnlockConstraints
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    ///   An unlock condition that relies on world data to track progress
    /// </summary>
    public abstract class WorldBasedUnlockCondition : IUnlockCondition
    {
        public abstract bool Satisfied(IUnlockStateDataSource data);

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
            if (data is not WorldAndPlayerDataSource worldArgs)
                return false;

            var energyBalance = worldArgs.EnergyBalance;

            if (energyBalance == null)
                return false;

            return energyBalance.TotalProduction >= Atp;
        }

        public override void GenerateTooltip(LocalizedStringBuilder builder, IUnlockStateDataSource data)
        {
            builder.Append(new LocalizedString("UNLOCK_CONDITION_ATP_PRODUCTION_ABOVE", Atp));
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
            if (data is not WorldAndPlayerDataSource worldArgs)
                return false;

            var energyBalance = worldArgs.EnergyBalance;

            if (energyBalance == null)
                return false;

            return energyBalance.FinalBalance >= Atp;
        }

        public override void GenerateTooltip(LocalizedStringBuilder builder, IUnlockStateDataSource data)
        {
            builder.Append(new LocalizedString("UNLOCK_CONDITION_EXCESS_ATP_ABOVE", Atp));
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
            if (data is not WorldAndPlayerDataSource worldArgs)
                return false;

            var playerData = worldArgs.PlayerData;

            if (playerData == null)
                return false;

            return GetPlayerSpeed(playerData) < Threshold;
        }

        public override void GenerateTooltip(LocalizedStringBuilder builder, IUnlockStateDataSource data)
        {
            builder.Append(new LocalizedString("UNLOCK_CONDITION_SPEED_BELOW", Threshold));
        }

        private float GetPlayerSpeed(ICellDefinition playerData)
        {
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
        public Compound? Compound;

        public override bool Satisfied(IUnlockStateDataSource data)
        {
            if (data is not WorldAndPlayerDataSource worldArgs)
                return false;

            var current = worldArgs.CurrentPatch.GetCompoundAmount(Compound!,
                CompoundAmountType.Biome);

            var minSatisfied = !Min.HasValue || current >= Min;
            var maxSatisfied = !Max.HasValue || current <= Max;
            return minSatisfied && maxSatisfied;
        }

        public override void GenerateTooltip(LocalizedStringBuilder builder, IUnlockStateDataSource data)
        {
            var compoundName = Compound!.InternalName;

            if (Min.HasValue && Max.HasValue)
            {
                builder.Append(new LocalizedString("UNLOCK_CONDITION_COMPOUND_IS_BETWEEN", compoundName, Min, Max));
            }
            else if (Min.HasValue)
            {
                builder.Append(new LocalizedString("UNLOCK_CONDITION_COMPOUND_IS_ABOVE", compoundName, Min));
            }
            else if (Max.HasValue)
            {
                builder.Append(new LocalizedString("UNLOCK_CONDITION_COMPOUND_IS_BELOW", compoundName, Max));
            }
        }

        public override void Check(string name)
        {
            if (Compound == null)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Compound is null");
            }

            if (Min == null && Max == null)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Min and Max are both null");
            }
        }
    }
}
