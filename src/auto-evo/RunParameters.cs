namespace AutoEvo
{
    using System;

    /// <summary>
    ///   Contains the parameters for an auto-evo run
    /// </summary>
    public class RunParameters
    {
        public readonly GameWorld World;
        public readonly DayNightConfiguration DayNightConfiguration;

        public RunParameters(GameWorld world)
        {
            World = world ?? throw new ArgumentException("GameWorld is null");

            // For now, always load the day/night cycle configuration from static JSON parameters
            DayNightConfiguration = SimulationParameters.Instance.GetDayNightCycleConfiguration();
        }
    }
}
