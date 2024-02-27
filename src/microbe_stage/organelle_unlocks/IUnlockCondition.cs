namespace UnlockConstraints
{
    public interface IUnlockCondition
    {
        /// <summary>
        ///   Checks if the current state of the game satisfies this unlock condition
        /// </summary>
        /// <param name="data">
        ///   The data about the world, if this condition doesn't handle the data of the given type this always
        ///   returns false
        /// </param>
        /// <returns>True if condition satisfied</returns>
        public bool Satisfied(IUnlockStateDataSource data);

        /// <summary>
        ///   Generates a tooltip describing how close this unlock condition is to be unlocked
        /// </summary>
        /// <param name="builder">Where to put the result text</param>
        /// <param name="data">Where to get info on how close to the condition is to be satisfied</param>
        public void GenerateTooltip(LocalizedStringBuilder builder, IUnlockStateDataSource data);

        /// <inheritdoc cref="IRegistryType.Check"/>
        public void Check(string name);

        /// <inheritdoc cref="OrganelleDefinition.Resolve"/>
        public void Resolve(SimulationParameters parameters);
    }

    /// <summary>
    ///   Interface that marks the various sources of data that <see cref="IUnlockCondition"/> uses to unlock itself
    ///   when the data matches the required conditions
    /// </summary>
    public interface IUnlockStateDataSource
    {
    }

    public class WorldAndPlayerDataSource : IUnlockStateDataSource
    {
        public readonly Patch CurrentPatch;
        public readonly GameWorld World;
        public readonly EnergyBalanceInfo? EnergyBalance;
        public readonly ICellDefinition? PlayerData;

        public WorldAndPlayerDataSource(GameWorld world, Patch currentPatch, EnergyBalanceInfo? energyBalance,
            ICellDefinition? playerData)
        {
            World = world;
            CurrentPatch = currentPatch;
            EnergyBalance = energyBalance;
            PlayerData = playerData;
        }
    }
}
