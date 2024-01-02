namespace UnlockConstraints
{
    using System;

    public interface IUnlockCondition
    {
        public bool Satisfied(EventArgs args);

        public void GenerateTooltip(LocalizedStringBuilder builder, EventArgs args);

        public void Check(string name);

        public void Resolve(SimulationParameters parameters);
    }

    public class StatisticTrackerEventArgs : EventArgs
    {
        public WorldStatsTracker StatsTracker;

        public StatisticTrackerEventArgs(WorldStatsTracker statsTracker)
        {
            StatsTracker = statsTracker;
        }
    }

    public class WorldAndPlayerEventArgs : EventArgs
    {
        public GameWorld World;
        public EnergyBalanceInfo? EnergyBalance;
        public ICellProperties? PlayerData;

        public WorldAndPlayerEventArgs(GameWorld world, EnergyBalanceInfo? energyBalance, ICellProperties? playerData)
        {
            World = world;
            EnergyBalance = energyBalance;
            PlayerData = playerData;
        }
    }
}
