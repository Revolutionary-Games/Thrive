namespace UnlockConstraints
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    ///   An unlock condition that relies on world data to track progress
    /// </summary>
    public abstract class WorldBasedUnlockCondition : IUnlockCondition
    {
        protected ICellProperties? PlayerData { get; set; }

        protected EnergyBalanceInfo? EnergyBalance { get; set; }

        protected GameWorld? GameWorld { get; set; }

        public virtual void UpdateData(GameWorld? gameWorld, ICellProperties? playerData,
            EnergyBalanceInfo? energyBalance)
        {
            PlayerData = playerData;
            EnergyBalance = energyBalance;
            GameWorld = gameWorld;
        }

        public abstract bool Satisfied();

        public abstract void GenerateTooltip(LocalizedStringBuilder builder);

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

        public override bool Satisfied()
        {
            if (EnergyBalance == null)
                return false;

            return EnergyBalance.TotalProduction >= Atp;
        }

        public override void GenerateTooltip(LocalizedStringBuilder builder)
        {
            if (EnergyBalance == null)
                return;

            builder.Append(new LocalizedString("ATP_PRODUCTION_ABOVE", Atp));
        }
    }

    /// <summary>
    ///   The player species produces a specified amount more ATP than it uses.
    /// </summary>
    public class ExcessAtpAbove : WorldBasedUnlockCondition
    {
        [JsonProperty]
        public float Atp;

        public override bool Satisfied()
        {
            if (EnergyBalance == null)
                return false;

            return EnergyBalance.FinalBalance >= Atp;
        }

        public override void GenerateTooltip(LocalizedStringBuilder builder)
        {
            if (EnergyBalance == null)
                return;

            builder.Append(new LocalizedString("EXCESS_ATP_ABOVE", Atp));
        }
    }

    /// <summary>
    ///   The player species has speed that is slower than a certain speed.
    /// </summary>
    public class SpeedBelow : WorldBasedUnlockCondition
    {
        [JsonProperty]
        public float Threshold;

        [JsonIgnore]
        private float speed;

        public override void UpdateData(GameWorld? gameWorld, ICellProperties? playerData,
            EnergyBalanceInfo? energyBalance)
        {
            base.UpdateData(gameWorld, playerData, energyBalance);

            if (PlayerData == null)
                return;

            var rawSpeed = MicrobeInternalCalculations.CalculateSpeed(PlayerData.Organelles.Organelles,
                PlayerData.MembraneType,
                PlayerData.MembraneRigidity,
                PlayerData.IsBacteria);

            // This needs to be user readable as it is shown by the tooltip
            speed = (float)Math.Round(MicrobeInternalCalculations.SpeedToUserReadableNumber(rawSpeed), 1);
        }

        public override bool Satisfied()
        {
            return speed < Threshold;
        }

        public override void GenerateTooltip(LocalizedStringBuilder builder)
        {
            builder.Append(new LocalizedString("SPEED_BELOW", Threshold));
        }
    }

    /// <summary>
    ///   The player's current patch has a certain amount of certain compound.
    /// </summary>
    public class PatchCompound : WorldBasedUnlockCondition
    {
        [JsonProperty("Compound")]
        public string RawCompound = string.Empty;

        [JsonProperty]
        public int? Min;

        [JsonProperty]
        public int? Max;

        [JsonIgnore]
        private Compound? compound;

        [JsonIgnore]
        private float current;

        public override void UpdateData(GameWorld? gameWorld, ICellProperties? playerData,
            EnergyBalanceInfo? energyBalance)
        {
            base.UpdateData(gameWorld, playerData, energyBalance);

            compound ??= SimulationParameters.Instance.GetCompound(RawCompound);

            if (GameWorld == null)
                return;

            current = GameWorld.Map.CurrentPatch!.GetCompoundAmount(compound);
        }

        public override bool Satisfied()
        {
            var minSatisfied = !Min.HasValue || current >= Min;
            var maxSatisfied = !Max.HasValue || current <= Max;
            return minSatisfied && maxSatisfied;
        }

        public override void GenerateTooltip(LocalizedStringBuilder builder)
        {
            if (compound == null)
                return;

            var compoundName = compound.InternalName;

            if (Min.HasValue && Max.HasValue)
                builder.Append(new LocalizedString("COMPOUND_IS_BETWEEN", compoundName, Min, Max));
            else if (Min.HasValue)
                builder.Append(new LocalizedString("COMPOUND_IS_ABOVE", compoundName, Min));
            else if (Max.HasValue)
                builder.Append(new LocalizedString("COMPOUND_IS_BELOW", compoundName, Max));
        }

        public override void Check(string name)
        {
            if (string.IsNullOrEmpty(RawCompound))
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Compound is empty");
            }
        }
    }
}
